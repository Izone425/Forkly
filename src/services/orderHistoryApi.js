// =========================================================
// Forkly — Order history API (order page "Most Recent Orders")
//
// Reads the signed-in user's orders from the ORDER service (Hanif) — the single
// source of truth for orders + status. The User API keeps a separate (empty)
// table, so history must be read here. Mapped to the shape OrderHistoryCard
// expects: { orderId, userId, orderDate, status, totalAmount, orderItems }.
// =========================================================

import { config } from '../config.js'
import { getToken } from './authApi.js'

// OrderResponse (Order service) -> the order-history card shape.
function mapOrder(o) {
  return {
    orderId: o.reference || `FRK-${o.id}`,
    orderNumericId: o.id,
    userId: o.userId,
    orderDate: o.createdAt,
    status: o.status,
    paymentStatus: o.paymentStatus,
    totalAmount: Number(o.total || 0),
    orderItems: (o.items || []).map((i) => ({
      menuId: i.menuId,
      menuName: i.itemName,
      quantity: i.quantity,
      price: Number(i.price || 0),
    })),
  }
}

async function getJson(path) {
  const base = config.orderApiBase.replace(/\/+$/, '')
  if (!base) return null
  const token = getToken()
  const res = await fetch(`${base}${path}`, {
    headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
  })
  if (!res.ok) throw new Error(`Order service responded ${res.status}`)
  return res.json()
}

/**
 * The signed-in user's most recent orders (newest first, top 3).
 * @param {number} userId  integer user id (UserDto.id / JWT `sub`)
 */
export async function getOrderHistory(userId) {
  if (!config.orderApiBase || userId == null) return []
  const data = await getJson(`/api/orders/user/${userId}/recent`)
  return (Array.isArray(data) ? data : []).map(mapOrder)
}

/**
 * Full details for one order. `orderId` is the numeric Order service id.
 */
export async function getOrderDetails(orderId) {
  const data = await getJson(`/api/orders/${orderId}`)
  return data ? mapOrder(data) : null
}
