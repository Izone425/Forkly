// =========================================================
// Forkly — Admin API gateway.
//
// Used only by the /admin screens. Reuses the same bearer-token storage as
// authApi.js (getToken). User management lives on the Forkly .NET API
// (config.apiBase); order management lives on the Order Service
// (config.orderApiBase). Every endpoint here is admin-only server-side
// ([Authorize(Roles="admin")]) — these calls return 403 for non-admins.
// =========================================================

import { config } from '../config.js'
import { getToken } from './authApi.js'

// Mirror authApi.readError, plus a clear message for the admin 403 case.
async function readError(res) {
  try {
    const body = await res.json()
    if (typeof body?.error === 'string') return body.error
    if (Array.isArray(body?.errors) && body.errors.length) return body.errors.join(' ')
    if (typeof body?.title === 'string') return body.title
  } catch {
    /* non-JSON body */
  }
  if (res.status === 401) return 'Your session has expired. Please sign in again.'
  if (res.status === 403) return 'Admin access required.'
  return `Request failed (${res.status}).`
}

function qs(params) {
  const usp = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') usp.append(key, value)
  }
  const s = usp.toString()
  return s ? `?${s}` : ''
}

async function request(base, path, { method = 'GET', body } = {}) {
  const res = await fetch(`${base.replace(/\/+$/, '')}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${getToken()}`,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!res.ok) throw new Error(await readError(res))
  return res.status === 204 ? null : res.json()
}

// ---- Users (Forkly-Api) ----

export function listUsers({ search, page = 1, pageSize = 20 } = {}) {
  return request(config.apiBase, `/api/admin/users${qs({ search, page, pageSize })}`)
}

export function getUser(id) {
  return request(config.apiBase, `/api/admin/users/${id}`)
}

// Promote (makeAdmin=true) or demote (false). Returns the updated user row.
export function setUserAdmin(id, makeAdmin) {
  return request(config.apiBase, `/api/admin/users/${id}/roles/admin`, {
    method: makeAdmin ? 'POST' : 'DELETE',
  })
}

// Disable (true) locks the account out of login; enable (false) restores it.
export function setUserDisabled(id, disabled) {
  return request(config.apiBase, `/api/admin/users/${id}/${disabled ? 'disable' : 'enable'}`, {
    method: 'POST',
  })
}

// ---- Orders (Forkly.OrderService) ----

export function isOrdersApiConfigured() {
  return Boolean(config.orderApiBase)
}

// Returns a PagedResult plus isLive. Mirrors reportApi's graceful fallback: when no
// Order Service is configured, return an empty page (isLive:false) so the admin
// Orders screen renders in local/demo mode instead of erroring.
export async function listAllOrders({ status, userId, page = 1, pageSize = 20 } = {}) {
  if (!isOrdersApiConfigured()) {
    return { items: [], total: 0, page, pageSize, isLive: false }
  }
  const data = await request(
    config.orderApiBase,
    `/api/orders/admin/all${qs({ status, userId, page, pageSize })}`,
  )
  return { ...data, isLive: true }
}

export function updateOrderStatus(orderId, status) {
  return request(config.orderApiBase, `/api/orders/${orderId}/status`, {
    method: 'PATCH',
    body: { status },
  })
}
