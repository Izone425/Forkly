<script setup>
// Orders management: every order across all users, filterable by status, with an
// inline status dropdown that PATCHes the order. Mirrors reportApi's graceful
// fallback — when no Order Service is configured the list is empty and labelled.
import { ref, onMounted } from 'vue'
import { listAllOrders, updateOrderStatus, isOrdersApiConfigured } from '../services/adminApi.js'
import { useToast } from '../stores/toast.js'

// Fulfilment statuses — must match Forkly.OrderService Models/OrderStatus.All.
// Payment is tracked separately (PaymentStatus) and shown as a read-only badge.
const ORDER_STATUSES = [
  'Pending', 'Preparing', 'Completed', 'OutForDelivery', 'Delivered', 'Cancelled',
]

const paymentLabel = (s) => (s === 'Paid' ? 'Paid' : 'Unpaid')
const paymentClass = (s) => `pay-${(s || 'unpaid').toLowerCase()}`

const { show } = useToast()

const orders = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const statusFilter = ref('')
const loading = ref(false)
const error = ref('')
const isLive = ref(true)
const busyId = ref(null)
const expandedId = ref(null)

const apiConfigured = isOrdersApiConfigured()

// Toggle the inline item-detail panel for a row (single-open).
function toggleExpand(id) {
  expandedId.value = expandedId.value === id ? null : id
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const result = await listAllOrders({
      status: statusFilter.value,
      page: page.value,
      pageSize: pageSize.value,
    })
    orders.value = result.items
    total.value = result.total
    isLive.value = result.isLive
  } catch (err) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

function onFilter() {
  page.value = 1
  load()
}

function pageCount() {
  return Math.max(1, Math.ceil(total.value / pageSize.value))
}

function goTo(next) {
  const target = Math.min(Math.max(1, next), pageCount())
  if (target === page.value) return
  page.value = target
  load()
}

function money(value) {
  return `RM ${Number(value).toFixed(2)}`
}

function shortDate(iso) {
  return new Date(iso).toLocaleDateString(undefined, { day: '2-digit', month: 'short', year: 'numeric' })
}

async function changeStatus(order, event) {
  const next = event.target.value
  if (next === order.status) return
  busyId.value = order.id
  try {
    const updated = await updateOrderStatus(order.id, next)
    const i = orders.value.findIndex((o) => o.id === updated.id)
    if (i !== -1) orders.value[i] = updated
    show(`${updated.reference || `Order #${updated.id}`} → ${updated.status}`)
  } catch (err) {
    show(err.message)
    event.target.value = order.status // revert the dropdown on failure
  } finally {
    busyId.value = null
  }
}

onMounted(load)
</script>

<template>
  <section class="orders">
    <header class="orders-head">
      <h1 class="orders-title">Orders</h1>
      <div class="orders-filter">
        <label class="orders-filter-label" for="status-filter">Status</label>
        <select id="status-filter" v-model="statusFilter" class="orders-select" @change="onFilter">
          <option value="">All</option>
          <option v-for="s in ORDER_STATUSES" :key="s" :value="s">{{ s }}</option>
        </select>
      </div>
    </header>

    <p v-if="!apiConfigured" class="notice notice-info">
      Order Service not connected (set <code>VITE_ORDER_API_BASE</code>). Showing an empty list.
    </p>
    <p v-if="error" class="notice notice-error">{{ error }}</p>

    <div class="orders-table-wrap">
      <table class="orders-table">
        <thead>
          <tr>
            <th>Reference</th>
            <th>User</th>
            <th>Placed</th>
            <th>Items</th>
            <th class="col-total">Total</th>
            <th>Payment</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="7" class="orders-empty">Loading…</td>
          </tr>
          <tr v-else-if="!orders.length">
            <td colspan="7" class="orders-empty">No orders found.</td>
          </tr>
          <template v-for="order in orders" v-else :key="order.id">
            <tr>
              <td class="cell-ref">{{ order.reference || `#${order.id}` }}</td>
              <td class="cell-muted">user #{{ order.userId }}</td>
              <td class="cell-muted">{{ shortDate(order.createdAt) }}</td>
              <td>
                <button
                  type="button"
                  class="items-toggle"
                  :aria-expanded="expandedId === order.id"
                  @click="toggleExpand(order.id)"
                >
                  {{ order.items?.length ?? 0 }} item{{ (order.items?.length ?? 0) === 1 ? '' : 's' }}
                  <span class="expando" :class="{ open: expandedId === order.id }">▾</span>
                </button>
              </td>
              <td class="col-total">{{ money(order.total) }}</td>
              <td>
                <span class="pay-badge" :class="paymentClass(order.paymentStatus)">
                  {{ paymentLabel(order.paymentStatus) }}
                </span>
              </td>
              <td>
                <select
                  class="orders-select status-select"
                  :value="order.status"
                  :disabled="busyId === order.id"
                  @change="changeStatus(order, $event)"
                >
                  <option v-for="s in ORDER_STATUSES" :key="s" :value="s">{{ s }}</option>
                </select>
              </td>
            </tr>
            <tr v-if="expandedId === order.id" class="detail-row">
              <td :colspan="7">
                <ul class="detail-items">
                  <li v-for="it in order.items" :key="it.id" class="detail-item">
                    <span class="di-name">{{ it.itemName }} <span class="di-qty">×{{ it.quantity }}</span></span>
                    <span class="di-unit">{{ money(it.price) }}</span>
                    <span class="di-line">{{ money(it.price * it.quantity) }}</span>
                  </li>
                </ul>
                <div class="detail-totals">
                  <span>Subtotal {{ money(order.subtotal) }}</span>
                  <span>SST {{ money(order.sst) }}</span>
                  <span class="dt-total">Total {{ money(order.total) }}</span>
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <footer class="orders-foot">
      <span class="orders-count">{{ total }} order{{ total === 1 ? '' : 's' }}</span>
      <div class="orders-pager">
        <button type="button" class="row-btn" :disabled="page <= 1 || loading" @click="goTo(page - 1)">
          Prev
        </button>
        <span class="orders-page">Page {{ page }} / {{ pageCount() }}</span>
        <button type="button" class="row-btn" :disabled="page >= pageCount() || loading" @click="goTo(page + 1)">
          Next
        </button>
      </div>
    </footer>
  </section>
</template>

<style scoped>
.orders { max-width: 1040px; }
.orders-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 18px;
  flex-wrap: wrap;
}
.orders-title { margin: 0; font-size: 1.5rem; color: var(--color-ink); }
.orders-filter { display: inline-flex; align-items: center; gap: 8px; }
.orders-filter-label { font-size: 0.85rem; font-weight: 600; color: var(--color-ink); }
.orders-select {
  font-family: inherit;
  font-size: 0.9rem;
  padding: 8px 12px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  background: #fff;
  cursor: pointer;
}
.orders-select:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}
.status-select:disabled { opacity: 0.5; cursor: not-allowed; }

