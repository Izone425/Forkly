// =========================================================
// Forkly — runtime configuration for external integrations.
//
// These values come from Vite env vars (a .env / .env.local file or the
// shell). Nothing here is required for the landing page to render — they
// only switch on the handoff to services owned by other teams.
//
// See .env.example for the available keys.
// =========================================================

export const config = {
  // Base URL of the LOGIN page/service (built by another team member).
  // Example: http://localhost:8081/login  or  https://auth.forkly.local/login
  // Leave empty until that service is ready — the Login button degrades
  // gracefully instead of navigating to a broken page.
  loginUrl: (import.meta.env.VITE_LOGIN_URL || '').trim(),

  // Base URL of the auth microservice's REST/gRPC-web gateway, used only by
  // the (optional) API-based handoff in services/authGateway.js.
  authApiBase: (import.meta.env.VITE_AUTH_API_BASE || '').trim(),

  // Where the auth service should send the user back to after login.
  // Defaults to this app's own origin.
  appOrigin:
    (import.meta.env.VITE_APP_ORIGIN || '').trim() ||
    (typeof window !== 'undefined' ? window.location.origin : ''),
}

// True once the login service URL has been configured.
export const isLoginConfigured = () => Boolean(config.loginUrl)
