<script setup>
import { ref, onMounted, watch } from 'vue'
import OrderHistoryCard from './OrderHistoryCard.vue'
import OrderDetailsModal from './OrderDetailsModal.vue'
import { getOrderHistory } from '../services/orderHistoryApi.js'
import { useAuth } from '../stores/auth.js'

const emit = defineEmits(['reorder'])

const { state: auth, isLoggedIn } = useAuth()

const orders = ref([])
const loading = ref(true)
const error = ref('')
const selectedOrder = ref(null)
const loaded = ref(false)

// Recent orders belong to a specific user, so they are only loaded once signed
// in (via the IZZUWAN user module). Guests see nothing here.
async function loadRecentOrders() {
  if (!isLoggedIn.value || loaded.value) return
  loading.value = true
  error.value = ''
  try {
    // TODO: real call once available — GET /orders/user/{userId}/recent
    // (from the order service, keyed by the signed-in user's id).
    const all = await getOrderHistory(auth.user?.id)
    orders.value = all.slice(0, 3) // 3 most recent (service returns newest first)
    loaded.value = true
  } catch (e) {
    error.value = e?.message || 'Could not load recent orders.'
  } finally {
    loading.value = false
  }
}

onMounted(loadRecentOrders)

// Load when the user signs in; clear when they sign out.
watch(isLoggedIn, (signedIn) => {
  if (signedIn) {
    loadRecentOrders()
  } else {
    orders.value = []
    loaded.value = false
    selectedOrder.value = null
  }
})

function viewDetails(order) {
  selectedOrder.value = order
}
function closeDetails() {
  selectedOrder.value = null
}
function reorder(order) {
  // Close the details modal if it was open, then bubble up to the order page.
  selectedOrder.value = null
  emit('reorder', order)
}
</script>

<template>
  <!-- Only shown to signed-in users — recent orders come from their profile. -->
  <section v-if="isLoggedIn" class="history">
    <h2 class="history-title">Most Recent Orders</h2>
    <p class="history-sub">Your latest orders — view details or reorder in one tap.</p>

    <p v-if="loading" class="history-state">Loading your orders…</p>
    <p v-else-if="error" class="history-state">{{ error }}</p>
    <p v-else-if="orders.length === 0" class="history-state">
      You have no previous orders yet.
    </p>

    <div v-else class="history-grid">
      <OrderHistoryCard
        v-for="order in orders"
        :key="order.orderId"
        :order="order"
        @view="viewDetails"
        @reorder="reorder"
      />
    </div>

    <OrderDetailsModal :order="selectedOrder" @close="closeDetails" @reorder="reorder" />
  </section>
</template>

<style scoped>
.history { margin: 0 0 40px; }
.history-title {
  margin: 0 0 4px;
  font-size: clamp(1.4rem, 3.5vw, 1.8rem);
  font-weight: 800;
  color: var(--color-ink);
  letter-spacing: -0.3px;
}
.history-sub { margin: 0 0 24px; color: var(--color-muted); }

.history-state { color: var(--color-muted); }

.history-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 20px;
}
</style>
