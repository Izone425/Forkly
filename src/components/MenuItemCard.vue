<script setup>
import { computed, ref } from 'vue'
import { useCart } from '../stores/cart.js'

const props = defineProps({
  item: { type: Object, required: true },
})

const { add, increment, decrement, qtyOf } = useCart()
const qty = computed(() => qtyOf(props.item.id))

// Show the picture only if there is one AND it actually loads; otherwise fall
// back to the emoji, then to the item's initial — so an item with a missing or
// broken image never renders a blank/broken box.
const imgFailed = ref(false)
const showImage = computed(() => Boolean(props.item.image) && !imgFailed.value)
const initial = computed(() => (props.item.name || '?').trim().charAt(0).toUpperCase())
</script>

<template>
  <article class="item">
    <div class="item-media" aria-hidden="true">
      <img
        v-if="showImage"
        :src="item.image"
        :alt="item.name"
        class="item-img"
        loading="lazy"
        @error="imgFailed = true"
      />
      <span v-else-if="item.emoji" class="item-emoji">{{ item.emoji }}</span>
      <span v-else class="item-fallback">{{ initial }}</span>
    </div>

    <div class="item-body">
      <h3 class="item-name">{{ item.name }}</h3>
      <p class="item-desc">{{ item.description }}</p>
      <p class="item-price">RM{{ item.price }}</p>
    </div>

    <!-- Add button turns into a quantity stepper once in the cart. -->
    <div class="item-action">
      <button v-if="qty === 0" type="button" class="btn btn-primary add-btn" @click="add(item)">
        Add
      </button>

      <div v-else class="stepper" role="group" :aria-label="`Quantity of ${item.name}`">
        <button type="button" class="step" aria-label="Decrease" @click="decrement(item.id)">−</button>
        <span class="step-qty">{{ qty }}</span>
        <button type="button" class="step" aria-label="Increase" @click="increment(item.id)">+</button>
      </div>
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
.item-fallback { font-size: 1.4rem; font-weight: 800; color: var(--color-primary); }

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

.item-action { flex: none; }
.add-btn { padding: 9px 22px; font-size: 0.95rem; }

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
