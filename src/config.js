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
  // Base URL of the Forkly .NET API (auth, profile, orders). Login/register/account
  // are served IN this app now; they call this API directly via services/authApi.js.
  apiBase: (import.meta.env.VITE_API_BASE || 'http://localhost:5080').trim(),

  // Base URL of the ORDER microservice REST/gRPC-web gateway. When empty, the
  // order page accepts orders in a local "demo" mode (no network call).
  orderApiBase: (import.meta.env.VITE_ORDER_API_BASE || '').trim(),

  // Whether the PAYMENT service/page is ready. When true, placing an order
  // auto-redirects to the payment page. Until the payment team (other branch)
  // ships, keep this false: the order is created and shown as awaiting payment.
  paymentReady: (import.meta.env.VITE_PAYMENT_READY || '').trim() === 'true',
}
