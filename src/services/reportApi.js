// =========================================================
// Forkly — Sales report gateway (admin report page).
//
// OUR side of the contract. The monthly sales report is derived from order data,
// so it belongs to the Order Service. Until VITE_ORDER_API_BASE is set (and the
// report endpoint exists), this returns the bundled MOCK data so the admin report
// page can be previewed end-to-end.
//
// Future real source: GET {orderApiBase}/api/orders/reports/monthly (admin only).
// =========================================================

import { config } from '../config.js'
import { getToken } from './authApi.js'
import { SALES_REPORT } from '../data/salesReport.js'

export function isReportApiConfigured() {
  return Boolean(config.orderApiBase)
}

// Returns { report, isLive }. isLive is false whenever the bundled mock data is
// used (no API configured, or the live call failed), so the UI can label it.
export async function getMonthlySalesReport(signal) {
  if (!isReportApiConfigured()) return { report: SALES_REPORT, isLive: false }

  try {
    const base = config.orderApiBase.replace(/\/+$/, '')
    const token = getToken()
    const res = await fetch(`${base}/api/orders/reports/monthly`, {
      signal,
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    })
    if (!res.ok) throw new Error(`Report API responded ${res.status}`)
    return { report: await res.json(), isLive: true }
  } catch {
    // Graceful fallback so the admin page still renders.
    return { report: SALES_REPORT, isLive: false }
  }
}
