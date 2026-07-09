// =========================================================
// Forkly — auth store
//
// Login/register/profile now live IN this app (src/views/{Login,Register,Profile}View,
// src/services/authApi.js). This store holds the signed-in display profile; the JWT
// itself is persisted by authApi (localStorage) and attached to API requests there.
//
// Default state is logged-out. hydrate() restores the session from a stored token on
// app start, so a page refresh stays signed in.
// =========================================================

import { reactive, computed } from 'vue'
import { me, getToken, clearToken } from '../services/authApi.js'

const state = reactive({
  user: null, // { name, email?, fullName?, roles?, avatarUrl?, addresses? } | null
})

// Shared promise for the one-time startup hydration. Both App.vue (onMounted) and
// the router guard call into this; the first caller triggers the actual /me fetch,
// everyone else awaits the same promise. Lets the admin guard wait for a known
// auth state on a hard refresh instead of bouncing while user is still null.
let hydrationPromise = null

// Map an API UserDto onto the shape the landing header/store expect (needs `name`).
function mapUser(u) {
  return { ...u, name: u.fullName || u.name || u.email }
}

export function useAuth() {
  const isLoggedIn = computed(() => state.user !== null)

  // Roles ride along on the mapped user (mapUser spreads ...u). Lowercase "admin"
  // matches the role string the API issues in the JWT / UserDto.
  const isAdmin = computed(
    () => Array.isArray(state.user?.roles) && state.user.roles.includes('admin'),
  )

  // Kitchen crew. (The User service must define + assign the "crew" role.)
  const isCrew = computed(
    () => Array.isArray(state.user?.roles) && state.user.roles.includes('crew'),
  )

  const initials = computed(() => {
    const name = state.user?.name?.trim()
    if (!name) return '?'
    return name
      .split(/\s+/)
      .map((p) => p[0])
      .slice(0, 2)
      .join('')
      .toUpperCase()
  })

  function setUser(user) {
    state.user = user
  }

  // Called by the login/register forms after authApi has stored the token.
  function signIn(user) {
    state.user = mapUser(user)
  }

  function logout() {
    state.user = null
    clearToken()
  }

  // Restore the session from a stored token on app start (App.vue onMounted).
  // Idempotent: the work runs once and the shared promise is reused, so calling
  // this from both App.vue and the router guard only fetches /me a single time.
  function hydrate() {
    if (hydrationPromise) return hydrationPromise
    hydrationPromise = (async () => {
      if (!getToken()) return
      try {
        state.user = mapUser(await me())
      } catch {
        // Token missing/expired/invalid — drop it and stay logged out.
        clearToken()
        state.user = null
      }
    })()
    return hydrationPromise
  }

  // Resolves once the initial hydration attempt has settled. Route guards await
  // this so they read a settled auth state (not a transient null) on first load.
  const whenReady = () => hydrate()

  return { state, isLoggedIn, isAdmin, isCrew, initials, setUser, signIn, logout, hydrate, whenReady }
}
