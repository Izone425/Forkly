<script setup>
import { useRouter } from 'vue-router'
import { useCart } from '../stores/cart.js'

// Floating cart button (bottom-left). Shows a live item count and opens the
// order page, where the pre-added items are waiting.
const router = useRouter()
const { count } = useCart()

function openOrder() {
  router.push('/order')
}
</script>

<template>
  <button class="cart-fab" type="button" aria-label="Open your cart" @click="openOrder">
    <span class="cart-fab-icon" aria-hidden="true">🛒</span>
    <span v-if="count > 0" class="cart-fab-badge">{{ count }}</span>
  </button>
</template>

<style scoped>
/* FloatingActions owns the screen corner. `relative` stays: the badge below is
   absolutely positioned and needs a containing block. */
.cart-fab {
  position: relative;
  width: 62px;
  height: 62px;
  border: none;
  border-radius: 50%;
  background: var(--color-primary);
  color: #fff;
  box-shadow: var(--shadow-cta);
  cursor: pointer;
  display: grid;
  place-items: center;
  transition: transform 0.15s ease, background 0.15s ease;
}
.cart-fab:hover {
  background: var(--color-primary-dark);
  transform: translateY(-3px);
}
.cart-fab-icon { font-size: 1.55rem; line-height: 1; }

.cart-fab-badge {
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
  .cart-fab { width: 56px; height: 56px; }
}
</style>
