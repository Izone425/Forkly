# Forkly API (.NET 10)

User-management microservice for Forkly: registration, login, and current-user
lookup, exposed over **both REST and gRPC** (gRPC-web enabled), backed by
PostgreSQL via EF Core + ASP.NET Core Identity, issuing JWTs.

## Endpoints

| Operation | REST | gRPC (`auth.AuthService`) |
|---|---|---|
| Register (→ JWT) | `POST /api/auth/register` | `Register` |
| Login (→ JWT) | `POST /api/auth/login` | `Login` |
| Current user | `GET /api/auth/me` *(Bearer)* | `GetCurrentUser` *(Bearer in metadata)* |

Both transports delegate to the same `IAccountService`, so logic isn't duplicated.

## Ports

- **5080** — HTTP/1.1: REST + gRPC-web (used by the browser/auth app).
- **5081** — HTTP/2 cleartext (h2c): native gRPC (service-to-service, grpcurl).

## Run

```powershell
# 1. Start PostgreSQL (self-contained local cluster; see DATABASE.md)
pwsh -File scripts/start-postgres.ps1

# 2. First-time secrets (JWT signing key + seed admin) — already set via user-secrets;
#    to recreate:
#    dotnet user-secrets set "Jwt:Key" "<base64 32+ bytes>"
#    dotnet user-secrets set "Seed:AdminEmail" "admin@forkly.local"
#    dotnet user-secrets set "Seed:AdminPassword" "Admin123!"

# 3. Apply migrations + run (migrations also run automatically on startup)
dotnet ef database update
dotnet run
```

Swagger UI: `http://localhost:5080/swagger` (Development only).

## Roles & seeding

Roles `client` (default for self-registration) and `admin` are seeded on startup.
If `Seed:AdminEmail`/`Seed:AdminPassword` are set, a bootstrap admin is created.

See **DATABASE.md** for provider switching (Postgres ⇄ SQLite) and DB details.
