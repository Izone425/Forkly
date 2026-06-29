// =========================================================
// Forkly — auth store (minimal)
//
// Authentication itself is handled by the user-management frontend
// (izone-user-management-FE). After a successful login it should return to the
// landing page and call setUser() with the signed-in profile; the header then
// shows the profile instead of the Login button.
//
// Default state is logged-out. This store holds no credentials — only the
// display profile.
// =========================================================

import { reactive, computed } from 'vue'

const state = reactive({
  user: null, // { name: string, email?: string } | null
})

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

  function logout() {
    state.user = null
  }

  return { state, isLoggedIn, initials, setUser, logout }
}
