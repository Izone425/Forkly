<script setup>
import { computed, onMounted, ref } from 'vue'
import AppHeader from '../components/AppHeader.vue'
import MenuItemCard from '../components/MenuItemCard.vue'
import CartSummary from '../components/CartSummary.vue'
import OrderHistoryList from '../components/OrderHistoryList.vue'
import ToastHost from '../components/ToastHost.vue'
import { useMenu } from '../stores/menu.js'
import { useCart } from '../stores/cart.js'
import { useToast } from '../stores/toast.js'

const { state: menu, load } = useMenu()
const cart = useCart()
const toast = useToast()

const gridRef = ref(null)

// Filtering state: a free-text search and the currently selected category tab.
// Default 'All' means search (when typed) spans the whole menu; picking a tab
// narrows both the browse view and the search to that category.
const query = ref('')
const activeCategory = ref('All')

onMounted(load)

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

// Category tabs: "All" plus one per distinct category (first-seen order), each
// with a count so the customer sees how many items live under each tab.
const categories = computed(() => {
  const counts = {}
  const order = []
  for (const item of menu.items) {
    if (!(item.category in counts)) {
      counts[item.category] = 0
      order.push(item.category)
    }
    counts[item.category]++
  }
  return [
    { name: 'All', count: menu.items.length },
    ...order.map((c) => ({ name: c, count: counts[c] })),
  ]
})

// Visible items grouped by category, after applying the active tab + search.
// Both filters combine (AND); with the default "All" tab the search is global.
const visibleGroups = computed(() => {
  const q = query.value.trim().toLowerCase()
  const groups = []
  const byKey = {}
  for (const item of menu.items) {
    if (activeCategory.value !== 'All' && item.category !== activeCategory.value) continue
    if (q) {
      const haystack = `${item.name} ${item.description || ''} ${item.category || ''}`.toLowerCase()
      if (!haystack.includes(q)) continue
    }
    if (!byKey[item.category]) {
      byKey[item.category] = { category: item.category, items: [] }
      groups.push(byKey[item.category])
    }
    byKey[item.category].items.push(item)
  }
  return groups
})

const resultCount = computed(() =>
  visibleGroups.value.reduce((n, g) => n + g.items.length, 0),
)

// Only show the "no results" empty state when there IS a menu but the current
// filters exclude everything — not while loading or when the menu is empty.
const noResults = computed(
  () => !menu.loading && !menu.error && menu.items.length > 0 && resultCount.value === 0,
)

const isFiltering = computed(() => query.value.trim() !== '' || activeCategory.value !== 'All')

function resetFilters() {
  query.value = ''
  activeCategory.value = 'All'
}
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
          <!-- Sticky toolbar: search + category tabs. Stays pinned while scrolling. -->
          <div class="menu-toolbar">
            <div class="search">
              <svg class="search-icon" viewBox="0 0 24 24" aria-hidden="true">
                <path
                  d="M21 21l-4.35-4.35M11 19a8 8 0 1 0 0-16 8 8 0 0 0 0 16z"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2"
                  stroke-linecap="round"
                />
              </svg>
              <input
                v-model="query"
                type="search"
                class="search-input"
                placeholder="Search the menu…"
                aria-label="Search the menu"
              />
              <button
                v-if="query"
                type="button"
                class="search-clear"
                aria-label="Clear search"
                @click="query = ''"
              >
                ×
              </button>
            </div>

            <div class="tabs" role="tablist" aria-label="Menu categories">
              <button
                v-for="c in categories"
                :key="c.name"
                type="button"
                role="tab"
                :aria-selected="activeCategory === c.name"
                class="tab"
                :class="{ active: activeCategory === c.name }"
                @click="activeCategory = c.name"
              >
                {{ c.name }}
                <span class="tab-count">{{ c.count }}</span>
              </button>
            </div>
          </div>

          <!-- States: loading / error / no-results / menu list. -->
          <p v-if="menu.loading" class="menu-note">Loading menu…</p>

          <p v-else-if="menu.error" class="menu-note menu-error">{{ menu.error }}</p>

          <p v-else-if="menu.items.length === 0" class="menu-note">
            The menu is currently unavailable. Please check back soon.
          </p>

          <div v-else-if="noResults" class="menu-empty">
            <p class="empty-title">No items found</p>
            <p class="empty-sub">Try a different search term or category.</p>
            <button type="button" class="btn btn-primary" @click="resetFilters">
              Clear filters
            </button>
          </div>

          <template v-else>
            <p v-if="isFiltering" class="result-count">
              {{ resultCount }} {{ resultCount === 1 ? 'item' : 'items' }}
            </p>
            <div v-for="group in visibleGroups" :key="group.category" class="menu-group">
              <h2 class="group-title">{{ group.category }}</h2>
              <div class="group-items">
                <MenuItemCard v-for="item in group.items" :key="item.id" :item="item" />
              </div>
            </div>
          </template>
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

