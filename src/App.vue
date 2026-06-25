<script setup>
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import { initAuthBridge } from './services/authBridge.js'

// Subscribe to the IZZUWAN auth module's login-success signal (no UI here).
onMounted(initAuthBridge)
</script>

<template>
  <RouterView />
</template>
