<script setup>
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import LoginDrawer from './components/LoginDrawer.vue'
import { useAuth } from './stores/auth.js'

// Restore the session from a stored token on load, so a refresh stays signed in.
const { hydrate } = useAuth()
onMounted(hydrate)
</script>

<template>
  <RouterView />
  <!-- Slide-in login drawer: answers `forkly:open-login-drawer`, renders the in-app form. -->
  <LoginDrawer />
</template>
