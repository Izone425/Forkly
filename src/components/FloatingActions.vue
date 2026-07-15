<script setup>
import { ref, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import FloatingCart from './FloatingCart.vue'
import ActiveOrdersFab from './ActiveOrdersFab.vue'
import ActiveOrdersPanel from './ActiveOrdersPanel.vue'
import TrackingDrawer from './TrackingDrawer.vue'
import { useActiveOrders } from '../stores/activeOrders.js'

// Owns the bottom-right corner for the whole app, so the two bubbles can't collide
// and the corner offset lives in exactly one place.
const route = useRoute()
const { hasActive, closePanel, closeTracking, setViewedOrder } = useActiveOrders()

const fab = ref(null)

// Allowlists, not denylists: a route added later defaults to hidden rather than
// leaking a customer bubble into the admin console or the kitchen board.
const CUSTOMER_ROUTES = ['landing', 'order', 'account', 'payment', 'track']
const CART_ROUTES = ['landing', 'account'] // exactly where FloatingCart lived before

const showOrders = computed(() => hasActive.value && CUSTOMER_ROUTES.includes(route.name))
const showCart = computed(() => CART_ROUTES.includes(route.name))

// Keep the store told which order is on screen. It suppresses its own status toast
// for that order, because TrackingView already announces it on a faster poll.
watch(
  () => (route.name === 'track' ? route.params.orderId : null),
  (id) => setViewedOrder(id),
  { immediate: true },
)

// The panel is teleported to <body>, so it outlives the button that opened it. Close
// it whenever its opener goes away — the last order was delivered, or we navigated
// somewhere the tracker doesn't belong — or it would hang there over an empty list.
watch(showOrders, (visible) => {
  if (!visible) closePanel()
})

// The drawer overlays whatever page we were on, so a navigation underneath it (browser
// back/forward, or the "Pay now" link) must not leave it stranded — e.g. floating over
// the admin console. Deliberately NOT tied to showOrders: an order that just went
// Delivered drops off the active list, and the customer should still finish reading it.
watch(
  () => route.fullPath,
  () => closeTracking(),
)

// Hand focus back to the button that opened the panel or drawer. The FAB may be gone by
// now — delivering the last active order unmounts it while its drawer is still open — so
// fall back to the page itself rather than dumping focus on <body>.
function restoreFocus() {
  if (fab.value) {
    fab.value.focus()
    return
  }
  const page = document.querySelector('main') || document.querySelector('h1')
  if (page) {
    page.setAttribute('tabindex', '-1')
    page.focus()
  }
}
</script>

<template>
  <div v-if="showCart || showOrders" class="fab-stack">
    <!-- column-reverse: the cart stays pinned to the corner and the stack grows up. -->
    <FloatingCart v-if="showCart" />
    <ActiveOrdersFab v-if="showOrders" ref="fab" />
  </div>

  <ActiveOrdersPanel @close="restoreFocus" />
  <TrackingDrawer @close="restoreFocus" />
</template>

<style scoped>
.fab-stack {
  position: fixed;
  right: 24px;
  bottom: 24px;
  z-index: 50;
  display: flex;
  flex-direction: column-reverse;
  align-items: flex-end;
  gap: 12px;
}

@media (max-width: 720px) {
  .fab-stack { right: 16px; bottom: 16px; }
}
</style>
