// =========================================================
// Forkly — Menu API gateway (Amirul's Menu service)
//
// Single place that talks to the MENU microservice. The landing page and the
// order page both render the menu through this, so when Amirul's REST API is
// ready we only change it here.
//
// The Menu service is NOT built yet. Until VITE_MENU_API_BASE is set, the menu
// store falls back to the bundled sample menu (src/data/menu.js) so the pages
// keep working. This module just defines OUR side of the contract.
//
// Expected contract (GET {menuApiBase}/api/menu -> array of items). Field-name
// variants are tolerated so we don't break on Amirul's exact naming:
//   {
//     "menuId" | "id":        1,
//     "name":                 "Classic Burger",
//     "description":          "Beef patty, cheddar, lettuce & house sauce",
//     "unitPrice" | "price":  10.0,
//     "imageUrl" | "image" | "picture": "https://.../burger.jpg",
//     "category":             "Mains",     // optional
//     "available":            true          // optional, defaults true
//   }
// =========================================================

import { config } from '../config.js'

export function isMenuApiConfigured() {
  return Boolean(config.menuApiBase)
}

// Map one raw record from the Menu service into the stable shape the UI uses:
//   { id, name, description, price, image, emoji, category, available }
export function normalizeMenuItem(raw) {
  return {
    id: raw.id ?? raw.menuId ?? raw._id ?? raw.code,
    name: raw.name ?? raw.itemName ?? '',
    description: raw.description ?? raw.desc ?? '',
    price: Number(raw.price ?? raw.unitPrice ?? 0),
    image: raw.image ?? raw.imageUrl ?? raw.picture ?? raw.photo ?? null,
    emoji: raw.emoji ?? null, // visual fallback when there is no picture
    category: raw.category ?? 'Menu',
    available: raw.available !== false,
  }
}

// Fetch the live menu and return UI-ready items. Throws on network/HTTP error so
// the caller (menu store) can fall back to the bundled menu.
export async function fetchMenu(signal) {
  const base = config.menuApiBase.replace(/\/+$/, '')
  const res = await fetch(`${base}/api/menu`, { signal })
  if (!res.ok) throw new Error(`Menu service responded ${res.status}`)

  const data = await res.json()
  const list = Array.isArray(data) ? data : (data.items ?? [])

  return list
    .map(normalizeMenuItem)
    .filter((i) => i.id != null && i.available)
}
