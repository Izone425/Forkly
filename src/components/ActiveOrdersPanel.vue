<script setup>
import { ref, watch, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { useActiveOrders } from '../stores/activeOrders.js'
import StatusIcon from './StatusIcon.vue'
import { statusLabel, statusClass, canPay, money, phaseOf } from '../utils/orderStatus.js'

// The expanded active-orders card. Each row is the way back into tracking (or back
// into payment, for an order the customer never finished paying for).
const emit = defineEmits(['close'])

const { state, orders, etaFor, closePanel, openTracking } = useActiveOrders()
const route = useRoute()
const closeBtn = ref(null)

function close() {
  closePanel()
  // Let the opener take focus back — a popover that drops focus to <body> strands
  // keyboard users at the top of the page.
  emit('close')
}

// The order the customer is already looking at: show it, but don't offer a link to
// the page they're on.
const isViewing = (id) =>
  route.name === 'track' && String(route.params.orderId) === String(id)

function onKeydown(e) {
  if (e.key === 'Escape' && state.panelOpen) close()
}

watch(
  () => state.panelOpen,
  (open) => {
    if (open) nextTick(() => closeBtn.value?.focus())
  },
)

onMounted(() => window.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
</script>

<template>
  <Teleport to="body">
    <div v-if="state.panelOpen" class="panel-root">
      <div class="panel-backdrop" @click="close" />

      <section
        id="active-orders-panel"
        class="panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="active-orders-title"
      >
        <header class="panel-head">
          <h2 id="active-orders-title" class="panel-title">Your active orders</h2>
          <button ref="closeBtn" type="button" class="panel-close" aria-label="Close" @click="close">
            &times;
          </button>
        </header>

        <ul class="rows">
          <li v-for="o in orders" :key="o.id" class="row">
            <!-- Already on this order's tracking page: show it, but don't link to
                 the page we're standing on. -->
            <div v-if="isViewing(o.id)" class="row-main viewing">
              <div class="row-top">
                <span class="row-ref">{{ o.reference || `Order #${o.id}` }}</span>
                <span class="row-status">
                  <StatusIcon :phase="phaseOf(o)" />
                  <span class="badge" :class="statusClass(o.status)">{{ statusLabel(o.status) }}</span>
                </span>
              </div>
              <div class="row-bottom">
                <span class="row-total">{{ money(o.total, o.currency) }}</span>
                <span class="row-note">Viewing</span>
              </div>
            </div>

            <!-- Opens tracking in the drawer rather than navigating away. openTracking()
                 closes this panel itself — calling our close() would emit and hand focus
                 back to the FAB, racing the drawer for it. -->
            <button v-else type="button" class="row-main" @click="openTracking(o.id)">
              <div class="row-top">
                <span class="row-ref">{{ o.reference || `Order #${o.id}` }}</span>
                <span class="row-status">
                  <StatusIcon :phase="phaseOf(o)" />
                  <span class="badge" :class="statusClass(o.status)">{{ statusLabel(o.status) }}</span>
                </span>
              </div>
              <div class="row-bottom">
                <span class="row-total">{{ money(o.total, o.currency) }}</span>
                <span v-if="etaFor(o.id) != null" class="row-eta">~{{ etaFor(o.id) }} min</span>
              </div>
            </button>

            <!-- The order was created but never paid for: today this is unreachable
                 once the payment tab is closed. -->
            <RouterLink
              v-if="canPay(o)"
              class="row-pay"
              :to="{ name: 'payment', params: { orderId: o.id } }"
              @click="close"
            >
              Pay now
            </RouterLink>
          </li>
        </ul>
      </section>
    </div>
  </Teleport>
</template>

<style scoped>
/* Sits on the modal tier — below ToastHost (200), so a status toast still lands on
   top of an open panel. */
.panel-root { position: fixed; inset: 0; z-index: 100; }

.panel-backdrop { position: absolute; inset: 0; background: rgba(15, 23, 42, 0.35); }

.panel {
  position: absolute;
  right: 24px;
  bottom: 100px;
  width: min(360px, calc(100vw - 32px));
  max-height: 60vh;
  display: flex;
  flex-direction: column;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  box-shadow: var(--shadow-lg);
  overflow: hidden;
}

.panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 16px;
  border-bottom: 1px solid var(--color-border);
}
.panel-title { margin: 0; font-size: 0.95rem; font-weight: 800; color: var(--color-ink); }
.panel-close {
  border: none;
  background: transparent;
  font-size: 1.5rem;
  line-height: 1;
  color: var(--color-muted);
  cursor: pointer;
  padding: 0 4px;
  border-radius: 8px;
}
.panel-close:hover { color: var(--color-ink); }

.rows { list-style: none; margin: 0; padding: 6px; overflow-y: auto; }
.row { display: flex; align-items: stretch; gap: 6px; }
.row + .row { border-top: 1px solid var(--color-border); }

/* Shared by the <button> (opens the drawer) and the muted <div> (already viewing). */
.row-main {
  flex: 1;
  display: grid;
  gap: 6px;
  padding: 12px 10px;
  border-radius: var(--radius-sm);
  transition: background 0.15s ease;
  /* button reset */
  width: 100%;
  text-align: left;
  font: inherit;
  color: inherit;
  background: none;
  border: none;
  cursor: pointer;
}
button.row-main:hover { background: var(--color-surface); }
.row-main.viewing { opacity: 0.65; cursor: default; }

.row-top { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.row-ref { font-weight: 700; color: var(--color-ink); font-size: 0.92rem; }
/* The animated icon sits with the badge, so the two read as one status unit. Sized a
   little above the row's text so the finer particles (steam, speed lines) stay legible. */
.row-status { display: inline-flex; align-items: center; gap: 7px; font-size: 1.1rem; }

.row-bottom { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.row-total { font-size: 0.86rem; color: var(--color-body); }
.row-eta { font-size: 0.82rem; font-weight: 700; color: var(--color-primary); }
.row-note {
  font-size: 0.72rem; font-weight: 700; text-transform: uppercase;
  letter-spacing: 0.4px; color: var(--color-muted);
}

.row-pay {
  flex: none;
  align-self: center;
  padding: 7px 12px;
  border-radius: 999px;
  background: var(--color-primary);
  color: #fff;
  font-size: 0.8rem;
  font-weight: 700;
  white-space: nowrap;
}
.row-pay:hover { background: var(--color-primary-dark); }

@media (max-width: 720px) {
  .panel { right: 16px; bottom: 88px; }
}
</style>
