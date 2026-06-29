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
  // TODO: Fetch recent orders list
  // GET /orders/user/{userId}/recent
  // (or the order service's history endpoint, e.g.
  //  GET {orderApiBase}/v1/users/{userId}/orders)
  void config // referenced so the integration point is obvious

  const orders = userId
    ? MOCK_ORDER_HISTORY.filter((o) => o.userId === userId)
    : MOCK_ORDER_HISTORY

  // Newest first.
  const sorted = [...orders].sort((a, b) => (a.orderDate < b.orderDate ? 1 : -1))
  return Promise.resolve(sorted)
}

/**
 * Fetch full details for one order (used by reorder / details view).
 * @param {string} orderId
 * @returns {Promise<object|null>}
 */
export async function getOrderDetails(orderId) {
  // TODO: Fetch recent order for reorder
  // GET /orders/{orderId}
  // OR gRPC: OrderService.GetOrderDetails(orderId)
  void config
  return Promise.resolve(MOCK_ORDER_HISTORY.find((o) => o.orderId === orderId) ?? null)
}
