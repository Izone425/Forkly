import { describe, it, expect } from 'vitest'
import { selectActiveOrders, isActiveOrder, UNPAID_TTL_MS } from './activeOrders.js'

// Fixed "now" so every rule is asserted against an explicit clock.
const NOW = Date.parse('2026-07-13T12:00:00Z')
const ago = (ms) => new Date(NOW - ms).toISOString()

function order(overrides = {}) {
  return {
    id: 1,
    reference: 'FRK-0001',
    status: 'Pending',
    paymentStatus: 'Paid',
    placedAt: ago(5 * 60_000),
    total: 42,
    ...overrides,
  }
}

describe('isActiveOrder', () => {
  it('keeps every in-flight fulfilment status', () => {
    // `Completed` renders as "Ready": the kitchen is done but the rider hasn't collected,
    // so the order is very much still in flight and must stay in the tracker.
    for (const status of ['Pending', 'Preparing', 'Completed', 'OutForDelivery']) {
      expect(isActiveOrder(order({ status }), NOW)).toBe(true)
    }
  })

  it('drops the terminal states — the tracker is for orders still in flight', () => {
    expect(isActiveOrder(order({ status: 'Delivered' }), NOW)).toBe(false)
    expect(isActiveOrder(order({ status: 'Cancelled' }), NOW)).toBe(false)
  })

  it('keeps a recent unpaid order so the customer can still pay', () => {
    const abandoned = order({ paymentStatus: 'Unpaid', placedAt: ago(UNPAID_TTL_MS - 60_000) })
    expect(isActiveOrder(abandoned, NOW)).toBe(true)
  })

  it('drops an unpaid order once the checkout has gone stale', () => {
    const abandoned = order({ paymentStatus: 'Unpaid', placedAt: ago(UNPAID_TTL_MS + 60_000) })
    expect(isActiveOrder(abandoned, NOW)).toBe(false)
  })

  it('does not apply the unpaid TTL to a paid order', () => {
    const old = order({ paymentStatus: 'Paid', placedAt: ago(UNPAID_TTL_MS * 3) })
    expect(isActiveOrder(old, NOW)).toBe(true)
  })

  it('tolerates missing or unparseable timestamps', () => {
    // A timestamp we can't read must not silently hide an order the customer can still pay.
    expect(() => isActiveOrder(order({ paymentStatus: 'Unpaid', placedAt: null }), NOW)).not.toThrow()
    expect(isActiveOrder(order({ paymentStatus: 'Unpaid', placedAt: null }), NOW)).toBe(true)
    expect(isActiveOrder(order({ paymentStatus: 'Unpaid', placedAt: 'not-a-date' }), NOW)).toBe(true)
  })
})

describe('selectActiveOrders', () => {
  it('returns an empty list for empty or invalid input', () => {
    expect(selectActiveOrders([], NOW)).toEqual([])
    expect(selectActiveOrders(null, NOW)).toEqual([])
    expect(selectActiveOrders(undefined, NOW)).toEqual([])
  })

  it('sorts out-for-delivery first, then unpaid, then the rest', () => {
    const orders = [
      order({ id: 1, status: 'Preparing' }),
      order({ id: 2, status: 'Pending', paymentStatus: 'Unpaid' }),
      order({ id: 3, status: 'OutForDelivery' }),
    ]

    expect(selectActiveOrders(orders, NOW).map((o) => o.id)).toEqual([3, 2, 1])
  })

  it('breaks ties newest-first', () => {
    const orders = [
      order({ id: 1, status: 'Preparing', placedAt: ago(30 * 60_000) }),
      order({ id: 2, status: 'Preparing', placedAt: ago(2 * 60_000) }),
    ]

    expect(selectActiveOrders(orders, NOW).map((o) => o.id)).toEqual([2, 1])
  })

  it('filters a realistic history down to the orders worth surfacing', () => {
    const orders = [
      order({ id: 1, status: 'Delivered' }), // long delivered
      order({ id: 2, status: 'Cancelled' }),
      order({ id: 3, status: 'OutForDelivery' }),
      order({ id: 4, status: 'Pending', paymentStatus: 'Unpaid', placedAt: ago(UNPAID_TTL_MS * 2) }),
      order({ id: 5, status: 'Delivered', placedAt: ago(1000) }), // delivered seconds ago
    ]

    // No grace window: order 5 was delivered moments ago and is already gone.
    expect(selectActiveOrders(orders, NOW).map((o) => o.id)).toEqual([3])
  })
})
