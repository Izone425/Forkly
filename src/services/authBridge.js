// =========================================================
// Forkly — login drawer trigger
//
// Login, register and account are served IN this app now (src/views/{Login,
// Register,Profile}View + src/services/authApi.js + stores/auth.js). This module
// is just the seam to OPEN the slide-in login drawer from anywhere (AppHeader,
// CartSummary) via a window event the drawer (components/LoginDrawer.vue) listens
// for. After a successful sign-in the form updates the auth store directly, so
// CartSummary's watch(isLoggedIn) resumes a pending checkout.
// =========================================================

// Ask the login drawer to open. No redirect, no UI created here.
export function openLoginDrawer() {
  if (typeof window === 'undefined') return
  window.dispatchEvent(new CustomEvent('forkly:open-login-drawer'))
}
