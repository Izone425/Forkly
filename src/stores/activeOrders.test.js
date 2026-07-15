import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'

// The store talks to three collaborators; stub all of them so the test drives the
// polling/toast/race logic rather than the network.
vi.mock('../services/orderGateway.js', () => ({
  fetchOrderHistory: vi.fn(),
  isOrderApiConfigured: vi.fn(() => true),
}))
vi.mock('../services/trackingApi.js', () => ({
  fetchTracking: vi.fn(),
  isTrackerApiConfigured: vi.fn(() => true),
}))
vi.mock('./toast.js', () => {
  const show = vi.fn()
  return { useToast: () => ({ show }), __show: show }
})
vi.mock('./auth.js', () => {
  const { reactive } = require('vue')
  const state = reactive({ user: { id: '1' } })
  return { useAuth: () => ({ state }), __authState: state }
})

const { fetchOrderHistory, isOrderApiConfigured } = await import('../services/orderGateway.js')
const { fetchTracking, isTrackerApiConfigured } = await import('../services/trackingApi.js')
const { __show: show } = await import('./toast.js')
const { __authState: authState } = await import('./auth.js')
const { useActiveOrders, __resetForTests } = await import('./activeOrders.js')

const store = useActiveOrders()

const order = (o = {}) => ({
  id: 1,
  reference: 'FRK-0001',
  status: 'Preparing',
  paymentStatus: 'Paid',
  placedAt: new Date().toISOString(),
  total: 20,
  ...o,
})

beforeEach(async () => {
  // Restoring the signed-in user trips the store's auth watcher, which kicks off a
  // real poll. Let that settle and cancel it, so each test starts from a quiet store.
  fetchOrderHistory.mockResolvedValue([])
  authState.user = { id: '1' }
  await new Promise((r) => setTimeout(r, 0))
  __resetForTests()
  store.setViewedOrder(null) // route-driven in the app; reset it explicitly here

  vi.clearAllMocks()
  isOrderApiConfigured.mockReturnValue(true)
  isTrackerApiConfigured.mockReturnValue(true)
  fetchTracking.mockResolvedValue({ etaMinutes: 12 })
})

afterEach(() => __resetForTests())

