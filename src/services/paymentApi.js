// =========================================================
// Forkly — Payment API gateway (Aiman's Payment service)
//
// The single place the frontend talks to the PAYMENT microservice. It's a MOCK:
// no real charge happens. Flow:
//   1. checkout(orderId) -> the service reads the amount from the Order service
//      and returns a pending payment.
//   2. pay(paymentId, ...) -> the service "charges" (mock) and, on success, marks
//      the order Paid in the Order service.
// =========================================================

import { config } from '../config.js'
import { getToken } from './authApi.js'

export function isPaymentApiConfigured() {
  return Boolean(config.paymentApiBase)
}

async function call(path, { method = 'GET', body } = {}) {
  const token = getToken()
  const base = config.paymentApiBase.replace(/\/+$/, '')
  const res = await fetch(`${base}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!res.ok) {
    let message = `Payment service responded ${res.status}`
    try {
      const err = await res.json()
      if (err?.error) message = err.error
    } catch { /* keep default */ }
    throw new Error(message)
  }
  return res.json()
}

// Start (or reuse) a payment for an order created in the Order service.
export function startCheckout(orderId) {
  return call('/api/payments/checkout', { method: 'POST', body: { orderId: Number(orderId) } })
}

// Settle a payment. Pass { simulateFailure: true } to exercise the decline path.
export function payNow(paymentId, { method = 'card', cardLast4 = null, simulateFailure = false } = {}) {
  return call(`/api/payments/${paymentId}/pay`, {
    method: 'POST',
    body: { method, cardLast4, simulateFailure },
  })
}

export function getPayment(paymentId) {
  return call(`/api/payments/${paymentId}`)
}
