// =========================================================
// Forkly — Auth bridge to the IZZUWAN Auth Module
//
// Login UI + logic are OWNED by the User Service / Auth Module (IZZUWAN).
// This file is the ONLY integration seam. We never render a login form, drawer,
// or modal here — we just ask their module to open, and react to its result.
//
// Cross-module contract (event-based, decoupled):
//   Forkly  -> IZZUWAN : window event  'forkly:open-login-drawer'
//   IZZUWAN -> Forkly  : window event  'forkly:login-success'  detail: { user, token? }
//
/// TODO: integrate with IZZUWAN Auth Module
/// TODO: listen to auth success event   (done below via 'forkly:login-success')
/// TODO: attach token to API requests   (capture detail.token here)
/// TODO: persist cart server-side after login
// =========================================================

import { useAuth } from '../stores/auth.js'

// Ask the IZZUWAN module to open its existing login drawer/modal.
// No redirect, no UI created here.
export function openLoginDrawer() {
  if (typeof window === 'undefined') return

  // Preferred: call their module directly if it exposes a global hook.
  // e.g. window.izzuwanAuth?.openLoginDrawer()
  // Fallback: broadcast an event their module can subscribe to.
  window.dispatchEvent(new CustomEvent('forkly:open-login-drawer'))
}

// Wire up the listener for IZZUWAN's success signal exactly once.
// On success we update our auth state; cart merge + checkout resume are handled
// by whoever initiated the login (see CartSummary).
export function initAuthBridge() {
  if (typeof window === 'undefined' || window.__forklyAuthBridgeReady) return
  window.__forklyAuthBridgeReady = true

  const { setUser } = useAuth()

  window.addEventListener('forkly:login-success', (event) => {
    const user = event?.detail?.user ?? { name: 'Signed-in User' }
    // TODO: capture event.detail.token and attach it to outgoing API requests.
    setUser(user)
  })
}
