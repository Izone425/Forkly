# Forkly — Single-Restaurant Food Ordering

A single-restaurant food ordering system built as a microservices training
project. This repository is the **integrated app**: one Vue frontend (landing +
login + register + account) backed by several **.NET services** (User API, Menu,
Order).

- **Stack:** Vue 3 (SFC) + Vite (frontend), ASP.NET Core (services), PostgreSQL (SIT)
- **Theme:** Enterprise SaaS (blue / white), Inter typography

## The apps

| App | Folder | URL |
|-----|--------|-----|
| **Forkly-Api** — User/Auth/Profile API | `Forkly-Api/` | http://localhost:5080 |
| **Forkly.MenuService** — Menu API (REST + gRPC) | `Forkly-MenuApi/` | http://localhost:5100 (REST) / :5102 (gRPC) |
| **Forkly.OrderService** — Order API | `backend/Forkly.OrderService/` | http://localhost:5208 |
| **Forkly-Landing** — the whole frontend (landing, login, account, admin) | repo root | http://localhost:5173 |

> Login/register/account are served **in the landing app** (routes `/login`,
> `/register`, `/account`, plus a slide-in login drawer) and call the API at
> `http://localhost:5080`. No per-machine env setup is needed; the API base
> defaults to `:5080` (override with `VITE_API_BASE`), and each service's
> `appsettings.Development.json` is committed with the right values.
>
> The admin **Menu** page calls the Menu service at
> `http://localhost:5100` (set via `VITE_MENU_API_BASE` in `.env.development`).
> If that service isn't running you'll see *"Couldn't reach the Menu service"* —
> start everything with `./start-all.ps1`.

## Getting started

### Option A — one command (no Visual Studio needed)

```powershell
./start-all.ps1
```

This frees stale ports (5080 / 5100 / 5102 / 5173), runs `npm install` if
`node_modules` is missing, and launches the API + Menu service + Order service +
frontend, each in its own window. Then open **http://localhost:5173**.

### Option B — Visual Studio (F5)

1. Open **`Forkly.sln`**.
2. Set the startup profile to **All Forkly** (starts API + Menu + Order + landing together).
3. Press **F5**.

Requires the Visual Studio **Node.js development** workload (the `.esproj`
project needs it). The JS SDK restores from NuGet on first open (needs internet).

### Run a single app manually

```bash
npm install && npm run dev                   # frontend -> http://localhost:5173
cd Forkly-Api      && dotnet run             # User API -> http://localhost:5080
cd Forkly-MenuApi  && dotnet run             # Menu     -> http://localhost:5100 (gRPC :5102)
cd backend/Forkly.OrderService && dotnet run # Order    -> http://localhost:5208
```

## Replacing the logo

The single logo asset is `public/assets/forkly-transparent-logo.png`. Replace
that file (keep the name) to swap the logo — no code change needed. If it is
missing, a `[ FORKLY LOGO ]` text fallback is shown. See
`public/assets/README.md`.

## Structure

```
src/
  main.js                 app entry
  App.vue                 renders the landing view
  style.css               global theme tokens + shared button/section styles
  config.js               reads env vars (login URL, auth API base)
  services/authGateway.js login service handoff (redirect today; REST/gRPC-web ready)
  composables/
    useLoginAction.js     shared Login-button behaviour
  views/
    LandingView.vue       composes the page sections
  components/
    AppHeader.vue  BrandLogo.vue  HeroSection.vue
    AboutSection.vue  MenuPreview.vue  ContactSection.vue  AppFooter.vue
```
