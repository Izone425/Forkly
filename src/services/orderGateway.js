// =========================================================
// Forkly — Order submission gateway
//
// OUR side of the contract with the Order microservice (Hanif). The order page
// builds the cart locally; this module is the single place that "places" an order.
//
// When VITE_ORDER_API_BASE is set, submitOrder() POSTs to the real Order service
// (POST /api/orders) and returns a redirect to the payment page. When unset, it
// runs in DEMO mode so the UI flow still works without a backend.
// =========================================================

import { config } from '../config.js'
import { getToken } from './authApi.js'
import { getSessionId } from './session.js'

export function isOrderApiConfigured() {
  return Boolean(config.orderApiBase)
}

/**
 * Turn the reactive cart into the Order service's CreateOrderRequest shape.
 * The server recomputes subtotal/SST/total from these lines — client totals are ignored.
 *
 * @param {Array<{item: object, qty: number}>} lines
 * @param {object|null} [deliveryAddress] chosen address snapshot (Order service
 *   ignores unknown fields today; kept for when it persists the address).
 */
export function buildOrderPayload(lines, deliveryAddress = null) {
  return {
    source: 'forkly-web',
    currency: 'MYR',
    // Cart/session that held the stock — the Order service passes this to the Menu
    // service at checkout to decrement stock and release these holds.
    sessionId: getSessionId(),
    items: lines.map((l) => ({
      menuId: Number(l.item.id) || 0,
      itemName: l.item.name,
      price: l.item.price,
      quantity: l.qty,
    })),
    deliveryAddress: deliveryAddress || null,
  }
}

/**
 * Submit an order to the Order service, then hand the customer to payment.
 *
 * @param {object} payload  result of buildOrderPayload()
 * @returns {Promise<{ok, orderId, reference, total, simulated, paymentRedirectUrl}>}
 */
export async function submitOrder(payload) {
  // DEMO mode — no Order service configured.
  if (!isOrderApiConfigured()) {
    const ref = 'DEMO-' + Date.now().toString(36).toUpperCase()
    return { ok: true, orderId: ref, reference: ref, total: null, simulated: true, paymentRedirectUrl: null }
  }

  const token = getToken()
  const base = config.orderApiBase.replace(/\/+$/, '')
  const res = await fetch(`${base}/api/orders`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: JSON.stringify(payload),
  })
  if (!res.ok) {
    let message = `Order service responded ${res.status}`
    try {
      const err = await res.json()
      if (err?.error) message = err.error
    } catch { /* keep default */ }
    throw new Error(message)
  }

  // OrderResponse { id, reference, subtotal, sst, total, status, items, ... }
  const data = await res.json()
  return {
    ok: true,
    orderId: data.id,
    reference: data.reference,
    total: data.total,
    simulated: false,
    // Hand off to Aiman's payment page; the order id travels in the path.
    paymentRedirectUrl: `/payment/${data.id}`,
  }
}

/**
 * Read the signed-in user's order history from the Order service (the single
 * source of truth for orders). Returns them mapped to the shape the account
 * page renders. The Order service owns creation + status, so history must be
 * read here — NOT from the User API, which keeps a separate (empty) table.
 *
 * @param {number} userId  integer user id (UserDto.id / JWT `sub`)
 * @returns {Promise<Array>} orders newest-first, or [] when unconfigured
 */
export async function fetchOrderHistory(userId) {
  if (!isOrderApiConfigured() || !userId) return []

  const token = getToken()
  const base = config.orderApiBase.replace(/\/+$/, '')
  const res = await fetch(`${base}/api/orders/user/${userId}`, {
    headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
  })
  if (!res.ok) throw new Error(`Order service responded ${res.status}`)

  // Map OrderResponse -> the fields ProfileView's order history expects.
  const data = await res.json()
  return data.map((o) => ({
    id: o.id,
    reference: o.reference,
    status: o.status,
    paymentStatus: o.paymentStatus,
    placedAt: o.createdAt,
    total: o.total,
    currency: o.currency,
    items: (o.items || []).map((it) => ({
      name: it.itemName,
      unitPrice: it.price,
      quantity: it.quantity,
    })),
  }))
}
