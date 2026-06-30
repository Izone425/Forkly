# Forkly-MenuApi — Persistent Menu Microservice with gRPC

**Date:** 2026-06-30
**Status:** Approved (brainstorm phase)
**Working branch:** `amirul-menu-grpc` (off `amirul-menu-be`, merged `amirul-menu-fe`)

## Context / Problem

The menu needs to be a proper standalone microservice that (a) persists all data — adding a menu item must save to the database, never show mock/sample data — and (b) interacts with other services via gRPC.

A persistent menu service already exists (built previously, on `amirul-menu-be`): `backend/Forkly.MenuService/` is a REST API backed by EF Core → PostgreSQL (shared AWS RDS `foodorder` DB, isolated `menu` schema) with migrations, an idempotent seeder, and JWT-admin CRUD that genuinely persists. What's missing:

1. **gRPC** — the contract `backend/contracts/menu.proto` (`GetMenu`, `GetItem`) exists and the Order service is ready to consume it, but the menu service has no gRPC server.
2. **Validation** — `Forkly.OrderService` accepts item name/price from the client without validating against the menu.
3. **Location** — the service lives under `backend/`; it should be a top-level service at the repo root (like `Forkly-Api`).
4. **Mock data** — on `main` the frontend falls back to a hardcoded list (`src/data/menu.js`) because `VITE_MENU_API_BASE` is unset; added items can't save.

This work: relocate the existing service to the root, add a gRPC server, wire the Order service to it, and remove the silent mock fallback.

## Decisions (approved)

- **Folder:** relocate `backend/Forkly.MenuService/` → **`Forkly-MenuApi/`** at repo root via `git mv` (preserve history).
- **gRPC scope:** add server (`GetMenu`/`GetItem`) **and** fully wire `Forkly.OrderService` to validate item existence and use the **authoritative DB price** at checkout (stop trusting client prices).
- **Frontend:** point `VITE_MENU_API_BASE` at the live service; when configured, on API failure show an empty/error state — **never** silently fall back to the sample list.

## Architecture

```
Forkly/
├─ Forkly-Api/          auth/accounts (REST 5080, native gRPC 5081)
├─ Forkly-MenuApi/      ← moved here. REST :5100  +  native gRPC :5102
├─ backend/
│  ├─ Forkly.OrderService/   ← gains gRPC client to the menu
│  └─ contracts/menu.proto   ← shared contract (unchanged)
└─ src/                 Vue frontend
```

Ports mirror the Forkly-Api pattern: **5100** REST (HTTP/1.1+2; browser + admin CRUD), **5102** native gRPC (HTTP/2 cleartext; service-to-service). 5101 is avoided (used by the legacy order service).

### Layered structure (unchanged from existing service)
Controller → Service (`IMenuService`/`ICategoryService`) → Repository → `MenuDbContext` (EF Core) → PostgreSQL `menu` schema.

## Components

### New: `Forkly-MenuApi/Services/Grpc/MenuGrpcService.cs`
Implements generated `MenuService.MenuServiceBase`, reusing `IMenuService`/repository. Entity ↔ proto mapping:

| proto field (`menu.proto`) | source (EF `MenuItem`) |
|---|---|
| `id` (string) | `Id` (int) → `ToString()` |
| `name` | `Name` |
| `description` | `Description` |
| `price_cents` (int32) | `Price` (decimal) × 100, rounded |
| `category` | `Category.Name` |
| `emoji` | `""` (entity has `ImageUrl`, no emoji; not needed by consumers) |
| `available` | `Availability` |

- `GetItem`: parse string `id` → int; missing → gRPC `StatusCode.NotFound`. (Also treat unavailable items per order rules.)
- `GetMenu`: return available items.
- csproj: add `Grpc.AspNetCore`; `<Protobuf Include="..\backend\contracts\menu.proto" GrpcServices="Server" />`.
- `Program.cs`: `AddGrpc()`, `MapGrpcService<MenuGrpcService>()`, two Kestrel endpoints (5100 http1+2, 5102 http2).

### Changed: `backend/Forkly.OrderService/`
- Add `Grpc.Net.ClientFactory` + `<Protobuf ... GrpcServices="Client" />` on `menu.proto`.
- Config: `Menu:GrpcAddress` (`http://localhost:5102`), `Menu:UseMock` (bool) — mirrors the legacy order service pattern.
- New `IMenuCatalog` abstraction: real gRPC-backed impl + mock impl (offline/tests).
- `OrderService.CreateOrder`: per cart line, `GetItem(menuId)`. Unknown item → reject (400). Use authoritative `price_cents`→decimal and name for the stored snapshot; SST/total computed server-side as today. Menu service unreachable with `UseMock=false` → 503 (do not trust client data).

### Changed: frontend
- `.env.development`: `VITE_MENU_API_BASE=http://localhost:5100`.
- `src/stores/menu.js`: when configured (now always), load from API; on failure set error/empty — no silent swap to `src/data/menu.js`. Sample file stays but is dead in configured mode.
- `AdminMenu.vue` already calls the real REST endpoints — no change.

### Tooling
- `start-all.ps1`: update menu path to `Forkly-MenuApi`.
- Solution file(s): update project path; ensure `Forkly-MenuApi` referenced.
- Migrations auto-apply on startup; seeder idempotent.

## Data flow

1. Admin add/edit/delete → REST (JWT admin) → EF → Postgres **(persisted permanently)**.
2. Landing/Order pages → `GET /api/menu` → real DB items.
3. Checkout → `Forkly.OrderService.CreateOrder` → **gRPC `GetItem`** (→ `:5102`) → validate + authoritative price → order persisted with trusted snapshot.

## Error handling

- REST: 400 validation, 404 not found, 401/403 auth (existing).
- gRPC server: `NotFound` for missing item.
- OrderService: unknown item → 400 with message; menu down + `UseMock=false` → 503.
- Frontend: service down → error/empty state, never mock.

## Testing

- **.NET (xUnit, new test project(s)):**
  - `MenuGrpcService`: `GetItem` maps entity→proto correctly (incl. `price_cents`); missing id → `NotFound`; `GetMenu` returns available items.
  - `OrderService.CreateOrder`: rejects unknown item; **overwrites client-sent price** with menu authoritative price (mock `IMenuCatalog`).
- **Frontend (Vitest):** menu store uses API data when configured; on API failure shows error/empty, **never** the sample list; unconfigured → legacy fallback.

## Out of scope (YAGNI)

Payment gRPC, the legacy `backend/OrderService`, category-over-gRPC, image upload.

## Verification (end-to-end)

1. Run `Forkly-MenuApi` (5100 REST + 5102 gRPC) + `Forkly.OrderService` + frontend.
2. Add a menu item in `/admin/menu` → reload → it persists (DB-backed, survives restart).
3. Frontend landing/order shows DB items (not the 6 sample items); with service stopped, UI shows error/empty (no mock).
4. Place an order containing that item → succeeds with the menu's authoritative price; an order with a bogus `menuId` → rejected.
5. `dotnet test` green; `npm run test` green.
