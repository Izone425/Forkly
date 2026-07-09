<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { isKitchenApiConfigured, fetchQueue, setStatus } from '../services/kitchenApi.js'
import { useAuth } from '../stores/auth.js'

const router = useRouter()
const { logout } = useAuth()
const configured = isKitchenApiConfigured()

const tickets = ref([])
const loading = ref(true)
const error = ref('')
const updating = ref(null) // orderId currently being advanced
let timer = null

// Board columns and the action that moves a ticket to the next column.
// Queue tickets are all paid; a freshly-paid order arrives as "Pending" (the New column).
const columns = [
  { key: 'Pending', title: 'New', action: { to: 'Preparing', label: 'Start preparing' } },
  { key: 'Preparing', title: 'Preparing', action: { to: 'Completed', label: 'Mark ready' } },
  { key: 'Completed', title: 'Ready', action: { to: 'OutForDelivery', label: 'Out for delivery' } },
  { key: 'OutForDelivery', title: 'Out for delivery', action: null },
]

const byStatus = computed(() => {
  const map = { Pending: [], Preparing: [], Completed: [], OutForDelivery: [] }
  for (const t of tickets.value) (map[t.status] || (map[t.status] = [])).push(t)
  return map
})

async function load() {
  if (!configured) { loading.value = false; return }
  try {
    tickets.value = await fetchQueue()
    error.value = ''
  } catch (e) {
    error.value = e?.message || 'Could not load the kitchen queue.'
  } finally {
    loading.value = false
  }
}

async function advance(ticket, to) {
  updating.value = ticket.orderId
  try {
    await setStatus(ticket.orderId, to)
    await load()
  } catch (e) {
    error.value = e?.message || 'Could not update the order.'
  } finally {
    updating.value = null
  }
}

function minutesAgo(iso) {
  const m = Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 60000))
  return m === 0 ? 'just now' : `${m}m ago`
}

function onLogout() {
  logout()
  router.push('/')
}

onMounted(() => {
  load()
  if (configured) timer = setInterval(load, 4000) // poll the queue
})
onUnmounted(() => { if (timer) clearInterval(timer) })
</script>

<template>
  <div class="kds">
    <header class="kds-head">
      <div>
        <span class="kds-brand">Forkly Kitchen</span>
        <span class="kds-sub">Crew display · live</span>
      </div>
      <div class="kds-head-right">
        <span class="live-dot" aria-hidden="true"></span>
        <span class="live-text">Auto-refresh</span>
        <button type="button" class="btn btn-ghost btn-sm" @click="onLogout">Log out</button>
      </div>
    </header>

    <p v-if="!configured" class="kds-state kds-warn">
      Kitchen service isn't configured. Set <code>VITE_KITCHEN_API_BASE</code> and start the kitchen service.
    </p>
    <p v-else-if="loading" class="kds-state">Loading kitchen queue…</p>

    <template v-else>
      <p v-if="error" class="kds-error" role="alert">{{ error }}</p>

      <div class="board">
        <section v-for="col in columns" :key="col.key" class="col" :class="'col-' + col.key.toLowerCase()">
          <header class="col-head">
            <span>{{ col.title }}</span>
            <span class="col-count">{{ byStatus[col.key].length }}</span>
          </header>

          <div class="col-body">
            <p v-if="byStatus[col.key].length === 0" class="col-empty">—</p>

            <article v-for="t in byStatus[col.key]" :key="t.orderId" class="ticket">
              <div class="ticket-top">
                <span class="ticket-ref">{{ t.reference || ('#' + t.orderId) }}</span>
                <span class="ticket-time">{{ minutesAgo(t.placedAt) }}</span>
              </div>
              <ul class="ticket-items">
                <li v-for="(it, i) in t.items" :key="i">
                  <span class="qty">{{ it.quantity }}×</span> {{ it.itemName }}
                </li>
              </ul>
              <button
                v-if="col.action"
                type="button"
                class="ticket-btn"
                :disabled="updating === t.orderId"
                @click="advance(t, col.action.to)"
              >
                {{ updating === t.orderId ? 'Updating…' : col.action.label }}
              </button>
              <p v-else class="ticket-out">🛵 Picked up — on the way</p>
            </article>
          </div>
        </section>
      </div>
    </template>
  </div>
