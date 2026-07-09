// =========================================================
// Forkly — guest/session id
//
// A stable per-browser id used to attribute cart stock reservations on the server
// (sent as the `X-Forkly-Session` header). It's independent of auth: guests and
// logged-in users both get one, so the cart works before sign-in. Persisted in
// localStorage; clearing storage simply starts a new session (old holds expire).
// =========================================================

const SESSION_KEY = 'forkly.sid'

export function getSessionId() {
  try {
    if (typeof localStorage === 'undefined') return 'ephemeral'
    let id = localStorage.getItem(SESSION_KEY)
    if (!id) {
      id = (crypto?.randomUUID?.() ?? `sid-${Date.now()}-${Math.random().toString(36).slice(2)}`)
      localStorage.setItem(SESSION_KEY, id)
    }
    return id
  } catch {
    // Storage unavailable (private mode quirks) — fall back to a volatile id.
    return 'ephemeral'
  }
}