describe('activeOrders store', () => {
  it('surfaces only the orders worth tracking', async () => {
    fetchOrderHistory.mockResolvedValue([
      order({ id: 1, status: 'Preparing' }),
      order({ id: 2, status: 'Cancelled' }),
    ])

    await store.refresh()

    expect(store.orders.value.map((o) => o.id)).toEqual([1])
    expect(store.count.value).toBe(1)
    expect(store.hasActive.value).toBe(true)
  })

  it('scopes the request to the signed-in user', async () => {
    fetchOrderHistory.mockResolvedValue([])
    authState.user = { id: '7' }

    await store.refresh()

    // The gateway takes an integer id (UserDto.id is a string).
    expect(fetchOrderHistory).toHaveBeenCalledWith(7)
  })

  it('does nothing when signed out', async () => {
    authState.user = null

    await store.refresh()

    expect(fetchOrderHistory).not.toHaveBeenCalled()
  })

  it('does nothing in demo mode (order API unconfigured)', async () => {
    isOrderApiConfigured.mockReturnValue(false)

    await store.refresh()

    expect(fetchOrderHistory).not.toHaveBeenCalled()
  })

  it('toasts a status advance, but not the first sighting of an order', async () => {
    fetchOrderHistory.mockResolvedValue([order({ status: 'Pending' })])
    await store.refresh()
    expect(show).not.toHaveBeenCalled() // first poll establishes a baseline

    fetchOrderHistory.mockResolvedValue([order({ status: 'Preparing' })])
    await store.refresh()
    expect(show).toHaveBeenCalledTimes(1)
    expect(show.mock.calls[0][0]).toContain('FRK-0001')

    // An unchanged status must stay quiet.
    await store.refresh()
    expect(show).toHaveBeenCalledTimes(1)
  })

  it('stays silent for the order already open on the tracking page', async () => {
    // TrackingView polls every 4s and toasts this transition itself.
    store.setViewedOrder('1')

    fetchOrderHistory.mockResolvedValue([order({ status: 'Pending' })])
    await store.refresh()

    fetchOrderHistory.mockResolvedValue([order({ status: 'OutForDelivery' })])
    await store.refresh()

    expect(show).not.toHaveBeenCalled()
  })

  it('announces delivery on the same poll that removes the order from the panel', async () => {
    // The trap: announce() used to walk only the ACTIVE list. A delivered order is no
    // longer active, so the row would drop off the bubble in total silence and the
    // customer would just watch the badge decrement.
    fetchOrderHistory.mockResolvedValue([order({ status: 'OutForDelivery' })])
    await store.refresh()
    expect(show).not.toHaveBeenCalled()

    fetchOrderHistory.mockResolvedValue([order({ status: 'Delivered' })])
    await store.refresh()

    expect(store.orders.value).toEqual([]) // the row is gone...
    expect(show).toHaveBeenCalledTimes(1) // ...but not silently
    expect(show.mock.calls[0][0]).toContain('Delivered')
  })

  it('announces a delivery exactly once, however long the tab stays open', async () => {
    // If announce() stops maintaining the entry once the order leaves the active list, its
    // prev stays 'OutForDelivery' against a status of 'Delivered' — and every subsequent
    // poll re-announces the same delivery, forever.
    fetchOrderHistory.mockResolvedValue([order({ status: 'OutForDelivery' })])
    await store.refresh()

    fetchOrderHistory.mockResolvedValue([order({ status: 'Delivered' })])
    await store.refresh()
    await store.refresh()
    await store.refresh()

    expect(show).toHaveBeenCalledTimes(1)
  })

  it('says nothing about orders that were already finished at sign-in', async () => {
    // announce() now walks the full history, so an old delivered order is in front of it.
    // It has no prevStatusById entry, so it must never toast.
    fetchOrderHistory.mockResolvedValue([order({ id: 9, status: 'Delivered' })])
    await store.refresh()
    await store.refresh()

    expect(show).not.toHaveBeenCalled()
    expect(store.orders.value).toEqual([])
  })

  it('announces a cancellation and drops the row', async () => {
    fetchOrderHistory.mockResolvedValue([order({ status: 'Preparing' })])
    await store.refresh()

    fetchOrderHistory.mockResolvedValue([order({ status: 'Cancelled' })])
    await store.refresh()

    expect(store.orders.value).toEqual([])
    expect(show.mock.calls[0][0]).toContain('Cancelled')
  })

  it('forgets the ETA of an order that has left the panel', async () => {
    fetchOrderHistory.mockResolvedValue([order({ status: 'OutForDelivery' })])
    await store.refresh()
    expect(store.etaFor(1)).toBe(12)

    fetchOrderHistory.mockResolvedValue([order({ status: 'Delivered' })])
    await store.refresh()

    expect(store.etaFor(1)).toBeNull() // a stale "~12 min" must not outlive its order
  })

  it('suppresses the delivery toast for the order open in the drawer', async () => {
    // The drawer's own OrderTracking announces it, in the Tracker's wording.
    store.openTracking(1)

    fetchOrderHistory.mockResolvedValue([order({ status: 'OutForDelivery' })])
    await store.refresh()

    fetchOrderHistory.mockResolvedValue([order({ status: 'Delivered' })])
    await store.refresh()

    expect(show).not.toHaveBeenCalled()
    expect(store.orders.value).toEqual([]) // still leaves the panel
  })

  it('stays silent for the order open in the tracking drawer', async () => {
    // The drawer causes no route change, so without this the store would announce a
    // status change that the drawer's own OrderTracking is already toasting.
    store.openTracking(1)

    fetchOrderHistory.mockResolvedValue([order({ status: 'Pending' })])
    await store.refresh()

    fetchOrderHistory.mockResolvedValue([order({ status: 'OutForDelivery' })])
    await store.refresh()

    expect(show).not.toHaveBeenCalled()
  })

  it('opening the drawer swaps out the panel, and sign-out clears it', () => {
    store.openPanel()
    store.openTracking(3)

    expect(store.state.drawerOrderId).toBe(3)
    expect(store.state.panelOpen).toBe(false) // drawer replaces the panel, never stacks

    __resetForTests()
    expect(store.state.drawerOrderId).toBeNull()
  })

  it('pulls an ETA only for orders out for delivery', async () => {
    fetchOrderHistory.mockResolvedValue([
      order({ id: 1, status: 'OutForDelivery' }),
      order({ id: 2, status: 'Preparing' }),
    ])

    await store.refresh()

    expect(fetchTracking).toHaveBeenCalledTimes(1)
    expect(fetchTracking).toHaveBeenCalledWith(1)
    expect(store.etaFor(1)).toBe(12)
    expect(store.etaFor(2)).toBeNull()
  })

  it('skips the tracker entirely when it is not configured', async () => {
    isTrackerApiConfigured.mockReturnValue(false)
    fetchOrderHistory.mockResolvedValue([order({ status: 'OutForDelivery' })])

    await store.refresh()

    expect(fetchTracking).not.toHaveBeenCalled()
    expect(store.orders.value).toHaveLength(1) // rows still render, just without an ETA
  })

  it('survives a tracker error on one order without blanking the panel', async () => {
    fetchOrderHistory.mockResolvedValue([
      order({ id: 1, status: 'OutForDelivery' }),
      order({ id: 2, status: 'OutForDelivery' }),
    ])
    fetchTracking.mockRejectedValueOnce(new Error('Tracker responded 404'))
    fetchTracking.mockResolvedValueOnce({ etaMinutes: 5 })

    await store.refresh()

    expect(store.orders.value).toHaveLength(2)
    expect(store.etaFor(2)).toBe(5)
  })

  it('keeps the last known list when a poll fails', async () => {
    fetchOrderHistory.mockResolvedValue([order()])
    await store.refresh()

    fetchOrderHistory.mockRejectedValue(new Error('network down'))
    await store.refresh()

    expect(store.orders.value).toHaveLength(1)
    expect(store.state.error).toBe('network down')
  })

  it('discards a response that lands after sign-out', async () => {
    let release
    fetchOrderHistory.mockReturnValue(new Promise((r) => { release = r }))

    const inFlight = store.refresh()
    __resetForTests() // user signs out while the request is on the wire
    release([order()])
    await inFlight

    expect(store.orders.value).toEqual([])
    expect(show).not.toHaveBeenCalled()
  })
})
