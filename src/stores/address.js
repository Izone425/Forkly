// =========================================================
// Forkly — delivery address state (client-side)
//
// Holds the saved address book (from the User Service mock) and the address
// chosen for the CURRENT order. The selected value is the FULL address object,
// which is snapshotted into the order on checkout (not just an id).
// =========================================================

import { reactive } from 'vue'

const state = reactive({
  saved: [],
  loaded: false,
  selected: null, // full address object — the order snapshot
})

export function useDeliveryAddress() {
  function setSaved(list) {
    state.saved = list
    state.loaded = true
    // Default to the user's default address (or the first one).
    if (!state.selected && list.length) {
      state.selected = list.find((a) => a.isDefault) || list[0]
    }
  }

  function select(address) {
    state.selected = address
  }

  // Add a newly created address to the list and select it.
  // persisted=true means it was saved to the user profile (User Service);
  // false means it's a temporary address for this order only.
  function addAddress(address, { persisted } = {}) {
    const entry = { ...address, temporary: !persisted }
    state.saved.unshift(entry)
    state.selected = entry
    return entry
  }

  function isSelected(address) {
    return !!state.selected && state.selected.id === address.id
  }

  return { state, setSaved, select, addAddress, isSelected }
}
