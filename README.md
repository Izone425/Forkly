# Forkly — Single-Restaurant Food Ordering

A single-restaurant food ordering system built as a microservices training
project. This repository is the **integrated app**: one Vue frontend (landing +
login + register + account) and the **.NET API**.

- **Stack:** Vue 3 (SFC) + Vite (frontend), ASP.NET Core (API), PostgreSQL (SIT)
- **Theme:** Enterprise SaaS (blue / white), Inter typography

## The two apps

| App | Folder | URL |
|-----|--------|-----|
| **Forkly-Api** — backend API | `Forkly-Api/` | http://localhost:5080 |
| **Forkly-Landing** — the whole frontend (landing, login, account) | repo root | http://localhost:5173 |

> Login/register/account are served **in the landing app** (routes `/login`,
> `/register`, `/account`, plus a slide-in login drawer) and call the API at
> `http://localhost:5080`. No per-machine env setup is needed; the API base
> defaults to `:5080` (override with `VITE_API_BASE`), and the API's
> `appsettings.Development.json` is committed with the right values.

## Getting started

### Option A — one command (no Visual Studio needed)

```powershell
./start-all.ps1
```

This frees stale ports (5080 / 5173), runs `npm install` if `node_modules` is
missing, and launches the API + frontend, each in its own window. Then open
**http://localhost:5173**.

### Option B — Visual Studio (F5)

1. Open **`Forkly.sln`**.
2. Set the startup profile to **All Forkly** (starts API + landing together).
3. Press **F5**.

Requires the Visual Studio **Node.js development** workload (the `.esproj`
project needs it). The JS SDK restores from NuGet on first open (needs internet).

### Run a single app manually

```bash
npm install && npm run dev          # frontend -> http://localhost:5173
cd Forkly-Api  && dotnet run        # API      -> http://localhost:5080
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
