// =========================================================
// Forkly — active-order selection (pure)
//
// Decides which of the user's orders the floating tracker should surface. Kept
// free of Vue/network so it can be unit-tested directly (see activeOrders.test.js).
//
// The Order service has no "my active orders" endpoint — the frontend reads the
// full history (GET /api/orders/user/{id}) and narrows it here.
// =========================================================

export const ACTIVE_STATUSES = ['Pending', 'Preparing', 'Completed', 'OutForDelivery']

// An order that was never paid for shouldn't badge the tracker forever. After this
// long we treat the checkout as abandoned and stop surfacing it.
export const UNPAID_TTL_MS = 24 * 60 * 60 * 1000

// Order service timestamps are DateTimeOffset, so the JSON always carries a zone
// and Date can parse them unambiguously. Returns null for missing/garbage input.
function toMs(iso) {
  if (!iso) return null
  const ms = new Date(iso).getTime()
  return Number.isNaN(ms) ? null : ms
}

const isUnpaid = (o) => o.paymentStatus !== 'Paid'

/**
 * Should this order appear in the floating tracker?
 *
 * @param {object} order  as mapped by orderGateway.fetchOrderHistory()
 * @param {number} nowMs  Date.now(), injected so the rules are testable
 */
export function isActiveOrder(order, nowMs) {
  if (!order) return false

  // Delivered and Cancelled are terminal, and neither is in ACTIVE_STATUSES — so this
  // one check drops both. A delivered order leaves the tracker immediately; the customer
  // is told by a toast, and the drawer (if open) stays up showing the delivered state.
  if (!ACTIVE_STATUSES.includes(order.status)) return false

  // Drop an abandoned checkout once it goes stale.
  if (isUnpaid(order)) {
    const placed = toMs(order.placedAt)
    if (placed != null && nowMs - placed > UNPAID_TTL_MS) return false
  }

  return true
}

// Most-urgent first: something arriving beats something that still needs paying,
// which beats everything else. Ties break newest-first.
function rank(o) {
  if (o.status === 'OutForDelivery') return 0
  if (isUnpaid(o)) return 1
  return 2
}

/**
 * Narrow a full order history down to the orders worth showing, most urgent first.
 *
 * @param {Array<object>} orders
 * @param {number} nowMs
 * @returns {Array<object>}
 */
export function selectActiveOrders(orders, nowMs) {
  if (!Array.isArray(orders)) return []

  return orders
    .filter((o) => isActiveOrder(o, nowMs))
    .sort((a, b) => {
      const byRank = rank(a) - rank(b)
      if (byRank !== 0) return byRank
      return (toMs(b.placedAt) ?? 0) - (toMs(a.placedAt) ?? 0)
    })
}
