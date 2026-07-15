// =========================================================
// Forkly — order status presentation helpers
//
// Shared by the account order history (ProfileView) and the floating
// active-orders panel, so both render an order's status the same way.
// The matching .badge-* classes live in style.css.
// =========================================================

import { config } from '../config.js'

// Fulfilment status (payment is tracked separately below).
//
// `Completed` is the Order service's name for "the kitchen is done, the rider hasn't
// collected yet" — NOT "the order is finished". To a customer that reads as done, so we
// render it as "Ready", matching the kitchen board (KitchenView) and the Tracker's own
// timeline step. Such an order is still in flight and stays in the active-orders bubble.
export const STATUS_LABELS = {
  Pending: 'Pending',
  Preparing: 'Preparing',
  Completed: 'Ready',
  OutForDelivery: 'Out for delivery',
  Delivered: 'Delivered',
  Cancelled: 'Cancelled',
}
export const statusLabel = (s) => STATUS_LABELS[s] || s
export const statusClass = (s) => `badge-${(s || '').toLowerCase()}`

// Payment status — separate from fulfilment, so a paid order keeps showing "Paid".
export const PAYMENT_LABELS = { Unpaid: 'Unpaid', Paid: 'Paid' }
export const paymentLabel = (s) => PAYMENT_LABELS[s] || s
export const paymentClass = (s) => `badge-pay-${(s || '').toLowerCase()}`

// Show "Pay now" only when the order still needs paying AND the payment service is
// live — without the config gate the link lands on a "not configured" payment page.
export const canPay = (o) =>
  config.paymentReady && o.paymentStatus !== 'Paid' && o.status !== 'Cancelled'

export function money(amount, currency = 'MYR') {
  const symbol = currency === 'MYR' ? 'RM' : `${currency} `
  return `${symbol}${Number(amount || 0).toFixed(2)}`
}

// ---------------------------------------------------------------------------
// Phases — what the customer is *shown* an order is doing.
//
// A phase folds `status` + `paymentStatus` into one presentational key. The keys
// deliberately match the Tracker's own timeline step keys (confirmed/preparing/ready/
// out/delivered), so the same map drives the active-orders panel AND the tracking
// timeline with no translation layer. `unpaid` and `cancelled` have no timeline step.
//
// The motion for each phase lives in components/StatusIcon.vue.
// ---------------------------------------------------------------------------
export const PHASES = {
  unpaid: { icon: '💳', label: 'Payment needed' },
  confirmed: { icon: '⏳', label: 'Waiting for the kitchen' },
  preparing: { icon: '👨‍🍳', label: 'Chef is cooking' },
  ready: { icon: '🍽️', label: 'Ready' },
  out: { icon: '🛵', label: 'On the way' },
  delivered: { icon: '🎉', label: 'Delivered' },
  cancelled: { icon: '✖️', label: 'Cancelled' },
}

/**
 * Which phase is this order in?
 *
 * @param {object} order  as mapped by orderGateway.fetchOrderHistory()
 * @returns {string|null} a key of PHASES
 */
export function phaseOf(order) {
  if (!order) return null

  // Terminal first — a cancelled order is cancelled whether or not it was ever paid.
  if (order.status === 'Cancelled') return 'cancelled'
  if (order.status === 'Delivered') return 'delivered'

  // Unpaid outranks the fulfilment status: paying is the action the customer needs to
  // take, whatever the kitchen happens to think the order is doing.
  if (order.paymentStatus !== 'Paid') return 'unpaid'

  switch (order.status) {
    case 'Preparing':
      return 'preparing'
    case 'Completed':
      return 'ready' // "Completed" = kitchen done, rider hasn't collected
    case 'OutForDelivery':
      return 'out'
    default:
      return 'confirmed' // Pending, paid — waiting for the kitchen to start
  }
}

export const phaseIcon = (phase) => PHASES[phase]?.icon ?? ''
export const phaseLabel = (phase) => PHASES[phase]?.label ?? ''
