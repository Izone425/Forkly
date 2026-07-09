<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'
import AppHeader from '../components/AppHeader.vue'
import MenuItemCard from '../components/MenuItemCard.vue'
import CartSummary from '../components/CartSummary.vue'
import OrderHistoryList from '../components/OrderHistoryList.vue'
import ToastHost from '../components/ToastHost.vue'
import { useMenu } from '../stores/menu.js'
import { useCart } from '../stores/cart.js'
import { useToast } from '../stores/toast.js'

const { state: menu, load, startPolling, stopPolling } = useMenu()
const cart = useCart()
const toast = useToast()

const gridRef = ref(null)

onMounted(() => {
  load()
  startPolling()
})
onUnmounted(stopPolling)

// Reorder: MERGE a previous order into the current cart (never replace), then
// scroll back to the cart summary and confirm with a toast. The cart stays fully
// editable afterwards. Each line is resolved against the live menu so prices and
// the menu steppers stay correct; the historical line is the fallback.
function reorder(order) {
  const reorderItems = order.orderItems.map((i) => ({
    menuId: i.menuId,
    name: i.menuName,
    quantity: i.quantity,
    price: i.price,
  }))

  cart.mergeReorder(reorderItems, (id) => menu.items.find((m) => m.id === id))

  // Scroll down to the cart summary (it now sits below Most Recent Orders).
  gridRef.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  toast.show('Previous order added to cart')
}

// Group menu items by category, preserving first-seen order.
const grouped = computed(() => {
  const groups = []
  const byKey = {}
  for (const item of menu.items) {
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
    <!-- Same header as the landing page: keep the login/profile button AND the cart. -->
    <AppHeader show-cart />

    <main class="container order-main">
      <div class="order-intro">
        <h1 class="order-title">Place your order</h1>
        <p class="order-sub">Browse the menu, add items to your cart, and check out.</p>
      </div>

      <!-- Most Recent Orders (top). Reorder merges into the cart below. -->
      <OrderHistoryList @reorder="reorder" />

      <div class="order-grid" ref="gridRef">
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

    <ToastHost />
  </div>
</template>

<style scoped>
.order-page { min-height: 100vh; background: var(--color-surface); }

.order-main { padding: 40px 24px 72px; }
.order-intro { margin-bottom: 28px; }
.order-title { margin: 0 0 6px; font-size: clamp(1.6rem, 4vw, 2.1rem); font-weight: 800; color: var(--color-ink); letter-spacing: -0.4px; }
.order-sub { margin: 0; color: var(--color-muted); }

.order-grid {
  display: grid;
  grid-template-columns: 1fr 360px;
  align-items: start;
  gap: 32px;
  scroll-margin-top: 90px; /* offset sticky header when scrolled to after reorder */
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
