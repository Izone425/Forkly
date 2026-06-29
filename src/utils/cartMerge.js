// =========================================================
// Forkly — cart merge logic (pure, reusable, UI-agnostic)
//
// Reorder MERGES into the existing cart — it never replaces it.
//   - menuId already in cart  -> quantity += reorder quantity
//   - menuId not in cart       -> append as a new line
//
// Items use the shape: { menuId, name, quantity, price }
// Returns a NEW array; inputs are not mutated.
// =========================================================

export function mergeCartWithReorderItems(cartItems, reorderItems) {
  const merged = (cartItems || []).map((i) => ({ ...i }))
  const indexByMenuId = new Map(merged.map((i, idx) => [i.menuId, idx]))

  for (const r of reorderItems || []) {
    const idx = indexByMenuId.get(r.menuId)
    if (idx !== undefined) {
      // Exists -> increase quantity.
      merged[idx].quantity += r.quantity
    } else {
      // New -> push.
      indexByMenuId.set(r.menuId, merged.length)
      merged.push({ menuId: r.menuId, name: r.name, quantity: r.quantity, price: r.price })
    }
  }

  return merged
}
