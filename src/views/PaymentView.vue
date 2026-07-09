<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { isPaymentApiConfigured, startCheckout, payNow } from '../services/paymentApi.js'
import { getToken } from '../services/authApi.js'

const route = useRoute()
const router = useRouter()
const orderId = route.params.orderId

const configured = isPaymentApiConfigured()
const loading = ref(true)
const error = ref('')
const payment = ref(null) // { paymentId, reference, amount, currency, status }
const paying = ref(false)

// Mock card form
const method = ref('card')
const card = ref({ number: '', name: '', expiry: '', cvv: '' })
const simulateFailure = ref(false)

const amountLabel = computed(() => {
  if (!payment.value) return ''
  const c = payment.value.currency === 'MYR' ? 'RM' : payment.value.currency + ' '
  return c + Number(payment.value.amount).toFixed(2)
})
const isPaid = computed(() => payment.value?.status === 'Paid')
const isFailed = computed(() => payment.value?.status === 'Failed')

const digits = (s) => (s || '').replace(/\D/g, '')
const canPay = computed(() => {
  if (paying.value || !payment.value) return false
  if (method.value !== 'card') return true
  return digits(card.value.number).length >= 12 &&
    card.value.name.trim() && card.value.expiry.trim() && digits(card.value.cvv).length >= 3
})

onMounted(async () => {
  if (!getToken()) {
    router.replace({ name: 'login', query: { redirect: route.fullPath } })
    return
  }
  if (!configured) { loading.value = false; return }
  try {
    payment.value = await startCheckout(orderId)
  } catch (e) {
    error.value = e?.message || 'Could not start the payment.'
  } finally {
    loading.value = false
  }
})

async function pay() {
  if (!canPay.value) return
  paying.value = true
  error.value = ''
  try {
    const last4 = digits(card.value.number).slice(-4) || null
    payment.value = await payNow(payment.value.paymentId, {
      method: method.value,
      cardLast4: method.value === 'card' ? last4 : null,
      simulateFailure: simulateFailure.value,
    })
  } catch (e) {
    error.value = e?.message || 'Payment failed. Please try again.'
  } finally {
    paying.value = false
  }
}
</script>

<template>
  <div class="pay">
    <div class="pay-card">
      <header class="pay-head">
        <span class="brand">Forkly&nbsp;Pay</span>
        <span class="secure">🔒 Secure checkout</span>
      </header>

      <p v-if="loading" class="state">Preparing your payment…</p>

      <p v-else-if="!configured" class="state state-warn">
        Payment service isn't configured. Set <code>VITE_PAYMENT_API_BASE</code> and start the payment service.
      </p>

      <p v-else-if="error && !payment" class="state state-error">{{ error }}</p>

      <!-- SUCCESS -->
      <div v-else-if="isPaid" class="done">
        <div class="tick" aria-hidden="true">✓</div>
        <h1>Payment successful</h1>
        <p class="done-sub">Your order is paid and sent to the kitchen.</p>
        <dl class="receipt">
          <div><dt>Order</dt><dd>{{ payment.reference || ('#' + payment.orderId) }}</dd></div>
          <div><dt>Amount</dt><dd>{{ amountLabel }}</dd></div>
          <div><dt>Payment ref</dt><dd>{{ payment.paymentId }}</dd></div>
          <div v-if="payment.cardLast4"><dt>Card</dt><dd>•••• {{ payment.cardLast4 }}</dd></div>
        </dl>
        <div class="done-actions">
          <RouterLink class="btn btn-primary btn-block" :to="{ name: 'track', params: { orderId: payment.orderId } }">Track my order</RouterLink>
          <RouterLink class="btn btn-ghost btn-block" :to="{ name: 'landing' }">Back to home</RouterLink>
        </div>
      </div>

      <!-- PAYMENT FORM (pending / failed) -->
      <div v-else-if="payment" class="form">
        <div class="summary">
          <div>
            <span class="summary-label">Order {{ payment.reference || ('#' + payment.orderId) }}</span>
            <span class="summary-sub">Amount due</span>
          </div>
          <span class="summary-amount">{{ amountLabel }}</span>
        </div>

        <div class="methods" role="tablist" aria-label="Payment method">
          <button type="button" :class="{ active: method === 'card' }" @click="method = 'card'">💳 Card</button>
          <button type="button" :class="{ active: method === 'fpx' }" @click="method = 'fpx'">🏦 FPX</button>
          <button type="button" :class="{ active: method === 'ewallet' }" @click="method = 'ewallet'">📱 E-Wallet</button>
        </div>

        <div v-if="method === 'card'" class="fields">
          <label class="field">
            <span>Card number</span>
            <input v-model="card.number" inputmode="numeric" placeholder="4242 4242 4242 4242" maxlength="23" />
          </label>
          <label class="field">
            <span>Name on card</span>
            <input v-model="card.name" type="text" placeholder="Ahmad Aiman" />
          </label>
          <div class="field-row">
            <label class="field">
              <span>Expiry</span>
              <input v-model="card.expiry" placeholder="MM/YY" maxlength="5" />
            </label>
            <label class="field">
              <span>CVV</span>
              <input v-model="card.cvv" inputmode="numeric" placeholder="123" maxlength="4" />
            </label>
          </div>
        </div>
        <p v-else class="redirect-note">
          You'll be redirected to your {{ method === 'fpx' ? 'bank' : 'e-wallet' }} to authorise the payment.
          (Mocked — just press Pay.)
        </p>

        <label class="sim">
          <input v-model="simulateFailure" type="checkbox" />
          <span>Simulate a declined payment (for testing)</span>
        </label>

        <p v-if="isFailed || error" class="pay-error" role="alert">
          {{ payment?.failureReason || error || 'Payment was declined. Please try again.' }}
        </p>

        <button type="button" class="btn btn-primary btn-block pay-btn" :disabled="!canPay" @click="pay">
          {{ paying ? 'Processing…' : `Pay ${amountLabel}` }}
        </button>
        <p class="mock-note">Mock gateway — no real card is charged.</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.pay { min-height: 100vh; background: var(--color-surface); display: flex; align-items: flex-start; justify-content: center; padding: 48px 20px; }
