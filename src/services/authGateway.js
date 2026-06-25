// =========================================================
// Forkly — Auth handoff gateway
//
// This is OUR side of the contract with the login service that another team
// member is building. The landing page does NOT implement authentication; it
// only hands control off to the login page/service when the user clicks Login.
//
// Two handoff strategies are provided:
//   1. redirectToLogin()  — simple URL redirect (the default, works today).
//   2. requestLoginSession() — optional REST/gRPC-web call that asks the auth
//      service where to send the user (commented stub, ready for the future).
// =========================================================

import { config, isLoginConfigured } from '../config.js'

/**
 * Build the handoff URL to the login service, attaching context so the
 * other team's page knows where the request originated and (optionally)
 * which role tab to preselect.
 *
 * @param {string} [role]  'client' | 'admin' | '' — purely a UI hint.
 * @returns {string} the full login URL, or '' if not configured yet.
 */
export function buildLoginUrl(role = '') {
  if (!isLoginConfigured()) return ''

  const url = new URL(config.loginUrl)
  url.searchParams.set('from', 'forkly-landing')
  if (config.appOrigin) url.searchParams.set('return_to', config.appOrigin)
  if (role) url.searchParams.set('role', role)
  return url.toString()
}

/**
 * Primary handoff: redirect the browser to the login service.
 *
 * @param {string} [role]
 * @returns {boolean} true if a redirect happened; false if the login service
 *   isn't wired up yet (so the UI can show a friendly "coming soon" message).
 */
export function redirectToLogin(role = '') {
  const target = buildLoginUrl(role)
  if (!target) return false
  window.location.assign(target)
  return true
}

// ---------------------------------------------------------------------------
// Future integration: API-driven handoff (REST today, gRPC-web via a proxy).
//
// When the auth microservice exposes a "start session" endpoint, call it here
// before redirecting. Browsers cannot speak raw gRPC — use a REST gateway or
// gRPC-web through an Envoy proxy. Uncomment and point VITE_AUTH_API_BASE at
// the gateway when it exists.
//
// export async function requestLoginSession(role = '') {
//   const res = await fetch(`${config.authApiBase}/v1/session/init`, {
//     method: 'POST',
//     headers: { 'Content-Type': 'application/json' },
//     body: JSON.stringify({
//       source: 'forkly-landing',
//       returnTo: config.appOrigin,
//       role,
//     }),
//   })
//   if (!res.ok) throw new Error(`Auth service responded ${res.status}`)
//
//   // The service tells us exactly where to send the user.
//   const { loginUrl } = await res.json()
//   window.location.assign(loginUrl)
// }
// ---------------------------------------------------------------------------
