// =========================================================
// Forkly — cart store (client-side only)
//
// A tiny shared reactive store. State lives at module scope so every component
// that calls useCart() reads and mutates the SAME cart. No backend, no
// persistence — this is the in-page ordering state only.
// =========================================================

import { reactive, computed } from 'vue'

const SST_RATE = 0.06 // 6% SST (Malaysia) — display only.

// id -> { item, qty }
const state = reactive({ lines: {} })

export function useCart() {
  function add(item) {
    const line = state.lines[item.id]
    if (line) line.qty += 1
    else state.lines[item.id] = { item, qty: 1 }
  }

  function increment(id) {
    const line = state.lines[id]
    if (line) line.qty += 1
  }

  function decrement(id) {
    const line = state.lines[id]
    if (!line) return
    line.qty -= 1
    if (line.qty <= 0) delete state.lines[id]
  }

  function remove(id) {
    delete state.lines[id]
  }

  function clear() {
    state.lines = {}
  }

  function qtyOf(id) {
    return state.lines[id]?.qty ?? 0
  }

  const lines = computed(() => Object.values(state.lines))
  const count = computed(() => lines.value.reduce((n, l) => n + l.qty, 0))
  const isEmpty = computed(() => lines.value.length === 0)
  const subtotal = computed(() => lines.value.reduce((s, l) => s + l.item.price * l.qty, 0))
  const tax = computed(() => Math.round(subtotal.value * SST_RATE * 100) / 100)
  const total = computed(() => Math.round((subtotal.value + tax.value) * 100) / 100)

  return {
    lines, count, isEmpty, subtotal, tax, total,
    add, increment, decrement, remove, clear, qtyOf,
    SST_RATE,
  }
}
