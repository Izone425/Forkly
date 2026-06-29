// Reads the handoff context the landing page attaches when it redirects/embeds:
//   ?from=forkly-landing&return_to=<origin>&role=<client|admin>[&embed=1]
// (produced by Forkly/src/services/authGateway.js:buildLoginUrl).
//
// Two completion modes:
//  - Embedded (in the landing's drawer iframe): postMessage the token+user to the
//    parent window so the drawer can close. No navigation.
//  - Standalone (full page): redirect back to return_to with the JWT in the URL
//    fragment (#access_token=...). Training default; for production prefer an
//    httpOnly cookie set by the API on a shared parent domain.

import { absoluteUrl } from '../services/api.js'

const params = new URLSearchParams(window.location.search)

// The landing renders the user's avatar; make its URL absolute (the file lives on
// the API origin, not the landing origin) before sending it across.
function withAbsoluteAvatar(user) {
  if (!user) return user
  return { ...user, avatarUrl: absoluteUrl(user.avatarUrl) }
}

export function useHandoff() {
  const from = params.get('from') || ''
  const returnTo = params.get('return_to') || ''
  const role = params.get('role') || ''
  const embedded = params.get('embed') === '1' || window.top !== window.self

  // Only follow http(s) return targets to avoid an open-redirect via javascript:.
  function isSafe(url) {
    try {
      const u = new URL(url)
      return u.protocol === 'http:' || u.protocol === 'https:'
    } catch {
      return false
    }
  }

  function completeHandoff(token, user) {
    if (embedded) {
      // Tell the parent (landing) drawer we're authenticated. Target the landing
      // origin when known, so the token isn't broadcast to arbitrary frames.
      const target = isSafe(returnTo) ? new URL(returnTo).origin : '*'
      window.parent.postMessage(
        { type: 'forkly-auth:success', token, user: withAbsoluteAvatar(user) },
        target,
      )
      return true
    }

    if (returnTo && isSafe(returnTo)) {
      const target = new URL(returnTo)
      if (token) target.hash = `access_token=${encodeURIComponent(token)}`
      window.location.assign(target.toString())
      return true
    }
    return false
  }

  // Send a message to the parent (landing) window, targeted at its origin.
  // Used by the profile page for live updates / logout while embedded.
  function postToParent(type, payload = {}) {
    const target = isSafe(returnTo) ? new URL(returnTo).origin : '*'
    window.parent.postMessage({ type, ...payload }, target)
  }

  return { from, returnTo, role, embedded, completeHandoff, postToParent }
}
