// =========================================================
// Forkly — menu store (shared by landing + order pages)
//
// Loads the menu from the MENU microservice (via services/menuApi.js). When the
// Menu API is NOT configured, it uses the bundled sample menu so the UI still
// renders. When it IS configured but the request fails, it shows an error/empty
// state — it does NOT silently swap in the sample menu (that data can't be saved).
// Loaded once and cached.
// =========================================================

import { reactive } from 'vue'
import { MENU as FALLBACK } from '../data/menu.js'
import { fetchMenu, isMenuApiConfigured } from '../services/menuApi.js'

const state = reactive({
  items: [],
  loading: false,
  loaded: false,
  error: '',
  source: 'none', // 'none' | 'api' | 'fallback' | 'error'
})

async function load(force = false) {
  if (state.loading) return
  if (state.loaded && !force) return

  state.loading = true
  state.error = ''

  // Menu service not wired yet -> use the bundled fallback menu.
  if (!isMenuApiConfigured()) {
    state.items = FALLBACK
    state.source = 'fallback'
    state.loaded = true
    state.loading = false
    return
  }

  try {
    state.items = await fetchMenu()
    state.source = 'api'
    state.loaded = true
  } catch (e) {
    // The service is configured but unreachable. Surface an error and show nothing
    // rather than silently swapping in the bundled sample menu (which can't be saved).
    state.error = e?.message || 'Could not load the menu.'
    state.items = []
    state.source = 'error'
    state.loaded = true
  } finally {
    state.loading = false
  }
}

export function useMenu() {
  return { state, load }
}
