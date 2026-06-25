<script setup>
import { onMounted } from 'vue'
import { useMenu } from '../stores/menu.js'
import { useCart } from '../stores/cart.js'

// Full menu, loaded from the backend (amirul-menu via the Order service).
const { state: menu, load } = useMenu()
const { add, qtyOf } = useCart()

onMounted(load)
</script>

<template>
  <section class="section menu" id="menu">
    <div class="container">
      <h2 class="section-title">Our Menu</h2>
      <p class="section-sub">Browse the full menu and add items straight to your cart.</p>

      <p v-if="menu.loading && menu.items.length === 0" class="menu-state">Loading menu…</p>

      <div v-else class="menu-grid">
        <article v-for="item in menu.items" :key="item.id" class="menu-card">
          <div class="menu-emoji" aria-hidden="true">{{ item.emoji }}</div>
          <h3 class="menu-name">{{ item.name }}</h3>
          <p class="menu-desc">{{ item.description }}</p>
          <p class="menu-price">RM{{ item.price }}</p>

          <!-- Small add button on the bottom-right edge of the card. -->
          <button
            type="button"
            class="menu-add"
            :class="{ 'in-cart': qtyOf(item.id) > 0 }"
            :aria-label="`Add ${item.name} to cart`"
            @click="add(item)"
          >
            <span v-if="qtyOf(item.id) > 0" class="menu-add-qty">{{ qtyOf(item.id) }}</span>
            <span v-else aria-hidden="true">+</span>
          </button>
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

.menu-emoji { font-size: 2.6rem; line-height: 1; margin-bottom: 14px; }
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
.menu-add:hover { background: var(--color-primary-dark); transform: scale(1.08); }
.menu-add.in-cart { background: var(--color-primary-dark); }
.menu-add-qty { font-size: 1rem; font-weight: 800; }

@media (max-width: 900px) {
  .menu-grid { grid-template-columns: repeat(2, 1fr); }
}
@media (max-width: 600px) {
  .menu-grid { grid-template-columns: 1fr; }
}
</style>