</template>

<style scoped>
.kds { min-height: 100vh; background: #0f172a; color: #e2e8f0; padding: 18px 20px 40px; }

.kds-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 18px; }
.kds-brand { font-size: 1.3rem; font-weight: 800; color: #fff; }
.kds-sub { margin-left: 12px; font-size: 0.82rem; color: #94a3b8; }
.kds-head-right { display: flex; align-items: center; gap: 10px; }
.live-dot { width: 9px; height: 9px; border-radius: 50%; background: #22c55e; box-shadow: 0 0 0 0 rgba(34,197,94,0.6); animation: pulse 1.8s infinite; }
@keyframes pulse { 0% { box-shadow: 0 0 0 0 rgba(34,197,94,0.5); } 70% { box-shadow: 0 0 0 7px rgba(34,197,94,0); } 100% { box-shadow: 0 0 0 0 rgba(34,197,94,0); } }
.live-text { font-size: 0.8rem; color: #94a3b8; margin-right: 6px; }
.kds .btn-ghost { background: #1e293b; color: #e2e8f0; border-color: #334155; }
.btn-sm { padding: 6px 14px; font-size: 0.82rem; }

.kds-state { text-align: center; color: #94a3b8; padding: 60px 0; }
.kds-warn { color: #fbbf24; }
.kds-state code { background: #1e293b; padding: 1px 6px; border-radius: 6px; }
.kds-error { background: #7f1d1d; color: #fecaca; padding: 10px 14px; border-radius: 10px; margin: 0 0 14px; }

.board { display: grid; grid-template-columns: repeat(4, 1fr); gap: 14px; }
.col { background: #1e293b; border-radius: 14px; overflow: hidden; display: flex; flex-direction: column; }
.col-head { display: flex; align-items: center; justify-content: space-between; padding: 12px 14px; font-weight: 700; font-size: 0.82rem; letter-spacing: 0.06em; text-transform: uppercase; color: #fff; }
.col-count { background: rgba(255,255,255,0.18); border-radius: 999px; padding: 1px 9px; font-size: 0.78rem; }
.col-pending .col-head { background: #2563eb; }
.col-preparing .col-head { background: #d97706; }
.col-completed .col-head { background: #059669; }
.col-outfordelivery .col-head { background: #7c3aed; }
.col-body { padding: 12px; display: flex; flex-direction: column; gap: 12px; min-height: 120px; }
.col-empty { text-align: center; color: #475569; margin: 16px 0; }

.ticket { background: #0f172a; border: 1px solid #334155; border-radius: 12px; padding: 12px 13px; }
.ticket-top { display: flex; align-items: baseline; justify-content: space-between; margin-bottom: 8px; }
.ticket-ref { font-weight: 800; color: #fff; font-size: 0.98rem; }
.ticket-time { font-size: 0.74rem; color: #94a3b8; }
.ticket-items { list-style: none; margin: 0 0 10px; padding: 0; display: grid; gap: 4px; font-size: 0.9rem; color: #cbd5e1; }
.ticket-items .qty { color: #fff; font-weight: 700; }
.ticket-btn { width: 100%; border: none; border-radius: 9px; padding: 9px 0; font-family: inherit; font-weight: 700; font-size: 0.86rem; color: #fff; background: #2563eb; cursor: pointer; transition: filter 0.15s ease; }
.ticket-btn:hover { filter: brightness(1.1); }
.ticket-btn:disabled { opacity: 0.6; cursor: default; }
.ticket-out { margin: 0; font-size: 0.8rem; color: #a78bfa; text-align: center; }

@media (max-width: 900px) { .board { grid-template-columns: repeat(2, 1fr); } }
@media (max-width: 520px) { .board { grid-template-columns: 1fr; } }
</style>
