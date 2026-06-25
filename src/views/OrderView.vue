<script setup>
import { computed } from 'vue'
import { RouterLink } from 'vue-router'
import BrandLogo from '../components/BrandLogo.vue'
import MenuItemCard from '../components/MenuItemCard.vue'
import CartSummary from '../components/CartSummary.vue'
import { MENU } from '../data/menu.js'
import { useCart } from '../stores/cart.js'

const { count } = useCart()

// Group menu items by category, preserving first-seen order.
const grouped = computed(() => {
  const groups = []
  const byKey = {}
  for (const item of MENU) {
    if (!byKey[item.category]) {
      byKey[item.category] = { category: item.category, items: [] }
      groups.push(byKey[item.category])
    }
    byKey[item.category].items.push(item)
  }
  return groups
})
</script>

<template>
  <div class="order-page">
    <!-- Top bar -->
    <header class="order-bar">
      <div class="container bar-inner">
        <RouterLink to="/" class="bar-brand" aria-label="Forkly home">
          <BrandLogo />
        </RouterLink>

        <div class="bar-cart" :class="{ active: count > 0 }" aria-live="polite">
          <span class="bar-cart-icon" aria-hidden="true">🛒</span>
          <span>{{ count }} {{ count === 1 ? 'item' : 'items' }}</span>
        </div>
      </div>
    </header>

    <main class="container order-main">
      <div class="order-intro">
        <h1 class="order-title">Place your order</h1>
        <p class="order-sub">Browse the menu, add items to your cart, and check out.</p>
      </div>

      <div class="order-grid">
        <!-- Menu -->
        <section class="menu-col" aria-label="Menu">
          <div v-for="group in grouped" :key="group.category" class="menu-group">
            <h2 class="group-title">{{ group.category }}</h2>
            <div class="group-items">
              <MenuItemCard v-for="item in group.items" :key="item.id" :item="item" />
            </div>
          </div>
        </section>

        <!-- Cart -->
        <CartSummary class="cart-col" />
      </div>
    </main>
  </div>
</template>

<style scoped>
.order-page { min-height: 100vh; background: var(--color-surface); }

.order-bar {
  position: sticky;
  top: 0;
  z-index: 10;
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(10px);
  border-bottom: 1px solid var(--color-border);
}
.bar-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding-top: 14px;
  padding-bottom: 14px;
}
.bar-brand { display: inline-flex; }
.bar-cart {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  font-size: 0.92rem;
  font-weight: 600;
  color: var(--color-body);
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: 999px;
  box-shadow: var(--shadow-sm);
  transition: color 0.15s ease, border-color 0.15s ease;
}
.bar-cart.active { color: var(--color-primary); border-color: #cdd9f5; }
.bar-cart-icon { font-size: 1.1rem; }

.order-main { padding: 40px 24px 72px; }
.order-intro { margin-bottom: 28px; }
.order-title { margin: 0 0 6px; font-size: clamp(1.6rem, 4vw, 2.1rem); font-weight: 800; color: var(--color-ink); letter-spacing: -0.4px; }
.order-sub { margin: 0; color: var(--color-muted); }

.order-grid {
  display: grid;
  grid-template-columns: 1fr 360px;
  align-items: start;
  gap: 32px;
}

.menu-group { margin-bottom: 32px; }
.menu-group:last-child { margin-bottom: 0; }
.group-title {
  margin: 0 0 14px;
  font-size: 0.82rem;
  font-weight: 700;
  letter-spacing: 1px;
  text-transform: uppercase;
  color: var(--color-muted);
}
.group-items { display: grid; gap: 14px; }

@media (max-width: 880px) {
  .order-grid { grid-template-columns: 1fr; }
  .cart-col { position: static; order: -1; }
}
</style>