.orders-table-wrap {
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: 14px;
  overflow: hidden;
  box-shadow: var(--shadow-sm);
}
.orders-table { width: 100%; border-collapse: collapse; }
.orders-table th,
.orders-table td {
  text-align: left;
  padding: 13px 16px;
  border-bottom: 1px solid var(--color-border);
  font-size: 0.93rem;
}
.orders-table th {
  font-size: 0.78rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--color-muted);
  background: var(--color-bg, #f6f7fb);
}
.orders-table tbody tr:last-child td { border-bottom: none; }
.cell-ref { font-weight: 700; color: var(--color-ink); }
.cell-muted { color: var(--color-muted); }
.col-total { font-weight: 700; color: var(--color-ink); white-space: nowrap; }
.orders-empty { text-align: center; color: var(--color-muted); padding: 28px; }

/* Read-only payment badge */
.pay-badge {
  display: inline-block;
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.2px;
  padding: 3px 10px;
  border-radius: 999px;
  text-transform: uppercase;
}
.pay-paid { color: var(--color-success); background: #ecfdf5; border: 1px solid #a7f3d0; }
.pay-unpaid { color: #92400e; background: #fffbeb; border: 1px solid #fde68a; }

/* Expandable item-detail row */
.items-toggle {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font: inherit;
  font-size: 0.9rem;
  color: var(--color-body);
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
}
.items-toggle:hover { color: var(--color-primary); }
.expando { font-size: 0.75rem; color: var(--color-muted); transition: transform 0.15s ease; }
.expando.open { transform: rotate(180deg); color: var(--color-primary); }

.detail-row td { background: var(--color-bg, #f6f7fb); padding: 12px 16px; }
.detail-items { list-style: none; margin: 0 0 10px; padding: 0; display: flex; flex-direction: column; gap: 6px; }
.detail-item {
  display: grid;
  grid-template-columns: 1fr auto auto;
  gap: 16px;
  align-items: baseline;
  font-size: 0.9rem;
  color: var(--color-ink);
}
.di-qty { color: var(--color-muted); }
.di-unit { color: var(--color-muted); white-space: nowrap; text-align: right; }
.di-line { font-weight: 600; white-space: nowrap; text-align: right; min-width: 84px; }
.detail-totals {
  display: flex;
  justify-content: flex-end;
  gap: 18px;
  padding-top: 8px;
  border-top: 1px solid var(--color-border);
  font-size: 0.88rem;
  color: var(--color-muted);
}
.dt-total { font-weight: 800; color: var(--color-primary); }

.row-btn {
  font: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-body);
  background: #fff;
  border: 1px solid var(--color-border);
  padding: 7px 13px;
  border-radius: 9px;
  cursor: pointer;
}
.row-btn:hover:not(:disabled) { border-color: var(--color-primary); color: var(--color-primary); }
.row-btn:disabled { opacity: 0.5; cursor: not-allowed; }

.orders-foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 16px;
}
.orders-count { color: var(--color-muted); font-size: 0.9rem; }
.orders-pager { display: inline-flex; align-items: center; gap: 12px; }
.orders-page { font-size: 0.9rem; color: var(--color-body); }

.notice { padding: 10px 14px; border-radius: var(--radius-sm); font-size: 0.88rem; margin: 0 0 16px; }
.notice-error { background: #fef2f2; color: var(--color-danger); border: 1px solid #fecaca; }
.notice-info { background: #eff6ff; color: #1d4ed8; border: 1px solid #bfdbfe; }
.notice-info code { font-family: ui-monospace, monospace; font-size: 0.85em; }
</style>
