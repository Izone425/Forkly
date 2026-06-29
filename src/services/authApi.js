// REST client for the Forkly .NET API.
//
// The API also speaks gRPC-web on the same origin. To swap this for a gRPC-web
// client you'd generate a stub from Forkly-Api/Protos/auth.proto with
// protoc-gen-grpc-web and call AuthService.Login/Register here instead — the
// rest of the app (views, handoff) stays unchanged. Left as a training exercise.

import { config } from '../config.js'

const TOKEN_KEY = 'forkly.auth.token'

export function getToken() {
  return localStorage.getItem(TOKEN_KEY) || ''
}

export function setToken(token) {
  if (token) localStorage.setItem(TOKEN_KEY, token)
}

export function clearToken() {
  localStorage.removeItem(TOKEN_KEY)
}

// Pull a human-readable message out of the API's error payloads.
// REST errors look like { errors: ["..."] } (see AuthController.ToResponse).
async function readError(res) {
  try {
    const body = await res.json()
    if (Array.isArray(body?.errors) && body.errors.length) return body.errors.join(' ')
    if (typeof body?.title === 'string') return body.title
  } catch {
    /* non-JSON body */
  }
  if (res.status === 401) return 'Invalid email or password.'
  if (res.status === 409) return 'Email is already registered.'
  return `Request failed (${res.status}).`
}

async function post(path, payload) {
  const res = await fetch(`${config.apiBase}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  })
  if (!res.ok) throw new Error(await readError(res))
  return res.json()
}

export async function register({ fullName, email, password }) {
  const auth = await post('/api/auth/register', { fullName, email, password })
  setToken(auth.token)
  return auth
}

export async function login({ email, password }) {
  const auth = await post('/api/auth/login', { email, password })
  setToken(auth.token)
  return auth
}

export async function me() {
  const res = await fetch(`${config.apiBase}/api/auth/me`, {
    headers: { Authorization: `Bearer ${getToken()}` },
  })
  if (!res.ok) throw new Error(await readError(res))
  return res.json()
}

// Authenticated JSON request with the stored bearer token.
async function authed(path, method, body) {
  const res = await fetch(`${config.apiBase}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${getToken()}`,
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!res.ok) throw new Error(await readError(res))
  // 204 No Content (e.g. change-password) has no body.
  return res.status === 204 ? null : res.json()
}

export function updateProfile(payload) {
  return authed('/api/auth/me', 'PUT', payload)
}

// ---- Delivery addresses ----
// Each mutation returns the full updated user (UserDto with the addresses array).
export function addAddress(payload) {
  return authed('/api/auth/me/addresses', 'POST', payload)
}

export function updateAddress(id, payload) {
  return authed(`/api/auth/me/addresses/${id}`, 'PUT', payload)
}

export function deleteAddress(id) {
  return authed(`/api/auth/me/addresses/${id}`, 'DELETE')
}

export function setDefaultAddress(id) {
  return authed(`/api/auth/me/addresses/${id}/default`, 'PUT')
}

// ---- Order history (read-only) ----
// Returns the signed-in user's orders, newest first. The separate ordering
// module owns order creation; this only reads.
export function listOrders() {
  return authed('/api/orders', 'GET')
}

export function changePassword({ currentPassword, newPassword }) {
  return authed('/api/auth/change-password', 'POST', { currentPassword, newPassword })
}

export async function uploadAvatar(file) {
  const form = new FormData()
  form.append('file', file)
  const res = await fetch(`${config.apiBase}/api/auth/me/avatar`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${getToken()}` }, // no Content-Type: browser sets multipart boundary
    body: form,
  })
  if (!res.ok) throw new Error(await readError(res))
  return res.json()
}

// Turn a relative avatar path (/uploads/x.png) into an absolute URL on the API.
export function absoluteUrl(path) {
  if (!path) return ''
  return /^https?:\/\//i.test(path) ? path : `${config.apiBase}${path}`
}
