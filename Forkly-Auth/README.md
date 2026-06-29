# Forkly — Auth App (Login + Register)

The login/register front end for Forkly. The landing page (`../Forkly`) redirects
here via `VITE_LOGIN_URL`; this app talks to the .NET API (`../Forkly-Api`) and,
on success, sends the user back to the `return_to` origin.

- **Stack:** Vue 3 (SFC) + Vite + vue-router
- **Port:** 5174
- **API:** REST against `VITE_API_BASE` (default `http://localhost:5080`)

## Getting started

```bash
npm install
npm run dev      # http://localhost:5174
```

## Handoff contract

The landing page sends `?from=forkly-landing&return_to=<origin>&role=<client|admin>`.
`src/composables/useHandoff.js` reads these and, after auth, redirects back to
`return_to` with the JWT in the URL fragment (`#access_token=...`).

> **Note:** the fragment handoff is the training default because the auth app and
> landing run on different origins (localStorage is not shared). For production,
> prefer an httpOnly cookie set by the API on a shared parent domain.

## REST vs gRPC

`src/services/api.js` uses the API's REST endpoints. The API also exposes the same
operations over gRPC-web (`../Forkly-Api/Protos/auth.proto`); swapping `api.js` for
a generated gRPC-web client is left as an exercise.
