<script setup>
import { ref, onMounted } from 'vue'
import OrderHistoryCard from './OrderHistoryCard.vue'
import OrderDetailsModal from './OrderDetailsModal.vue'
import { getOrderHistory } from '../services/orderHistoryApi.js'
import { useAuth } from '../stores/auth.js'

const emit = defineEmits(['reorder'])

const { state: auth } = useAuth()

const orders = ref([])
const loading = ref(true)
const error = ref('')
const selectedOrder = ref(null)

onMounted(async () => {
  try {
    // userId comes from the signed-in profile when available (auth handled by
    // izone-user-management-FE). Falls back to all mock orders during dev.
    orders.value = await getOrderHistory(auth.user?.id)
  } catch (e) {
    error.value = e?.message || 'Could not load order history.'
  } finally {
    loading.value = false
  }
})

function viewDetails(order) {
  selectedOrder.value = order
}
function closeDetails() {
  selectedOrder.value = null
}
function reorder(order) {
  emit('reorder', order)
}
</script>

<template>
  <section class="history">
    <h2 class="history-title">Order History</h2>
    <p class="history-sub">Your previous orders — view details or reorder in one tap.</p>

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

    <OrderDetailsModal :order="selectedOrder" @close="closeDetails" />
  </section>
</template>

<style scoped>
.history { margin-top: 48px; }
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
