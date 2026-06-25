// =========================================================
// Forkly — Order history API
//
// Returns the signed-in user's previous orders. Currently backed by mock data;
// the shape matches the order microservice so this is the single place to wire
// the real call later.
// =========================================================

import { config } from '../config.js'
import { MOCK_ORDER_HISTORY } from '../data/orderHistory.js'

/**
 * Fetch order history for a user.
 *
 * @param {string} [userId]
 * @returns {Promise<Array>} list of orders (newest first)
 */
export async function getOrderHistory(userId) {
  // TODO(API): when the order service exposes history, replace the mock with:
  //   const res = await fetch(`${config.orderApiBase}/v1/users/${userId}/orders`)
  //   if (!res.ok) throw new Error(`Order history responded ${res.status}`)
  //   return await res.json()
  void config // referenced so the integration point is obvious

  const orders = userId
    ? MOCK_ORDER_HISTORY.filter((o) => o.userId === userId)
    : MOCK_ORDER_HISTORY

  // Newest first.
  const sorted = [...orders].sort((a, b) => (a.orderDate < b.orderDate ? 1 : -1))
  return Promise.resolve(sorted)
}
