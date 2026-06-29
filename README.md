# Forkly — Single-Restaurant Food Ordering

A single-restaurant food ordering system built as a microservices training
project. This repository is the **integrated app**: the Vue landing page, the
Vue login/account app (**Forkly-Auth**), and the **.NET API** all live here.

- **Stack:** Vue 3 (SFC) + Vite (frontends), ASP.NET Core (API), PostgreSQL (SIT)
- **Theme:** Enterprise SaaS (blue / white), Inter typography

## The three apps (all must run for login to work)

| App | Folder | URL |
|-----|--------|-----|
| **Forkly-Api** — backend API | `Forkly-Api/` | http://localhost:5080 |
| **Forkly-Landing** — landing page (this folder) | repo root | http://localhost:5173 |
| **Forkly-Auth** — login / register / account | `Forkly-Auth/` | http://localhost:5174 |

> The landing's **Login** drawer embeds Forkly-Auth in an iframe
> (`VITE_LOGIN_URL=http://localhost:5174/login`, committed in `.env.development`).
> **If Forkly-Auth isn't running on 5174, the drawer can't load** — so you must
> start all three. No per-machine env setup is needed; `.env.development` and the
> API's `appsettings.Development.json` are committed with the right values.

## Getting started — run all three

### Option A — one command (no Visual Studio needed)

```powershell
./start-all.ps1
```

This frees stale ports (5080 / 5173 / 5174), runs `npm install` where
`node_modules` is missing (landing + Forkly-Auth), and launches all three apps,
each in its own window. Then open **http://localhost:5173** and click **Login**.

### Option B — Visual Studio (F5)

1. Open **`Forkly.sln`**.
2. Set the startup profile to **All Forkly** (starts API + landing + auth together).
3. Press **F5**.

Requires the Visual Studio **Node.js development** workload (the two `.esproj`
projects need it). The JS SDK restores from NuGet on first open (needs internet).

### Run a single app manually

```bash
npm install && npm run dev          # landing  -> http://localhost:5173
cd Forkly-Auth && npm install && npm run dev   # auth -> http://localhost:5174
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