.pay-card {
  width: 100%; max-width: 440px; background: var(--color-bg);
  border: 1px solid var(--color-border); border-radius: var(--radius);
  box-shadow: var(--shadow-lg); padding: 24px 26px 28px;
}
.pay-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; }
.brand { font-weight: 800; font-size: 1.15rem; color: var(--color-primary); letter-spacing: -0.3px; }
.secure { font-size: 0.76rem; color: var(--color-muted); }

.state { text-align: center; color: var(--color-muted); padding: 40px 0; }
.state-error { color: #d33; }
.state-warn { color: #b45309; }
.state code { background: var(--color-surface); padding: 1px 6px; border-radius: 6px; }

/* Summary */
.summary {
  display: flex; align-items: center; justify-content: space-between;
  background: var(--color-primary-soft); border: 1px solid #cdd9f5;
  border-radius: var(--radius-sm); padding: 14px 16px; margin-bottom: 18px;
}
.summary-label { display: block; font-weight: 700; color: var(--color-ink); }
.summary-sub { display: block; font-size: 0.78rem; color: var(--color-muted); }
.summary-amount { font-size: 1.4rem; font-weight: 800; color: var(--color-primary); }

/* Method tabs */
.methods { display: grid; grid-template-columns: repeat(3, 1fr); gap: 8px; margin-bottom: 16px; }
.methods button {
  font-family: inherit; font-size: 0.85rem; font-weight: 600; padding: 10px 0;
  border: 1px solid var(--color-border); border-radius: var(--radius-sm);
  background: #fff; color: var(--color-body); cursor: pointer; transition: all 0.15s ease;
}
.methods button.active { border-color: var(--color-primary); background: var(--color-primary-soft); color: var(--color-primary); }

/* Fields */
.fields { display: grid; gap: 12px; }
.field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.field { display: flex; flex-direction: column; }
.field span { font-size: 0.8rem; font-weight: 600; color: var(--color-ink); margin-bottom: 6px; }
.field input {
  font-family: inherit; font-size: 0.95rem; padding: 11px 12px;
  border: 1px solid var(--color-border); border-radius: var(--radius-sm); color: var(--color-ink);
}
.field input:focus { outline: none; border-color: var(--color-primary); box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.15); }
.redirect-note { font-size: 0.88rem; color: var(--color-muted); background: var(--color-surface); border-radius: var(--radius-sm); padding: 14px; margin: 0; }

.sim { display: flex; align-items: center; gap: 9px; margin: 16px 0 4px; font-size: 0.82rem; color: var(--color-muted); }
.sim input { width: 15px; height: 15px; }

.pay-error { margin: 12px 0 0; color: #d33; font-size: 0.86rem; }
.pay-btn { margin-top: 18px; padding: 14px 0; font-size: 1.02rem; }
.pay-btn:disabled { opacity: 0.6; cursor: default; transform: none; }
.mock-note { text-align: center; font-size: 0.74rem; color: var(--color-muted); margin: 10px 0 0; }

/* Success */
.done { text-align: center; padding: 8px 0 4px; }
.tick { width: 60px; height: 60px; margin: 4px auto 14px; display: grid; place-items: center; border-radius: 50%; background: #e7f6f0; color: #059669; font-size: 1.9rem; font-weight: 800; }
.done h1 { margin: 0 0 4px; font-size: 1.45rem; color: var(--color-ink); }
.done-sub { margin: 0 0 20px; color: var(--color-muted); }
.receipt { text-align: left; border: 1px solid var(--color-border); border-radius: var(--radius-sm); padding: 6px 14px; margin: 0 0 20px; }
.receipt div { display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid var(--color-border); }
.receipt div:last-child { border-bottom: none; }
.receipt dt { color: var(--color-muted); font-size: 0.88rem; }
.receipt dd { margin: 0; font-weight: 700; color: var(--color-ink); font-size: 0.9rem; }
.done-actions { display: grid; gap: 10px; }
.done-actions .btn { padding: 12px 0; }
</style>
