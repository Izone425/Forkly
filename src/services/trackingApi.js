// =========================================================
// Forkly — Tracking API gateway (Alia's Notification & Tracker service)
//
// The customer's order-tracking screen polls this. It's a BFF over the Order
// service (the single source of truth for status): it returns the live status,
// progress steps, and a MOCK delivery ETA, and auto-marks the order Delivered
// once the timer elapses.
// =========================================================

import { config } from '../config.js'
import { getToken } from './authApi.js'

export function isTrackerApiConfigured() {
  return Boolean(config.trackerApiBase)
}

export async function fetchTracking(orderId) {
  const token = getToken()
  const base = config.trackerApiBase.replace(/\/+$/, '')
  const res = await fetch(`${base}/api/tracking/${orderId}`, {
    headers: { ...(token ? { Authorization: `Bearer ${token}` } : {}) },
  })
  if (!res.ok) {
    const e = new Error(`Tracker responded ${res.status}`)
    e.status = res.status
    throw e
  }
  return res.json()
}
