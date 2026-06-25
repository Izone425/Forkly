<script setup>
import { ref } from 'vue'
import { useCart } from '../stores/cart.js'
import { buildOrderPayload, submitOrder, isOrderApiConfigured } from '../services/orderGateway.js'

const cart = useCart()

const placing = ref(false)
const confirmation = ref(null) // { orderId, simulated }
const error = ref('')

function money(n) {
  return 'RM' + n.toFixed(2)
}

async function placeOrder() {
  if (cart.isEmpty.value || placing.value) return
  placing.value = true
  error.value = ''
  confirmation.value = null
  try {
    const payload = buildOrderPayload(cart.lines.value)
    const result = await submitOrder(payload)
    confirmation.value = result
    cart.clear()
  } catch (e) {
    error.value = e?.message || 'Could not place the order. Please try again.'
  } finally {
    placing.value = false
  }
}
</script>

<template>
  <aside class="cart">
    <h2 class="cart-title">Your Order</h2>

    <!-- Confirmation after a successful place-order. -->
    <div v-if="confirmation" class="confirm" role="status">
      <div class="confirm-check" aria-hidden="true">✓</div>
      <p class="confirm-head">Order placed!</p>
      <p class="confirm-id">Reference: <strong>{{ confirmation.orderId }}</strong></p>
      <p v-if="confirmation.simulated" class="confirm-note">
        Demo mode — no order service connected yet.
      </p>
      <button type="button" class="btn btn-ghost confirm-again" @click="confirmation = null">
        Start a new order
      </button>
    </div>

    <template v-else>
      <p v-if="cart.isEmpty.value" class="cart-empty">
        Your cart is empty. Add items from the menu to get started.
      </p>

      <template v-else>
        <ul class="cart-lines">
          <li v-for="line in cart.lines.value" :key="line.item.id" class="cart-line">
            <span class="line-qty">{{ line.qty }}×</span>
            <span class="line-name">{{ line.item.name }}</span>
            <span class="line-price">{{ money(line.item.price * line.qty) }}</span>
            <button
              type="button"
              class="line-remove"
              :aria-label="`Remove ${line.item.name}`"
              @click="cart.remove(line.item.id)"
            >×</button>
          </li>
        </ul>

        <dl class="totals">
          <div><dt>Subtotal</dt><dd>{{ money(cart.subtotal.value) }}</dd></div>
          <div><dt>SST (6%)</dt><dd>{{ money(cart.tax.value) }}</dd></div>
          <div class="grand"><dt>Total</dt><dd>{{ money(cart.total.value) }}</dd></div>
        </dl>

        <p v-if="error" class="cart-error" role="alert">{{ error }}</p>

        <button
          type="button"
          class="btn btn-primary btn-block place-btn"
          :disabled="placing"
          @click="placeOrder"
        >
          {{ placing ? 'Placing…' : `Place Order · ${money(cart.total.value)}` }}
        </button>

        <p v-if="!isOrderApiConfigured()" class="cart-hint">
          Orders run in demo mode until the order service is connected.
        </p>
      </template>
    </template>
  </aside>
</template>

<style scoped>
.cart {
  position: sticky;
  top: 88px;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  box-shadow: var(--shadow);
  padding: 24px;
}
.cart-title { margin: 0 0 18px; font-size: 1.2rem; font-weight: 800; color: var(--color-ink); }

.cart-empty { margin: 0; color: var(--color-muted); font-size: 0.95rem; line-height: 1.6; }

.cart-lines { list-style: none; margin: 0 0 16px; padding: 0; display: grid; gap: 12px; }
.cart-line {
  display: grid;
  grid-template-columns: auto 1fr auto auto;
  align-items: center;
  gap: 10px;
  font-size: 0.95rem;
}
.line-qty { color: var(--color-primary); font-weight: 700; }
.line-name { color: var(--color-ink); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.line-price { color: var(--color-ink); font-weight: 600; }
.line-remove {
  width: 22px; height: 22px;
  border: none; border-radius: 50%;
  background: var(--color-surface); color: var(--color-muted);
  font-size: 1rem; line-height: 1; cursor: pointer;
}
.line-remove:hover { background: #fde8e8; color: #d33; }

.totals {
  margin: 0 0 18px;
  padding-top: 16px;
  border-top: 1px dashed var(--color-border);
  display: grid;
  gap: 8px;
}
.totals div { display: flex; justify-content: space-between; }
.totals dt, .totals dd { margin: 0; }
.totals dt { color: var(--color-body); }
.totals dd { color: var(--color-ink); font-weight: 600; }
.totals .grand { padding-top: 8px; border-top: 1px solid var(--color-border); }
.totals .grand dt, .totals .grand dd { font-size: 1.15rem; font-weight: 800; color: var(--color-ink); }

.place-btn { padding: 14px 0; font-size: 1.02rem; }
.place-btn:disabled { opacity: 0.7; cursor: default; transform: none; }
.cart-hint { margin: 12px 0 0; text-align: center; font-size: 0.82rem; color: var(--color-muted); }
.cart-error { margin: 0 0 12px; color: #d33; font-size: 0.88rem; }

/* Confirmation */
.confirm { text-align: center; padding: 8px 0; }
.confirm-check {
  width: 52px; height: 52px; margin: 0 auto 12px;
  display: grid; place-items: center;
  background: var(--color-primary-soft); color: var(--color-primary);
  border-radius: 50%; font-size: 1.6rem; font-weight: 800;
}
.confirm-head { margin: 0 0 4px; font-size: 1.2rem; font-weight: 800; color: var(--color-ink); }
.confirm-id { margin: 0 0 4px; color: var(--color-body); font-size: 0.95rem; }
.confirm-note { margin: 0 0 16px; color: var(--color-muted); font-size: 0.82rem; }
.confirm-again { width: 100%; }

@media (max-width: 880px) {
  .cart { position: static; }
}
</style>
