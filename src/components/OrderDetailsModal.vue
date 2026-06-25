<script setup>
import { computed } from 'vue'

const props = defineProps({
  // When set, the modal is open and shows this order.
  order: { type: Object, default: null },
})

const emit = defineEmits(['close'])

const orderNo = computed(() => {
  if (!props.order) return ''
  const digits = String(props.order.orderId).replace(/\D/g, '').replace(/^0+/, '')
  return '#' + (digits || props.order.orderId)
})

const dateLabel = computed(() => {
  if (!props.order) return ''
  const d = new Date(props.order.orderDate)
  return d.toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric' })
})

function money(n) {
  return 'RM' + n.toFixed(2)
}
</script>

<template>
  <Teleport to="body">
    <div v-if="order" class="modal-overlay" @click.self="emit('close')">
      <div class="modal" role="dialog" aria-modal="true" :aria-label="`Order ${orderNo} details`">
        <div class="modal-head">
          <h3 class="modal-title">Order {{ orderNo }}</h3>
          <button type="button" class="modal-close" aria-label="Close" @click="emit('close')">×</button>
        </div>

        <ul class="modal-items">
          <li v-for="item in order.orderItems" :key="item.menuId" class="modal-item">
            <span class="modal-item-name">{{ item.menuName }} <span class="modal-item-qty">x{{ item.quantity }}</span></span>
            <span class="modal-item-price">{{ money(item.price * item.quantity) }}</span>
          </li>
        </ul>

        <dl class="modal-facts">
          <div class="modal-fact grand">
            <dt>Total</dt>
            <dd>{{ money(order.totalAmount) }}</dd>
          </div>
          <div class="modal-fact">
            <dt>Status</dt>
            <dd>{{ order.status }}</dd>
          </div>
          <div class="modal-fact">
            <dt>Date</dt>
            <dd>{{ dateLabel }}</dd>
          </div>
        </dl>

        <button type="button" class="btn btn-ghost btn-block modal-done" @click="emit('close')">
          Close
        </button>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 100;
  background: rgba(15, 23, 42, 0.5);
  backdrop-filter: blur(2px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}
.modal {
  width: 100%;
  max-width: 420px;
  background: var(--color-bg);
  border-radius: var(--radius);
  box-shadow: var(--shadow-lg);
  padding: 26px;
}

.modal-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 18px;
}
.modal-title { margin: 0; font-size: 1.3rem; font-weight: 800; color: var(--color-ink); }
.modal-close {
  border: none;
  background: var(--color-surface);
  color: var(--color-muted);
  width: 32px;
  height: 32px;
  border-radius: 50%;
  font-size: 1.3rem;
  line-height: 1;
  cursor: pointer;
}
.modal-close:hover { background: #fde8e8; color: #d33; }

.modal-items { list-style: none; margin: 0 0 16px; padding: 0; display: grid; gap: 12px; }
.modal-item { display: flex; align-items: center; justify-content: space-between; font-size: 1rem; }
.modal-item-name { color: var(--color-ink); }
.modal-item-qty { color: var(--color-primary); font-weight: 700; }
.modal-item-price { color: var(--color-body); font-weight: 600; }

.modal-facts {
  margin: 0 0 20px;
  padding-top: 16px;
  border-top: 1px solid var(--color-border);
  display: grid;
  gap: 8px;
}
.modal-fact { display: flex; align-items: baseline; justify-content: space-between; }
.modal-fact dt, .modal-fact dd { margin: 0; }
.modal-fact dt { color: var(--color-muted); }
.modal-fact dd { color: var(--color-ink); font-weight: 600; }
.modal-fact.grand dt, .modal-fact.grand dd { font-size: 1.2rem; font-weight: 800; color: var(--color-primary); }

.modal-done { padding: 12px 0; }
</style>
