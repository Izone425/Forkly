// =========================================================
// Forkly — menu store (shared by landing + order pages)
//
// Loads the menu from the Order service REST API (which proxies amirul-menu).
// If no backend is configured, or the request fails, it falls back to the
// bundled menu so the UI still renders. Loaded once and cached.
// =========================================================

import { reactive } from 'vue'
import { config } from '../config.js'
import { MENU as FALLBACK } from '../data/menu.js'

const state = reactive({
  items: [],
  loading: false,
  loaded: false,
  error: '',
  source: 'none', // 'api' | 'fallback'
})

async function load(force = false) {
  if (state.loading) return
  if (state.loaded && !force) return

  state.loading = true
  state.error = ''

  // No backend configured -> use the bundled fallback menu.
  if (!config.orderApiBase) {
    state.items = FALLBACK
    state.source = 'fallback'
    state.loaded = true
    state.loading = false
    return
  }

  try {
    const res = await fetch(`${config.orderApiBase}/v1/menu`)
    if (!res.ok) throw new Error(`Menu service responded ${res.status}`)
    const data = await res.json()
    state.items = data
      .filter((i) => i.available !== false)
      .map((i) => ({
        id: i.id,
        name: i.name,
        description: i.description,
        price: i.price,
        emoji: i.emoji,
        category: i.category,
      }))
    state.source = 'api'
    state.loaded = true
  } catch (e) {
    // Graceful fallback so the page still works.
    state.error = e?.message || 'Could not load the menu.'
    state.items = FALLBACK
    state.source = 'fallback'
    state.loaded = true
  } finally {
    state.loading = false
  }
}

export function useMenu() {
  return { state, load }
}
