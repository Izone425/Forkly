// =========================================================
// Forkly — cart store (client-side, persisted)
//
// A tiny shared reactive store. State lives at module scope so every component
// that calls useCart() reads and mutates the SAME cart.
//
// The cart is PERSISTED to localStorage so it survives page refreshes, the
// login modal opening/closing, and the (external) login process — the guest
// cart is never lost. After login it is merged into the user's cart.
// =========================================================

import { reactive, computed, watch } from 'vue'
import { mergeCartWithReorderItems } from '../utils/cartMerge.js'

const SST_RATE = 0.06 // 6% SST (Malaysia) — display only.
const STORAGE_KEY = 'forkly:cart'

// id -> { item, qty }
const state = reactive({ lines: {} })

// --- persistence ---------------------------------------------------------
function restore() {
  try {
    if (typeof localStorage === 'undefined') return
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return
    const saved = JSON.parse(raw)
    const lines = {}
    for (const l of saved) {
      if (l?.item?.id) lines[l.item.id] = { item: l.item, qty: l.qty }
    }
    state.lines = lines
  } catch {
    /* ignore corrupt storage */
  }
}

function persist() {
  try {
    if (typeof localStorage === 'undefined') return
    const data = Object.values(state.lines).map((l) => ({ item: l.item, qty: l.qty }))
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data))
  } catch {
    /* storage full / unavailable — non-fatal */
  }
}

restore()
watch(() => state.lines, persist, { deep: true })

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

  // Add an item with a specific quantity (merges into an existing line).
  function addQuantity(item, qty) {
    const n = Math.max(1, Math.floor(qty || 1))
    const line = state.lines[item.id]
    if (line) line.qty += n
    else state.lines[item.id] = { item, qty: n }
  }

  // Reorder: MERGE reorder items into the current cart (never replace).
  // Delegates the quantity math to the pure mergeCartWithReorderItems(), then
  // applies the result back — reusing existing rich item objects and resolving
  // new ones via `resolveItem(menuId)` (e.g. the live menu) so menu steppers and
  // prices stay correct.
  //
  // reorderItems: [{ menuId, name, quantity, price }]
  function mergeReorder(reorderItems, resolveItem) {
    const current = Object.values(state.lines).map((l) => ({
      menuId: l.item.id,
      name: l.item.name,
      quantity: l.qty,
      price: l.item.price,
    }))

    const merged = mergeCartWithReorderItems(current, reorderItems)

    for (const m of merged) {
      const existing = state.lines[m.menuId]
      if (existing) {
        existing.qty = m.quantity
      } else {
        const item = (resolveItem && resolveItem(m.menuId)) || {
          id: m.menuId,
          name: m.name,
          price: m.price,
        }
        state.lines[m.menuId] = { item, qty: m.quantity }
      }
    }
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
    add, addQuantity, mergeReorder, increment, decrement, remove, clear, qtyOf,
    SST_RATE,
  }
}
