// =========================================================
// Forkly — delivery address state (client-side, for the order page)
//
// Source of truth for SAVED addresses is the User Service (Izzuwan) — they ride
// in on auth.user.addresses and are pushed here via setPersisted(). This store
// never invents addresses; if the profile has none, the list is empty.
//
// "temporary" addresses are entered at checkout for THIS order only (the user
// chose not to save them to their profile); they are not persisted anywhere.
// The selected value is the FULL address object, snapshotted into the order.
// =========================================================

import { reactive, computed } from 'vue'

// A user may keep at most this many SAVED addresses (enforced client-side; the
// User Service does not cap them). Order-only "temporary" addresses don't count.
export const MAX_SAVED_ADDRESSES = 3

const state = reactive({
  persisted: [], // saved addresses from the user's profile (User Service)
  temporary: [], // order-only addresses (not saved)
  selected: null, // full address object — the order snapshot
  loaded: false,
})

function withFlag(list, temporary) {
  return (Array.isArray(list) ? list : []).map((a) => ({ ...a, temporary }))
}

export function useDeliveryAddress() {
  const all = computed(() => [...state.temporary, ...state.persisted])
  const savedCount = computed(() => state.persisted.length)
  const canSaveMore = computed(() => state.persisted.length < MAX_SAVED_ADDRESSES)
  const hasAny = computed(() => all.value.length > 0)

  // Replace the saved list from the user's profile, keeping any order-only
  // temporary addresses and a still-valid selection.
  function setPersisted(list) {
    state.persisted = withFlag(list, false)
    state.loaded = true

    // Re-point the selection at the refreshed object (or clear it if it's gone).
    if (state.selected) {
      state.selected = all.value.find((a) => a.id === state.selected.id) || null
    }
    // Nothing chosen yet -> default address, else first available.
    if (!state.selected) {
      state.selected =
        state.persisted.find((a) => a.isDefault) || state.persisted[0] || state.temporary[0] || null
    }
  }

  function select(address) {
    state.selected = address
  }

  function isSelected(address) {
    return !!state.selected && state.selected.id === address.id
  }

  // Add an order-only address (not saved to the profile) and select it.
  function addTemporary(address) {
    const entry = { ...address, id: 'temp-' + Math.random().toString(36).slice(2, 8), temporary: true }
    state.temporary.unshift(entry)
    state.selected = entry
    return entry
  }

  return { state, all, savedCount, canSaveMore, hasAny, setPersisted, select, isSelected, addTemporary }
}
