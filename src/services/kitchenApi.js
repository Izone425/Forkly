// =========================================================
// Forkly — Kitchen API gateway (Zul's Kitchen service)
//
// The crew kitchen board talks to the Kitchen service (a BFF over the Order
// service). It reads the active queue and pushes crew status changes. Status
// itself lives in the Order service (single source of truth), which Alia's
// tracker reads for the customer.
// =========================================================

import { config } from '../config.js'
import { getToken } from './authApi.js'

export function isKitchenApiConfigured() {
  return Boolean(config.kitchenApiBase)
}

async function call(path, { method = 'GET', body } = {}) {
  const token = getToken()
  const base = config.kitchenApiBase.replace(/\/+$/, '')
  const res = await fetch(`${base}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!res.ok) {
    let message = `Kitchen service responded ${res.status}`
    try {
      const err = await res.json()
      if (err?.error) message = err.error
    } catch { /* keep default */ }
    const e = new Error(message)
    e.status = res.status
    throw e
  }
  return res.json()
}

// Active tickets for the board (New/Preparing/Completed/OutForDelivery), oldest first.
export function fetchQueue() {
  return call('/api/kitchen/queue')
}

// Advance a ticket: 'Preparing' | 'Completed' | 'OutForDelivery'.
export function setStatus(orderId, status) {
  return call(`/api/kitchen/orders/${orderId}/status`, { method: 'POST', body: { status } })
}
