// =========================================================
// Forkly — toast notifications (lightweight, client-side)
//
// A tiny shared queue. Call show(message) from anywhere; render <ToastHost />
// once on the page. Toasts auto-dismiss.
// =========================================================

import { reactive } from 'vue'

const state = reactive({ items: [] })
let seq = 0

export function useToast() {
  function show(message, { duration = 2800 } = {}) {
    const id = ++seq
    state.items.push({ id, message })
    setTimeout(() => dismiss(id), duration)
  }

  function dismiss(id) {
    const i = state.items.findIndex((t) => t.id === id)
    if (i !== -1) state.items.splice(i, 1)
  }

  return { state, show, dismiss }
}
