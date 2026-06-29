<script setup>
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import { initAuthBridge } from './services/authBridge.js'
import LoginDrawer from './components/LoginDrawer.vue'

// Subscribe to the IZZUWAN auth module's login-success signal.
onMounted(initAuthBridge)
</script>

<template>
  <RouterView />
  <!-- IZZUWAN login drawer: answers `forkly:open-login-drawer`, embeds Forkly-Auth. -->
  <LoginDrawer />
</template>
