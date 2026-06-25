# Forkly — Frontend (Landing Page)

A single-restaurant food ordering system built as a microservices training
project. This repository is the **frontend landing page** only.

- **Stack:** Vue 3 (SFC) + Vite
- **Theme:** Enterprise SaaS (blue / white), Inter typography
- **Scope:** Landing page (Hero, About, Menu preview, Contact). The login page
  is built separately by another team member.

## Getting started

```bash
npm install
npm run dev      # http://localhost:5173
npm run build    # production build into dist/
npm run preview  # preview the production build
```

## The Login button (service handoff)

The landing page does **not** implement authentication. The "Login" buttons
hand control off to a separate login service via `src/services/authGateway.js`:

- If `VITE_LOGIN_URL` is set, clicking Login redirects there with context:
  `?from=forkly-landing&return_to=<this app>&role=<client|admin>`.
- If it is not set yet, the button shows a friendly "service connecting soon"
  message instead of navigating to a broken page.

To wire it up, copy `.env.example` to `.env.local` and set `VITE_LOGIN_URL`.
A REST / gRPC-web (via proxy) handoff stub is included in `authGateway.js` for
when the auth microservice exposes a session-init endpoint.

## Replacing the logo

Drop `forkly-logo.png` into `public/assets/`. No code change needed — until the
file exists, a `[ FORKLY LOGO ]` text fallback is shown. See
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
