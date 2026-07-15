<script setup>
import { ref } from 'vue'
import { useActiveOrders } from '../stores/activeOrders.js'

// Floating "orders in flight" button. Unlike the cart FAB (always visible), this one
// only exists when there's actually something to track — see FloatingActions.vue.
const { state, count, togglePanel } = useActiveOrders()

const btn = ref(null)

// FloatingActions calls this to return focus here when the panel closes.
defineExpose({ focus: () => btn.value?.focus() })
</script>

<template>
  <button
    ref="btn"
    class="orders-fab"
    type="button"
    aria-haspopup="dialog"
    aria-controls="active-orders-panel"
    :aria-expanded="state.panelOpen"
    :aria-label="`Active orders, ${count}`"
    @click="togglePanel"
  >
    <span class="orders-fab-icon" aria-hidden="true">🛵</span>
    <span class="orders-fab-badge" aria-hidden="true">{{ count }}</span>
  </button>
</template>

<style scoped>
/* No `position: fixed` here — FloatingActions owns the screen corner. `relative` is
   still required so the badge below has a containing block. */
.orders-fab {
  position: relative;
  width: 62px;
  height: 62px;
  border: none;
  border-radius: 50%;
  background: var(--color-bg);
  color: var(--color-ink);
  border: 1px solid var(--color-border);
  box-shadow: var(--shadow-lg);
  cursor: pointer;
  display: grid;
  place-items: center;
  transition: transform 0.15s ease, border-color 0.15s ease;
}
.orders-fab:hover {
  border-color: var(--color-primary);
  transform: translateY(-3px);
}
.orders-fab-icon { font-size: 1.55rem; line-height: 1; }

.orders-fab-badge {
  position: absolute;
  top: -4px;
  right: -4px;
  min-width: 24px;
  height: 24px;
  padding: 0 6px;
  border-radius: 999px;
  background: #ef4444;
  color: #fff;
  font-size: 0.78rem;
  font-weight: 800;
  display: grid;
  place-items: center;
  border: 2px solid #fff;
}

@media (max-width: 720px) {
  .orders-fab { width: 56px; height: 56px; }
}
</style>
