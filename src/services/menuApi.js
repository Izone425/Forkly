// =========================================================
// Forkly — Menu API gateway (Amirul's Menu service).
//
// Single place that talks to the MENU microservice (Forkly.MenuService, REST on
// http://localhost:5100). The landing page and the order page render the buyer
// menu through fetchMenu(); the /admin/menu screen manages items through the
// admin functions below.
//
// Buyer reads are public. Admin writes are admin-only server-side
// ([Authorize(Roles="admin")] — JWT issued by the User service), so they reuse the
// same bearer-token storage as authApi.js and return 401/403 without an admin token.
//
// When VITE_MENU_API_BASE is unset the menu store falls back to the bundled sample
// menu (src/data/menu.js) so the pages keep working.
//
// Contract (GET {menuApiBase}/api/menu -> array of items). The Menu service exposes
// the price as "unitPrice" and the picture as "imageUrl"; legacy field-name variants
// are still tolerated so we never break on small naming differences:
//   {
//     "id":          1,
//     "name":        "Classic Beef Burger",
//     "description": "Grilled halal beef patty…",
//     "unitPrice":   18.90,
//     "imageUrl":    "https://images.unsplash.com/…?w=1200&q=80",
//     "category":    "Burger",
//     "availability": true
//   }
// =========================================================

import { config } from '../config.js'
import { getToken } from './authApi.js'

export function isMenuApiConfigured() {
  return Boolean(config.menuApiBase)
}

function base() {
  return config.menuApiBase.replace(/\/+$/, '')
}

// Uploaded pictures are served by the Menu service as a relative path
// (/api/menu/{id}/image?v=…); legacy items may still carry an absolute CDN URL.
// Resolve either to something an <img> can load.
export function menuImageUrl(path) {
  if (!path) return null
  return /^https?:\/\//i.test(path) ? path : `${base()}${path}`
}

// Map one raw record from the Menu service into the stable shape the buyer UI uses:
//   { id, name, description, price, image, emoji, category, available, stockQuantity }
export function normalizeMenuItem(raw) {
  return {
    id: raw.id ?? raw.menuId ?? raw._id ?? raw.code,
    categoryId: raw.categoryId ?? null,
    name: raw.name ?? raw.itemName ?? '',
    description: raw.description ?? raw.desc ?? '',
    price: Number(raw.price ?? raw.unitPrice ?? 0),
    image: menuImageUrl(raw.image ?? raw.imageUrl ?? raw.picture ?? raw.photo ?? null),
    emoji: raw.emoji ?? null, // visual fallback when there is no picture
    category: raw.category ?? 'Menu',
    stockQuantity: raw.stockQuantity ?? null,
    // The service field is "availability"; tolerate the older "available" too.
    available: (raw.availability ?? raw.available) !== false,
  }
}

// Fetch the live buyer menu and return UI-ready items. Throws on network/HTTP error
// so the caller (menu store) can fall back to the bundled menu.
export async function fetchMenu(signal) {
  const res = await fetch(`${base()}/api/menu`, { signal })
  if (!res.ok) throw new Error(`Menu service responded ${res.status}`)

  const data = await res.json()
  const list = Array.isArray(data) ? data : (data.items ?? [])

  return list
    .map(normalizeMenuItem)
    .filter((i) => i.id != null && i.available)
}

// ---------- Admin (write) API ----------
// These hit the same Menu service but require an admin bearer token.

async function readError(res) {
  try {
    const body = await res.json()
    if (typeof body?.error === 'string') return body.error
    if (Array.isArray(body?.errors) && body.errors.length) return body.errors.join(' ')
    if (body?.errors && typeof body.errors === 'object') {
      const flat = Object.values(body.errors).flat()
      if (flat.length) return flat.join(' ')
    }
    if (typeof body?.title === 'string') return body.title
  } catch {
    /* non-JSON body */
  }
  if (res.status === 401) return 'Your session has expired. Please sign in again.'
  if (res.status === 403) return 'Admin access required.'
  return `Request failed (${res.status}).`
}

async function request(path, { method = 'GET', body, auth = false } = {}) {
  const headers = { 'Content-Type': 'application/json' }
  if (auth) headers.Authorization = `Bearer ${getToken()}`

  const res = await fetch(`${base()}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!res.ok) throw new Error(await readError(res))
  return res.status === 204 ? null : res.json()
}

// Categories — public read, used to populate the admin item form's dropdown.
export function getCategories() {
  return request('/api/categories')
}

// Admin listing: includes unavailable items (buyers only see available ones).
export function fetchAdminMenu() {
  return request('/api/menu/admin/all', { auth: true })
}

// payload: { categoryId, name, description, unitPrice, stockQuantity, availability }
// Pictures are uploaded separately via uploadMenuImage() and stored as bytes in the DB.
export function createMenuItem(payload) {
  return request('/api/menu', { method: 'POST', body: payload, auth: true })
}

export function updateMenuItem(id, payload) {
  return request(`/api/menu/${id}`, { method: 'PUT', body: payload, auth: true })
}

// Upload an item's picture (multipart). Mirrors authApi.uploadAvatar — the browser
// sets the multipart boundary, so we only add the Authorization header. Returns the
// updated item DTO.
export async function uploadMenuImage(id, file) {
  const form = new FormData()
  form.append('file', file)

  const res = await fetch(`${base()}/api/menu/${id}/image`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${getToken()}` },
    body: form,
  })
  if (!res.ok) throw new Error(await readError(res))
  return res.json()
}

export function setMenuItemAvailability(id, availability) {
  return request(`/api/menu/${id}/availability`, {
    method: 'PATCH',
    body: { availability },
    auth: true,
  })
}

export function deleteMenuItem(id) {
  return request(`/api/menu/${id}`, { method: 'DELETE', auth: true })
}
