<script setup>
import { computed, ref } from 'vue'
import { useCart } from '../stores/cart.js'
import { useToast } from '../stores/toast.js'

const props = defineProps({
  item: { type: Object, required: true },
})

const { add, increment, decrement, qtyOf } = useCart()
const { show } = useToast()

const qty = computed(() => qtyOf(props.item.id))
const busy = ref(false)

// No more can be added once stock (net of everyone's cart holds) is exhausted.
const soldOut = computed(
  () => props.item.availableStock != null && props.item.availableStock <= 0,
)
const stockHint = computed(() => {
  const a = props.item.availableStock
  if (a == null) return ''
  if (a <= 0) return 'Sold out'
  if (a <= 5) return `Only ${a} left`
  return ''
})

// Run a cart mutation, surfacing "Only N left" / errors as a toast. One at a time.
async function change(fn) {
  if (busy.value) return
  busy.value = true
  try {
    await fn()
  } catch (e) {
    show(e?.message || 'Could not update your cart.')
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <article class="item">
    <div class="item-media" aria-hidden="true">
      <img v-if="item.image" :src="item.image" :alt="item.name" class="item-img" loading="lazy" />
      <span v-else class="item-emoji">{{ item.emoji }}</span>
    </div>

    <div class="item-body">
      <h3 class="item-name">{{ item.name }}</h3>
      <p class="item-desc">{{ item.description }}</p>
      <p class="item-price">RM{{ item.price }}</p>
    </div>

    <!-- Add button turns into a quantity stepper once in the cart. Adding is blocked
         when stock (net of other shoppers' cart holds) runs out. -->
    <div class="item-action">
      <button
        v-if="qty === 0"
        type="button"
        class="btn btn-primary add-btn"
        :disabled="soldOut || busy"
        @click="change(() => add(item))"
      >
        {{ soldOut ? 'Sold out' : 'Add' }}
      </button>

      <div v-else class="stepper" role="group" :aria-label="`Quantity of ${item.name}`">
        <button type="button" class="step" aria-label="Decrease" :disabled="busy" @click="change(() => decrement(item.id))">−</button>
        <span class="step-qty">{{ qty }}</span>
        <button type="button" class="step" aria-label="Increase" :disabled="soldOut || busy" @click="change(() => increment(item.id))">+</button>
      </div>

      <span v-if="stockHint" class="item-stock-hint">{{ stockHint }}</span>
    </div>
  </article>
</template>

<style scoped>
.item {
  display: flex;
  align-items: center;
  gap: 16px;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  padding: 18px 20px;
  box-shadow: var(--shadow-sm);
  transition: border-color 0.15s ease, box-shadow 0.15s ease, transform 0.15s ease;
}
.item:hover {
  border-color: #cdd9f5;
  box-shadow: var(--shadow);
  transform: translateY(-2px);
}

.item-media {
  flex: none;
  width: 56px;
  height: 56px;
  display: grid;
  place-items: center;
  background: var(--color-surface);
  border-radius: 12px;
  overflow: hidden;
}
.item-emoji { font-size: 1.9rem; }
.item-img { width: 100%; height: 100%; object-fit: cover; }

.item-body { flex: 1 1 auto; min-width: 0; }
.item-name { margin: 0 0 2px; font-size: 1.05rem; font-weight: 700; color: var(--color-ink); }
.item-desc {
  margin: 0 0 6px;
  font-size: 0.86rem;
  color: var(--color-muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.item-price { margin: 0; font-weight: 800; color: var(--color-primary); }

.item-action { flex: none; display: flex; flex-direction: column; align-items: flex-end; gap: 4px; }
.add-btn { padding: 9px 22px; font-size: 0.95rem; }
.add-btn:disabled { opacity: 0.6; cursor: not-allowed; }
.step:disabled { opacity: 0.4; cursor: not-allowed; }
.step:disabled:hover { background: #fff; color: var(--color-primary); }
.item-stock-hint { font-size: 0.72rem; font-weight: 700; color: var(--color-muted); }

.stepper {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  padding: 4px;
}
.step {
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 50%;
  background: #fff;
  color: var(--color-primary);
  font-size: 1.2rem;
  font-weight: 700;
  line-height: 1;
  cursor: pointer;
  box-shadow: var(--shadow-sm);
  transition: background 0.15s ease, color 0.15s ease;
}
.step:hover { background: var(--color-primary); color: #fff; }
.step-qty { min-width: 22px; text-align: center; font-weight: 700; color: var(--color-ink); }
</style>
