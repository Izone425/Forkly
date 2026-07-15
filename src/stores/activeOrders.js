// =========================================================
// Forkly — active-orders store (the floating tracker)
//
// Keeps the signed-in customer's in-flight orders on screen wherever they are in
// the app. Without this, /track/:orderId is a dead end: it's only reachable from
// the payment-success screen, so closing that page loses the order for good.
//
// There is no "my active orders" endpoint — we read the user's order history
// (GET /api/orders/user/{id}) and narrow it with the pure selector in
// utils/activeOrders.js.
//
// Deliberately NOT persisted to localStorage: a cached list can't be attributed to
// a user until /me resolves (wrong-user leak on a shared machine), and it goes
// stale the moment another device pays or takes delivery. Module scope already
// survives route changes, which is the case that matters.
// =========================================================

import { reactive, computed, watch } from 'vue'
import { fetchOrderHistory, isOrderApiConfigured } from '../services/orderGateway.js'
import { fetchTracking, isTrackerApiConfigured } from '../services/trackingApi.js'
import { selectActiveOrders } from '../utils/activeOrders.js'
import { statusLabel } from '../utils/orderStatus.js'
import { useAuth } from '../stores/auth.js'
import { useToast } from '../stores/toast.js'

const POLL_MS = 15000

const state = reactive({
  orders: [],
  etaById: {}, // orderId -> minutes remaining, for orders out for delivery
  loading: false,
  error: '',
  panelOpen: false,
  // The order whose tracking page is currently open, if any. FloatingActions keeps
  // this in sync with the route; see announce() for why the store cares.
  viewedOrderId: null,
  // The order shown in the slide-in tracking drawer, if any. Separate from
  // viewedOrderId: you can sit on /track/7 and open the drawer on order 5.
  drawerOrderId: null,
})

// Last seen status per order, so we can toast only on an actual advance. Plain Map:
// this drives notifications, it isn't rendered.
const prevStatusById = new Map()

let timer = null
// Bumped on every stop(). An in-flight response whose generation is stale (the user
// signed out, or switched account, while it was on the wire) is discarded instead of
// repopulating the panel.
let generation = 0

const { state: auth } = useAuth()
const { show } = useToast()

function currentUserId() {
  const id = Number(auth.user?.id)
  return Number.isFinite(id) && id > 0 ? id : null
}

// Is the customer already watching this order — on the tracking page, or in the
// drawer? Either way an OrderTracking component is polling it every 4s and toasting
// its own status changes; without this guard the same advance is announced twice.
function isBeingViewed(orderId) {
  const id = String(orderId) // route params are strings, order ids are numbers
  return (
    (state.viewedOrderId != null && String(state.viewedOrderId) === id) ||
    (state.drawerOrderId != null && String(state.drawerOrderId) === id)
  )
}

// Toast a status advance.
//
// Walks the FULL history, not just the active list. A delivered order is no longer
// active, so an active-only loop would never see it — the row would drop off the panel
// in total silence and the customer would just watch the bubble decrement.
//
// prevStatusById only ever gains an entry for an order that was ACTIVE (see the set()
// below), and we only toast when an entry already exists — so an order that was already
// finished when the user signed in can never toast on first sight.
function announce(history, active) {
  const activeIds = new Set(active.map((o) => o.id))

  for (const o of history) {
    const prev = prevStatusById.get(o.id)

    if (prev === undefined) {
      // Never seen in flight this session — establish a baseline, don't announce.
      if (activeIds.has(o.id)) prevStatusById.set(o.id, o.status)
      continue
    }

    if (prev !== o.status && !isBeingViewed(o.id)) {
      show(`Order ${o.reference || o.id}: ${statusLabel(o.status)}`)
    }

    // Every order we've announced must be updated or dropped here. Do neither and its
    // prev stays 'OutForDelivery' while its status reads 'Delivered' — so every later
    // poll re-announces the same delivery, forever. We drop rather than update, because
    // an order that's left the list is one we're no longer watching, and updating would
    // grow the map by one entry for every order the customer has ever placed.
    if (activeIds.has(o.id)) prevStatusById.set(o.id, o.status)
    else prevStatusById.delete(o.id)
  }
}

// An ETA belongs to an order that's out for delivery; a stale "~12 min" must not outlive
// its order. Rebuild rather than mutate — etaById is a plain object so its keys are
// STRINGS while order ids are NUMBERS, and a naive has(key) check would drop everything.
function pruneEtas(active) {
  const kept = {}
  for (const o of active) {
    if (o.id in state.etaById) kept[o.id] = state.etaById[o.id]
  }
  state.etaById = kept
}

