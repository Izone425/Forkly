<script setup>
// Slide-in order tracking. Opened from a row in the active-orders panel, so checking
// on a delivery is a peek rather than a full-page departure — the customer keeps their
// place (and their scroll position) on whatever page they were on.
//
// The full page at /track/:orderId still exists for deep links and the payment-success
// handoff; both render the same OrderTracking component.
import { ref, computed, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'
import OrderTracking from './OrderTracking.vue'
import { useActiveOrders } from '../stores/activeOrders.js'

const emit = defineEmits(['close'])

const { state, closeTracking } = useActiveOrders()

const closeBtn = ref(null)
const tracking = ref(null) // latest payload, just to title the drawer

const isOpen = computed(() => state.drawerOrderId != null)

function close() {
  closeTracking()
  emit('close') // FloatingActions hands focus back to the FAB
}

function onKeydown(event) {
  if (event.key === 'Escape' && isOpen.value) close()
}

watch(isOpen, (open) => {
  if (open) nextTick(() => closeBtn.value?.focus())
  else tracking.value = null
})

onMounted(() => window.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <Teleport to="body">
    <div class="drawer-root" :class="{ open: isOpen }">
      <div class="drawer-backdrop" @click="close" />

      <aside class="drawer-panel" role="dialog" aria-modal="true" aria-label="Order tracking">
        <header class="drawer-head">
          <div>
            <p class="drawer-eyebrow">Order tracking</p>
            <span class="drawer-title">
              {{ tracking?.reference || (state.drawerOrderId ? `Order #${state.drawerOrderId}` : '') }}
            </span>
          </div>
          <button ref="closeBtn" type="button" class="drawer-close" aria-label="Close" @click="close">
            &times;
          </button>
        </header>

        <div class="drawer-body">
          <!-- Mounted only while open: the root stays in the DOM so the slide can
               animate, so an unguarded child would poll forever with a null id.
               The key remounts (fresh timers, fresh state) when a different order
               is opened. -->
          <OrderTracking
            v-if="isOpen"
            :key="state.drawerOrderId"
            :order-id="state.drawerOrderId"
            @loaded="(t) => (tracking = t)"
          />
        </div>
      </aside>
    </div>
  </Teleport>
</template>

<style scoped>
/* Above the active-orders panel (100), but BELOW ToastHost (200) — a status toast
   fired while the drawer is open must land on top of it, not behind the backdrop. */
.drawer-root {
  position: fixed;
  inset: 0;
  z-index: 150;
  pointer-events: none;
  visibility: hidden;
}
.drawer-root.open { pointer-events: auto; visibility: visible; }

.drawer-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(15, 23, 42, 0.45);
  opacity: 0;
  transition: opacity 0.25s ease;
}
.drawer-root.open .drawer-backdrop { opacity: 1; }

.drawer-panel {
  position: absolute;
  top: 0;
  right: 0;
  height: 100%;
  width: min(440px, 100vw);
  background: #fff;
  box-shadow: -8px 0 30px rgba(15, 23, 42, 0.18);
  display: flex;
  flex-direction: column;
  transform: translateX(100%);
  transition: transform 0.28s cubic-bezier(0.4, 0, 0.2, 1);
}
.drawer-root.open .drawer-panel { transform: translateX(0); }

.drawer-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid var(--color-border);
}
.drawer-eyebrow {
  margin: 0 0 3px;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.14em;
  text-transform: uppercase;
  color: var(--color-primary);
}
.drawer-title { font-weight: 800; font-size: 1.1rem; color: var(--color-ink); }
.drawer-close {
  border: none;
  background: transparent;
  font-size: 1.6rem;
  line-height: 1;
  color: var(--color-muted);
  cursor: pointer;
  padding: 0 4px;
  border-radius: 8px;
}
.drawer-close:hover { color: var(--color-ink); }

.drawer-body {
  flex: 1;
  overflow-y: auto;
  padding: 24px 22px;
}

@media (max-width: 720px) {
  .drawer-panel { width: 100vw; }
}
</style>
