<script setup>
// The live order-tracking UI: status banner, progress timeline, item summary.
//
// Chrome-free on purpose — it's hosted by BOTH the full page (views/TrackingView.vue)
// and the slide-in drawer (components/TrackingDrawer.vue), each of which supplies its
// own header. It polls the Tracker service itself and emits every payload back up via
// `loaded`, so the host can title itself from the order reference.
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { isTrackerApiConfigured, fetchTracking } from '../services/trackingApi.js'
import StatusIcon from './StatusIcon.vue'
import { money } from '../utils/orderStatus.js'
import { useToast } from '../stores/toast.js'

const props = defineProps({
  orderId: { type: [String, Number], required: true },
})
const emit = defineEmits(['loaded'])

const { show } = useToast()

const configured = isTrackerApiConfigured()

// Nothing left to poll for once the order reaches one of these.
const TERMINAL = ['Delivered', 'Cancelled']

const tracking = ref(null)
const loading = ref(true)
const error = ref('')
const localRemaining = ref(null) // ticks down between polls for a smooth countdown
let pollTimer = null
let tickTimer = null
let stopped = false
// Per-instance, so a freshly opened drawer establishes a baseline and doesn't toast
// a status it never actually saw change.
let prevStatus = null

function stopPolling() {
  stopped = true
  if (pollTimer) { clearInterval(pollTimer); pollTimer = null }
  if (tickTimer) { clearInterval(tickTimer); tickTimer = null }
}

const isDelivered = computed(() => tracking.value?.status === 'Delivered')

const countdown = computed(() => {
  if (localRemaining.value == null) return ''
  const s = Math.max(0, localRemaining.value)
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`
})

async function load() {
  if (!configured) { loading.value = false; return }
  try {
    const data = await fetchTracking(props.orderId)
    // Notification: pop a toast whenever the status advances.
    if (prevStatus && data.status !== prevStatus) show(data.message)
    prevStatus = data.status
    tracking.value = data
    localRemaining.value = data.remainingSeconds ?? null
    error.value = ''
    emit('loaded', data)

    // The Tracker only advances an order while it's OutForDelivery, so once it reports
    // Delivered/Cancelled there is no further transition a GET could drive — stopping
    // strands nothing. It matters because a delivered order leaves the bubble while the
    // drawer deliberately stays open: without this we'd poll a finished order forever.
    // Deliberately NOT in the catch() — a 404/500 is transient and must keep retrying.
    if (TERMINAL.includes(data.status)) stopPolling()
  } catch (e) {
    error.value = e?.status === 404 ? 'Order not found.' : (e?.message || 'Could not load tracking.')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  load()
  // !stopped: load() may already have resolved to a terminal status, in which case
  // starting the intervals here would orphan them.
  if (configured && !stopped) {
    pollTimer = setInterval(load, 4000)
    tickTimer = setInterval(() => {
      if (localRemaining.value != null && localRemaining.value > 0) localRemaining.value--
    }, 1000)
  }
})
onUnmounted(stopPolling)
</script>

<template>
  <div class="tracking">
    <p v-if="!configured" class="state state-warn">
      Tracker service isn't configured. Set <code>VITE_TRACKER_API_BASE</code> and start it.
    </p>
    <p v-else-if="loading" class="state">Loading tracking…</p>
    <p v-else-if="error" class="state state-error">{{ error }}</p>

    <template v-else-if="tracking">
      <!-- Current status banner -->
      <div class="banner" :class="{ done: isDelivered }">
        <span class="banner-msg">{{ tracking.message }}</span>
        <div v-if="tracking.remainingSeconds != null" class="eta">
          <span class="eta-time">{{ countdown }}</span>
          <span class="eta-label">arriving</span>
        </div>
      </div>

      <!-- Progress timeline -->
      <ol class="timeline">
        <li v-for="step in tracking.steps" :key="step.key" class="step" :class="step.state">
          <!-- The dot itself runs the pulse ring, so the icon animates on an inner
               element — one element can't run two animations. Only the step the order is
               actually on animates; five icons moving at once is noise. -->
          <span class="dot">
            <template v-if="step.state === 'done'">✓</template>
            <StatusIcon v-else :phase="step.key" :animated="step.state === 'current'" />
          </span>
          <span class="step-label">{{ step.label }}</span>
        </li>
      </ol>

      <!-- Order summary -->
      <div class="summary">
        <ul class="lines">
          <li v-for="(it, i) in tracking.items" :key="i">
            <span class="q">{{ it.quantity }}×</span> {{ it.itemName }}
          </li>
        </ul>
        <div class="total"><span>Total</span><span>{{ money(tracking.total) }}</span></div>
      </div>

      <p class="mock-note">Delivery time is simulated (no real GPS).</p>
    </template>
  </div>
</template>

<style scoped>
.state { text-align: center; color: var(--color-muted); padding: 40px 0; }
.state-error { color: #d33; }
.state-warn { color: #b45309; }
.state code { background: var(--color-surface); padding: 1px 6px; border-radius: 6px; }

/* Status banner */
.banner { display: flex; align-items: center; justify-content: space-between; gap: 12px; background: var(--color-primary-soft); border: 1px solid #cdd9f5; border-radius: var(--radius-sm); padding: 16px 18px; margin-bottom: 22px; }
.banner.done { background: #e7f6f0; border-color: #abebd3; }
.banner-msg { font-size: 1rem; font-weight: 700; color: var(--color-ink); }
.eta { flex: none; text-align: center; }
.eta-time { display: block; font-size: 1.4rem; font-weight: 800; color: var(--color-primary); font-variant-numeric: tabular-nums; }
.eta-label { font-size: 0.68rem; text-transform: uppercase; letter-spacing: 0.08em; color: var(--color-muted); }

/* Timeline */
.timeline { list-style: none; margin: 0 0 22px; padding: 0; }
.step { position: relative; display: flex; align-items: center; gap: 14px; padding: 10px 0 10px 4px; }
.step:not(:last-child)::before { content: ''; position: absolute; left: 19px; top: 34px; bottom: -6px; width: 2px; background: var(--color-border); }
.step.done:not(:last-child)::before { background: #059669; }
.dot { flex: none; width: 32px; height: 32px; border-radius: 50%; display: grid; place-items: center; font-size: 0.95rem; background: var(--color-surface); border: 2px solid var(--color-border); color: var(--color-muted); }
.step.done .dot { background: #059669; border-color: #059669; color: #fff; }
.step.current .dot { background: var(--color-primary); border-color: var(--color-primary); color: #fff; animation: pulse 1.6s infinite; }
@keyframes pulse { 0% { box-shadow: 0 0 0 0 rgba(37,99,235,0.5); } 70% { box-shadow: 0 0 0 8px rgba(37,99,235,0); } 100% { box-shadow: 0 0 0 0 rgba(37,99,235,0); } }
.step-label { font-weight: 600; color: var(--color-muted); }
.step.done .step-label, .step.current .step-label { color: var(--color-ink); }
.step.current .step-label { font-weight: 800; }

/* Summary */
.summary { border-top: 1px dashed var(--color-border); padding-top: 16px; }
.lines { list-style: none; margin: 0 0 12px; padding: 0; display: grid; gap: 6px; font-size: 0.92rem; color: var(--color-body); }
.lines .q { color: var(--color-primary); font-weight: 700; }
.total { display: flex; justify-content: space-between; font-weight: 800; color: var(--color-ink); font-size: 1.05rem; }

.mock-note { text-align: center; font-size: 0.74rem; color: var(--color-muted); margin: 16px 0 0; }
</style>
