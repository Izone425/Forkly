<script setup>
import { computed } from 'vue'

const props = defineProps({
  order: { type: Object, required: true },
})

const emit = defineEmits(['view', 'reorder'])

// "ORD-000102" -> "#102"
const orderNo = computed(() => {
  const digits = String(props.order.orderId).replace(/\D/g, '').replace(/^0+/, '')
  return '#' + (digits || props.order.orderId)
})

const itemCount = computed(() =>
  props.order.orderItems.reduce((n, i) => n + i.quantity, 0),
)

const dateLabel = computed(() => {
  const d = new Date(props.order.orderDate)
  const date = d.toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
  const time = d.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit', hour12: true })
  return `${date} • ${time}`
})

const statusClass = computed(() => 'status-' + props.order.status.toLowerCase())
const total = computed(() => 'RM' + props.order.totalAmount.toFixed(2))
</script>

<template>
  <article class="history-card">
    <div class="history-head">
      <h3 class="history-no">Order {{ orderNo }}</h3>
      <span class="status-badge" :class="statusClass">{{ order.status }}</span>
    </div>

    <p class="history-date">{{ dateLabel }}</p>

    <div class="history-meta">
      <span class="history-items">{{ itemCount }} {{ itemCount === 1 ? 'Item' : 'Items' }}</span>
      <span class="history-total">{{ total }}</span>
    </div>

    <div class="history-actions">
      <button type="button" class="btn btn-ghost" @click="emit('view', order)">
        View Details
      </button>
      <button type="button" class="btn btn-primary" @click="emit('reorder', order)">
        Reorder
      </button>
    </div>
  </article>
</template>

<style scoped>
.history-card {
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  box-shadow: var(--shadow-sm);
  padding: 20px 22px;
  transition: box-shadow 0.15s ease, border-color 0.15s ease, transform 0.15s ease;
}
.history-card:hover {
  box-shadow: var(--shadow);
  border-color: #cdd9f5;
  transform: translateY(-2px);
}

.history-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 4px;
}
.history-no { margin: 0; font-size: 1.1rem; font-weight: 800; color: var(--color-ink); }

.status-badge {
  font-size: 0.74rem;
  font-weight: 700;
  letter-spacing: 0.4px;
  text-transform: uppercase;
  padding: 4px 12px;
  border-radius: 999px;
}
.status-completed { color: #047857; background: #d1fae5; }
.status-preparing { color: #b45309; background: #fef3c7; }
.status-cancelled { color: #b91c1c; background: #fee2e2; }

.history-date { margin: 0 0 14px; font-size: 0.9rem; color: var(--color-muted); }

.history-meta {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  padding: 12px 0;
  border-top: 1px solid var(--color-border);
  margin-bottom: 16px;
}
.history-items { font-size: 0.92rem; color: var(--color-body); font-weight: 600; }
.history-total { font-size: 1.25rem; font-weight: 800; color: var(--color-primary); }

.history-actions { display: flex; gap: 10px; }
.history-actions .btn { flex: 1; padding: 10px 0; font-size: 0.95rem; text-align: center; }
</style>
