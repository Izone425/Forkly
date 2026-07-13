<script setup>
// Full-page order tracking (/track/:orderId). Reached by deep link, bookmark, or the
// payment-success handoff. The tracking UI itself lives in OrderTracking.vue, shared
// with the slide-in TrackingDrawer — this view only supplies the page chrome and the
// signed-out redirect.
import { ref, onMounted } from 'vue'
import { useRoute, useRouter, RouterLink } from 'vue-router'
import OrderTracking from '../components/OrderTracking.vue'
import { getToken } from '../services/authApi.js'

const route = useRoute()
const router = useRouter()

const orderId = route.params.orderId

// Gate the child rather than the redirect: without this, OrderTracking's onMounted
// would fire a token-less fetch before router.replace() resolves.
const authed = ref(Boolean(getToken()))

// Fed by OrderTracking's `loaded` event, purely so the heading can show the order
// reference (FRK-####) once we know it.
const tracking = ref(null)

onMounted(() => {
  if (!authed.value) {
    router.replace({ name: 'login', query: { redirect: route.fullPath } })
  }
})
</script>

<template>
  <div class="track">
    <div class="track-card">
      <header class="track-head">
        <div>
          <p class="eyebrow">Order tracking</p>
          <h1>{{ tracking?.reference || ('Order #' + orderId) }}</h1>
        </div>
        <RouterLink class="btn btn-ghost btn-sm" :to="{ name: 'landing' }">Home</RouterLink>
      </header>

      <OrderTracking
        v-if="authed"
        :key="orderId"
        :order-id="orderId"
        @loaded="(t) => (tracking = t)"
      />
    </div>
  </div>
</template>

<style scoped>
.track { min-height: 100vh; background: var(--color-surface); display: flex; justify-content: center; padding: 40px 20px; }
.track-card { width: 100%; max-width: 460px; background: var(--color-bg); border: 1px solid var(--color-border); border-radius: var(--radius); box-shadow: var(--shadow-lg); padding: 24px 26px 26px; }

.track-head { display: flex; align-items: flex-start; justify-content: space-between; margin-bottom: 20px; }
.eyebrow { margin: 0 0 3px; font-size: 0.72rem; font-weight: 700; letter-spacing: 0.14em; text-transform: uppercase; color: var(--color-primary); }
.track-head h1 { margin: 0; font-size: 1.35rem; font-weight: 800; color: var(--color-ink); }
.btn-sm { padding: 6px 14px; font-size: 0.82rem; }
</style>
