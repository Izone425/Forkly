<script setup>
import { onMounted, onUnmounted, reactive, ref } from 'vue'
import { useMenu } from '../stores/menu.js'
import { useCart } from '../stores/cart.js'
import { useToast } from '../stores/toast.js'

// Full menu, loaded from the backend (amirul-menu via the Order service).
const { state: menu, load, startPolling, stopPolling } = useMenu()
const { add, increment, decrement, qtyOf } = useCart()
const { show } = useToast()

// Track items whose picture failed to load, so we can fall back gracefully.
const failed = reactive(new Set())
const showImage = (item) => Boolean(item.image) && !failed.has(item.id)
const initialOf = (item) => (item.name || '?').trim().charAt(0).toUpperCase()

// One in-flight cart change at a time per card, so a double-click can't reserve
// the same quantity twice.
const busyId = ref(null)

// Run a cart mutation, surfacing "Only N left" / errors as a toast.
async function change(fn, item) {
  if (busyId.value != null) return
  busyId.value = item.id
  try {
    await fn()
  } catch (e) {
    show(e?.message || 'Could not update your cart.')
  } finally {
    busyId.value = null
  }
}

// No more can be added once stock (net of everyone's holds) is exhausted.
function soldOut(item) {
  return item.availableStock != null && item.availableStock <= 0
}

// Small hint under the control: sold out, or a low-stock nudge.
function stockHint(item) {
  const a = item.availableStock
  if (a == null) return ''
  if (a <= 0) return 'Sold out'
  if (a <= 5) return `Only ${a} left`
  return ''
}

onMounted(() => {
  load()
  startPolling()
})
onUnmounted(stopPolling)
</script>

<template>
  <section class="section menu" id="menu">
    <div class="container">
      <h2 class="section-title">Our Menu</h2>
      <p class="section-sub">Browse the full menu and add items straight to your cart.</p>

      <p v-if="menu.loading && menu.items.length === 0" class="menu-state">Loading menu…</p>

      <div v-else class="menu-grid">
        <article v-for="item in menu.items" :key="item.id" class="menu-card">
          <div class="menu-media" aria-hidden="true">
            <img
              v-if="showImage(item)"
              :src="item.image"
              :alt="item.name"
              class="menu-img"
              loading="lazy"
              @error="failed.add(item.id)"
            />
            <span v-else-if="item.emoji" class="menu-emoji">{{ item.emoji }}</span>
            <span v-else class="menu-fallback">{{ initialOf(item) }}</span>
          </div>
          <h3 class="menu-name">{{ item.name }}</h3>
          <p class="menu-desc">{{ item.description }}</p>
          <p class="menu-price">RM{{ item.price }}</p>

          <!-- Bottom-right of the card: a "+" to add, becoming a −/+ stepper
               once in the cart. Adding is blocked when stock (net of other shoppers'
               cart holds) runs out. -->
          <button
            v-if="qtyOf(item.id) === 0"
            type="button"
            class="menu-add"
            :class="{ 'is-out': soldOut(item) }"
            :disabled="soldOut(item) || busyId === item.id"
            :aria-label="soldOut(item) ? `${item.name} sold out` : `Add ${item.name} to cart`"
            @click="change(() => add(item), item)"
          >
            <span aria-hidden="true">{{ soldOut(item) ? '—' : '+' }}</span>
          </button>

          <div
            v-else
            class="menu-stepper"
            role="group"
            :aria-label="`Quantity of ${item.name}`"
          >
            <button
              type="button"
              class="menu-step"
              aria-label="Decrease"
              :disabled="busyId === item.id"
              @click="change(() => decrement(item.id), item)"
            >−</button>
            <span class="menu-step-qty">{{ qtyOf(item.id) }}</span>
            <button
              type="button"
              class="menu-step"
              aria-label="Increase"
              :disabled="soldOut(item) || busyId === item.id"
              @click="change(() => increment(item.id), item)"
            >+</button>
          </div>

          <!-- Sold-out / low-stock hint. -->
          <span v-if="stockHint(item)" class="menu-stock-hint">{{ stockHint(item) }}</span>
        </article>
      </div>
    </div>
  </section>
</template>

<style scoped>
.menu {
  background:
    linear-gradient(rgba(248, 250, 252, 0.9), rgba(248, 250, 252, 0.95)),
    url('/assets/image1.jpg') center / cover no-repeat;
}

.menu-state { text-align: center; color: var(--color-muted); }

.menu-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr); /* 3 per row, wraps to new rows */
  gap: 24px;
  margin-top: 8px;
}

.menu-card {
  position: relative;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  padding: 28px 24px 26px;
  text-align: center;
  box-shadow: var(--shadow);
  transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
}
.menu-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
  border-color: #cdd9f5;
}

.menu-media {
  width: 88px;
  height: 88px;
  margin: 0 auto 14px;
  display: grid;
  place-items: center;
  border-radius: 14px;
  overflow: hidden;
  background: var(--color-surface);
}
.menu-emoji { font-size: 2.6rem; line-height: 1; }
.menu-img { width: 100%; height: 100%; object-fit: cover; }
.menu-fallback { font-size: 2rem; font-weight: 800; color: var(--color-primary); }
.menu-name { margin: 0 0 6px; font-size: 1.15rem; color: var(--color-ink); font-weight: 700; }
.menu-desc { margin: 0 0 10px; font-size: 0.88rem; color: var(--color-muted); }
.menu-price { margin: 0; font-size: 1.3rem; font-weight: 800; color: var(--color-primary); }

/* Add button anchored to the bottom-right edge of the card. */
.menu-add {
  position: absolute;
  right: 16px;
  bottom: 16px;
  width: 40px;
  height: 40px;
  border: none;
  border-radius: 50%;
  background: var(--color-primary);
  color: #fff;
  font-size: 1.4rem;
  font-weight: 700;
  line-height: 1;
  cursor: pointer;
  box-shadow: var(--shadow-cta);
  display: grid;
  place-items: center;
  transition: transform 0.12s ease, background 0.12s ease;
}
.menu-add:hover:not(:disabled) { background: var(--color-primary-dark); transform: scale(1.08); }
.menu-add:disabled { cursor: not-allowed; }
.menu-add.is-out { background: var(--color-border); color: var(--color-muted); box-shadow: none; }
.menu-step:disabled { opacity: 0.4; cursor: not-allowed; }
.menu-step:disabled:hover { background: #fff; color: var(--color-primary); }

/* Sold-out / low-stock hint, bottom-left of the card. */
.menu-stock-hint {
  position: absolute;
  left: 16px;
  bottom: 24px;
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--color-muted);
}

/* Stepper (−/+ with qty) anchored where the add button sits, once in the cart. */
.menu-stepper {
  position: absolute;
  right: 16px;
  bottom: 16px;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  padding: 4px;
  box-shadow: var(--shadow-cta);
}
.menu-step {
  width: 30px;
  height: 30px;
  border: none;
  border-radius: 50%;
  background: #fff;
  color: var(--color-primary);
  font-size: 1.2rem;
  font-weight: 700;
  line-height: 1;
  cursor: pointer;
  box-shadow: var(--shadow-sm);
  display: grid;
  place-items: center;
  transition: background 0.15s ease, color 0.15s ease;
}
.menu-step:hover { background: var(--color-primary); color: #fff; }
.menu-step-qty { min-width: 20px; text-align: center; font-weight: 800; color: var(--color-ink); }

@media (max-width: 900px) {
  .menu-grid { grid-template-columns: repeat(2, 1fr); }
}
@media (max-width: 600px) {
  .menu-grid { grid-template-columns: 1fr; }
}
</style>