/* --- Toolbar: search + category tabs, pinned below the site header --------- */
.menu-toolbar {
  position: sticky;
  top: 70px; /* tucks just under the sticky site header */
  z-index: 5;
  background: var(--color-surface);
  padding: 10px 0 14px;
  margin-bottom: 4px;
}

.search {
  position: relative;
  display: flex;
  align-items: center;
  margin-bottom: 12px;
}
.search-icon {
  position: absolute;
  left: 16px;
  width: 18px;
  height: 18px;
  color: var(--color-muted);
  pointer-events: none;
}
.search-input {
  width: 100%;
  font: inherit;
  font-size: 0.98rem;
  color: var(--color-ink);
  padding: 12px 42px 12px 44px;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  box-shadow: var(--shadow-sm);
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}
.search-input::placeholder { color: var(--color-muted); }
.search-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}
/* Hide the native search clear control; we render our own. */
.search-input::-webkit-search-cancel-button { -webkit-appearance: none; appearance: none; }
.search-clear {
  position: absolute;
  right: 12px;
  width: 26px;
  height: 26px;
  display: grid;
  place-items: center;
  border: none;
  border-radius: 50%;
  background: var(--color-surface);
  color: var(--color-muted);
  font-size: 1.2rem;
  line-height: 1;
  cursor: pointer;
  transition: background 0.15s ease, color 0.15s ease;
}
.search-clear:hover { background: var(--color-primary); color: #fff; }

.tabs {
  display: flex;
  gap: 8px;
  overflow-x: auto;
  padding-bottom: 4px;
  scrollbar-width: thin;
}
.tab {
  flex: none;
  display: inline-flex;
  align-items: center;
  gap: 7px;
  font: inherit;
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--color-body);
  padding: 8px 15px;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: 999px;
  cursor: pointer;
  white-space: nowrap;
  transition: color 0.15s ease, background 0.15s ease, border-color 0.15s ease;
}
.tab:hover { border-color: var(--color-primary); color: var(--color-primary); }
.tab.active {
  background: var(--color-primary);
  border-color: var(--color-primary);
  color: #fff;
}
.tab-count {
  font-size: 0.75rem;
  font-weight: 700;
  padding: 1px 7px;
  border-radius: 999px;
  background: var(--color-surface);
  color: var(--color-muted);
}
.tab.active .tab-count { background: rgba(255, 255, 255, 0.25); color: #fff; }

/* --- States ---------------------------------------------------------------- */
.menu-note {
  margin: 24px 0;
  padding: 28px 20px;
  text-align: center;
  color: var(--color-muted);
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
}
.menu-error { color: #b4232a; }

.menu-empty {
  margin: 24px 0;
  padding: 44px 20px;
  text-align: center;
  background: var(--color-bg);
  border: 1px dashed var(--color-border);
  border-radius: var(--radius);
}
.empty-title { margin: 0 0 4px; font-size: 1.1rem; font-weight: 700; color: var(--color-ink); }
.empty-sub { margin: 0 0 18px; color: var(--color-muted); }

.result-count { margin: 0 0 16px; font-size: 0.86rem; color: var(--color-muted); }

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

/* Keep the cart in view while browsing a long menu (desktop only). */
.cart-col { position: sticky; top: 90px; }

@media (max-width: 880px) {
  .order-grid { grid-template-columns: 1fr; }
  .cart-col { position: static; order: -1; }
  .menu-toolbar { top: 62px; }
}
</style>
