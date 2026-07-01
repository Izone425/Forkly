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
import { reserveItem, releaseItem, isMenuApiConfigured, StockError } from '../services/menuApi.js'

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

// Keep the live "N left" figure on a menu item in sync after a reserve/release. The
// item object is shared with the menu store, so the card's availability updates too.
function setAvailable(item, remaining) {
  if (item && typeof item === 'object' && remaining != null) item.availableStock = remaining
}

export function useCart() {
  // Increasing a line HOLDS stock server-side first; if the hold is refused the local
  // cart is left unchanged and the error (StockError) propagates so the UI can react.
  async function add(item) {
    const next = (state.lines[item.id]?.qty ?? 0) + 1
    if (isMenuApiConfigured()) setAvailable(item, await reserveItem(item.id, next))

    const line = state.lines[item.id]
    if (line) line.qty = next
    else state.lines[item.id] = { item, qty: next }
  }

  async function increment(id) {
    const line = state.lines[id]
    if (!line) return
    const next = line.qty + 1
    if (isMenuApiConfigured()) setAvailable(line.item, await reserveItem(id, next))
    line.qty = next
  }

  // Add an item with a specific quantity (merges into an existing line).
  async function addQuantity(item, qty) {
    const n = Math.max(1, Math.floor(qty || 1))
    const next = (state.lines[item.id]?.qty ?? 0) + n
    if (isMenuApiConfigured()) setAvailable(item, await reserveItem(item.id, next))

    const line = state.lines[item.id]
    if (line) line.qty = next
    else state.lines[item.id] = { item, qty: next }
  }

  // Reorder: MERGE reorder items into the current cart (never replace).
  // Delegates the quantity math to the pure mergeCartWithReorderItems(), then
  // applies the result back — reusing existing rich item objects and resolving
  // new ones via `resolveItem(menuId)` (e.g. the live menu) so menu steppers and
  // prices stay correct.
  //
  // reorderItems: [{ menuId, name, quantity, price }]
  async function mergeReorder(reorderItems, resolveItem) {
    const current = Object.values(state.lines).map((l) => ({
      menuId: l.item.id,
      name: l.item.name,
      quantity: l.qty,
      price: l.item.price,
    }))

    const merged = mergeCartWithReorderItems(current, reorderItems)

    for (const m of merged) {
      // Reserve the merged quantity; if stock is short, clamp to whatever's available.
      let granted = m.quantity
      if (isMenuApiConfigured()) {
        try {
          await reserveItem(m.menuId, m.quantity)
        } catch (e) {
          if (e instanceof StockError) {
            granted = Math.max(0, e.remaining)
            if (granted > 0) {
              try { await reserveItem(m.menuId, granted) } catch { granted = 0 }
            }
          }
          // Non-stock (network) errors fall through and apply the merge locally.
        }
      }

      const existing = state.lines[m.menuId]
      if (granted <= 0) {
        if (existing) delete state.lines[m.menuId]
        continue
      }
      if (existing) {
        existing.qty = granted
      } else {
        const item = (resolveItem && resolveItem(m.menuId)) || {
          id: m.menuId,
          name: m.name,
          price: m.price,
        }
        state.lines[m.menuId] = { item, qty: granted }
      }
    }
  }

  // Reducing/removing applies locally first (snappy) and releases the hold in the
  // background; a release failure is harmless because holds also expire via TTL.
  async function decrement(id) {
    const line = state.lines[id]
    if (!line) return
    const next = line.qty - 1

    if (next <= 0) {
      if (line.item) setAvailable(line.item, (line.item.availableStock ?? 0) + line.qty)
      delete state.lines[id]
      if (isMenuApiConfigured()) { try { await releaseItem(id) } catch { /* TTL cleans up */ } }
      return
    }

    if (line.item) setAvailable(line.item, (line.item.availableStock ?? 0) + 1)
    line.qty = next
    if (isMenuApiConfigured()) { try { await reserveItem(id, next) } catch { /* poll corrects */ } }
  }

  async function remove(id) {
    const line = state.lines[id]
    if (line?.item) setAvailable(line.item, (line.item.availableStock ?? 0) + (line.qty ?? 0))
    delete state.lines[id]
    if (isMenuApiConfigured()) { try { await releaseItem(id) } catch { /* TTL cleans up */ } }
  }

  async function clear() {
    const held = Object.values(state.lines)
    state.lines = {}
    if (isMenuApiConfigured()) {
      await Promise.allSettled(held.map((l) => releaseItem(l.item.id)))
    }
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