// Pull the live ETA for anything out for delivery.
//
// This also has a load-bearing side effect: the Tracker service has no background
// worker, so an order only flips to Delivered when someone HITS its tracking
// endpoint. Calling it here means deliveries complete even when the customer never
// opens the tracking page. Don't "optimise" this away.
async function refreshEtas(orders) {
  if (!isTrackerApiConfigured()) return

  const enRoute = orders.filter((o) => o.status === 'OutForDelivery')
  if (!enRoute.length) return

  const myGen = generation
  // allSettled: fetchTracking throws on any non-2xx, and one bad order must not
  // take out the rest of the panel.
  const results = await Promise.allSettled(enRoute.map((o) => fetchTracking(o.id)))
  if (myGen !== generation) return

  results.forEach((res, i) => {
    if (res.status !== 'fulfilled') return
    const minutes = res.value?.etaMinutes
    if (minutes != null) state.etaById[enRoute[i].id] = minutes
  })
}

async function refresh() {
  const userId = currentUserId()
  if (!userId || !isOrderApiConfigured()) return

  const myGen = generation
  state.loading = true
  try {
    const history = await fetchOrderHistory(userId)
    if (myGen !== generation) return

    const active = selectActiveOrders(history, Date.now())
    announce(history, active)
    state.orders = active
    pruneEtas(active)
    state.error = ''

    await refreshEtas(active)
  } catch (e) {
    if (myGen !== generation) return
    // Non-fatal: the tracker is ambient UI, so a failed poll stays quiet and the
    // next tick retries. We keep the last known list on screen.
    state.error = e?.message || 'Could not refresh your orders.'
  } finally {
    if (myGen === generation) state.loading = false
  }
}

// Chained timeout rather than setInterval, so a slow poll can never overlap the next.
function scheduleNext() {
  if (timer) clearTimeout(timer)
  timer = setTimeout(tick, POLL_MS)
}

async function tick() {
  await refresh()
  if (timer !== null) scheduleNext()
}

function start() {
  if (timer !== null) return
  timer = 0 // claim the slot before the first await, so start() isn't re-entrant
  tick()
}

function stop() {
  generation++
  if (timer) clearTimeout(timer)
  timer = null
  state.loading = false
}

// Clear everything a signed-out user must not keep seeing.
function reset() {
  stop()
  state.orders = []
  state.etaById = {}
  state.error = ''
  state.panelOpen = false
  state.drawerOrderId = null // signing out must not leave an order on screen
  prevStatusById.clear()
}

// One watcher covers the whole auth lifecycle: hydrate() resolving on a hard refresh,
// signing in, signing out, and switching account. `immediate` starts polling for a
// session that's already live by the time this module is first imported.
watch(
  () => auth.user?.id,
  (id) => {
    reset()
    if (id && isOrderApiConfigured()) start()
  },
  { immediate: true },
)

// Don't poll a tab nobody is looking at; catch up the moment it comes back.
if (typeof document !== 'undefined') {
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') {
      if (currentUserId() && isOrderApiConfigured()) start()
    } else {
      stop()
    }
  })
}

export function useActiveOrders() {
  const orders = computed(() => state.orders)
  const count = computed(() => state.orders.length)
  const hasActive = computed(() => state.orders.length > 0)
  const etaFor = (id) => state.etaById[id] ?? null

  function openPanel() {
    state.panelOpen = true
  }
  function closePanel() {
    state.panelOpen = false
  }
  function togglePanel() {
    state.panelOpen = !state.panelOpen
  }

  // Told by FloatingActions which order (if any) the customer is watching, so we
  // don't double-announce a status change that TrackingView is already toasting.
  function setViewedOrder(orderId) {
    state.viewedOrderId = orderId ?? null
  }

  // Show an order in the slide-in tracking drawer. The drawer REPLACES the panel
  // rather than stacking on it — and it closes the panel directly rather than going
  // through ActiveOrdersPanel's close(), which would hand focus back to the FAB and
  // race the drawer for it.
  function openTracking(orderId) {
    state.drawerOrderId = orderId
    state.panelOpen = false
  }
  function closeTracking() {
    state.drawerOrderId = null
  }

  return {
    state,
    orders,
    count,
    hasActive,
    etaFor,
    refresh,
    openPanel,
    closePanel,
    togglePanel,
    setViewedOrder,
    openTracking,
    closeTracking,
  }
}

// Test seam: reset module-scope state between cases.
export function __resetForTests() {
  reset()
}
