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
  user: null, // { name, email?, fullName?, avatarUrl?, addresses? } | null
})

// Map an API UserDto onto the shape the landing header/store expect (needs `name`).
function mapUser(u) {
  return { ...u, name: u.fullName || u.name || u.email }
}

export function useAuth() {
  const isLoggedIn = computed(() => state.user !== null)

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
  async function hydrate() {
    if (!getToken()) return
    try {
      state.user = mapUser(await me())
    } catch {
      // Token missing/expired/invalid — drop it and stay logged out.
      clearToken()
      state.user = null
    }
  }

  return { state, isLoggedIn, initials, setUser, signIn, logout, hydrate }
}
