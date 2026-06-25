// =========================================================
// Forkly — Order submission gateway
//
// OUR side of the contract with the order microservice. The order page builds
// the cart locally; this module is the single place that "places" an order.
//
// Until the order service exists (VITE_ORDER_API_BASE unset), submitOrder()
// runs in DEMO mode: it returns a simulated confirmation so the UI flow can be
// demonstrated end-to-end. Wire the real REST/gRPC-web call when ready.
// =========================================================

import { config } from '../config.js'

export function isOrderApiConfigured() {
  return Boolean(config.orderApiBase)
}

/**
 * Turn the reactive cart into a plain, serializable payload.
 * @param {Array<{item: object, qty: number}>} lines
 */
export function buildOrderPayload(lines) {
  return {
    source: 'forkly-web',
    currency: 'MYR',
    items: lines.map((l) => ({
      id: l.item.id,
      name: l.item.name,
      unitPrice: l.item.price,
      quantity: l.qty,
    })),
  }
}

/**
 * Submit an order.
 *
 * The .NET Order service prices the cart server-side (from the menu service),
 * creates a payment, and returns a `paymentRedirectUrl` pointing at the payment
 * page. The UI uses that to hand the customer off to payment.
 *
 * @param {object} payload  result of buildOrderPayload()
 * @returns {Promise<{ok: boolean, orderId: string, simulated: boolean, paymentRedirectUrl: string|null}>}
 */
export async function submitOrder(payload) {
  // DEMO mode — no order service configured yet.
  if (!isOrderApiConfigured()) {
    const orderId = 'DEMO-' + Date.now().toString(36).toUpperCase()
    return { ok: true, orderId, simulated: true, paymentRedirectUrl: null }
  }

  // Real submission to the Order service REST API.
  const res = await fetch(`${config.orderApiBase}/v1/orders`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
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

  const data = await res.json()
  return {
    ok: true,
    orderId: data.orderId,
    simulated: false,
    paymentRedirectUrl: data.paymentRedirectUrl ?? null,
  }
}
