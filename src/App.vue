<script setup>
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import LoginDrawer from './components/LoginDrawer.vue'
import FloatingActions from './components/FloatingActions.vue'
import ToastHost from './components/ToastHost.vue'
import { useAuth } from './stores/auth.js'

// Restore the session from a stored token on load, so a refresh stays signed in.
const { hydrate } = useAuth()
onMounted(hydrate)
</script>

<template>
  <RouterView />

  <!-- Floating bubbles (bottom-right): the cart, and the active-order tracker that
       keeps /track/:orderId reachable from anywhere. Mounted once, app-wide — each
       decides for itself which routes it belongs on. -->
  <FloatingActions />

  <!-- Toasts, mounted once so ANY page can notify (order status changes, stock). -->
  <ToastHost />

  <!-- Slide-in login drawer: answers `forkly:open-login-drawer`, renders the in-app form. -->
  <LoginDrawer />
</template>
