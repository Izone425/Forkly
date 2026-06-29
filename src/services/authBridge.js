// =========================================================
// Forkly — Auth bridge to the IZZUWAN Auth Module
//
// Login UI + logic are OWNED by the User Service / Auth Module (IZZUWAN).
// This file is the ONLY integration seam. We never render a login form, drawer,
// or modal here — we just ask their module to open, and react to its result.
//
// Cross-module contract:
//   Forkly  -> IZZUWAN : window event   'forkly:open-login-drawer'
//   IZZUWAN -> Forkly  : window event   'forkly:login-success'  detail: { user, token? }
//   IZZUWAN -> Forkly  : postMessage    'forkly-auth:logout'           (from the embedded
//   IZZUWAN -> Forkly  : postMessage    'forkly-auth:profile-updated'   Forkly-Auth iframe)
//
// Logout + live profile updates are handled HERE (app-wide) rather than in any
// single view, so they work no matter which page hosts the auth iframe (the login
// drawer or the full-page /account). App.vue is always mounted, so this listener
// is always live.
// =========================================================

import { useAuth } from '../stores/auth.js'
import { config } from '../config.js'
import router from '../router/index.js'

// Origin of the auth app (Forkly-Auth), used to validate incoming postMessages.
function authOrigin() {
  try {
    return new URL(config.loginUrl).origin
  } catch {
    return ''
  }
}

// Ask the IZZUWAN module to open its existing login drawer/modal.
// No redirect, no UI created here.
export function openLoginDrawer() {
  if (typeof window === 'undefined') return

  // Preferred: call their module directly if it exposes a global hook.
  // e.g. window.izzuwanAuth?.openLoginDrawer()
  // Fallback: broadcast an event their module can subscribe to.
  window.dispatchEvent(new CustomEvent('forkly:open-login-drawer'))
}

// Wire up the listeners for IZZUWAN's signals exactly once.
export function initAuthBridge() {
  if (typeof window === 'undefined' || window.__forklyAuthBridgeReady) return
  window.__forklyAuthBridgeReady = true

  const { setUser, logout } = useAuth()

  // Login success: the drawer relays Forkly-Auth's success as this window event.
  // Cart merge + checkout resume are handled by whoever initiated the login (see CartSummary).
  window.addEventListener('forkly:login-success', (event) => {
    const user = event?.detail?.user ?? { name: 'Signed-in User' }
    // TODO: capture event.detail.token and attach it to outgoing API requests.
    setUser(user)
  })

  // postMessages from the embedded Forkly-Auth iframe (logout + live profile edits).
  window.addEventListener('message', (event) => {
    const data = event?.data
    if (!data || typeof data.type !== 'string' || !data.type.startsWith('forkly-auth:')) return

    // Only trust the configured auth origin. Warn (don't silently drop) on a
    // mismatch so a port/host drift surfaces instead of a dead logout button.
    const expected = authOrigin()
    if (expected && event.origin !== expected) {
      console.warn(
        `[authBridge] Ignored "${data.type}" from ${event.origin} (expected ${expected}). ` +
          'Is Forkly-Auth running on the port in VITE_LOGIN_URL?',
      )
      return
    }

    if (data.type === 'forkly-auth:logout') {
      // Clear our display profile and return to the landing.
      logout()
      if (router.currentRoute.value.path !== '/') router.push('/')
    } else if (data.type === 'forkly-auth:profile-updated') {
      // Header (name + avatar) updates live while the user edits their profile.
      const u = data.user || {}
      setUser({ ...u, name: u.fullName || u.name || u.email })
    }
  })
}
