<script setup>
import { computed } from 'vue'
import AppHeader from '../components/AppHeader.vue'
import FloatingCart from '../components/FloatingCart.vue'
import { config } from '../config.js'

// Full-page "My Account": embeds Forkly-Auth's /profile page (the account UI owned by
// the user-management module). No ?embed=1 so it renders the full layout — but since
// it runs inside an iframe, Forkly-Auth still posts profile-updated / logout to us.
//
// Those postMessages (logout → return to landing, profile-updated → refresh header)
// are handled app-wide in services/authBridge.js, so this view just renders the frame.

const src = computed(() => {
  if (!config.loginUrl) return 'about:blank'
  const url = new URL(config.loginUrl)
  url.pathname = '/profile'
  url.searchParams.set('from', 'forkly-landing')
  url.searchParams.set('return_to', window.location.origin)
  return url.toString()
})
</script>

<template>
  <div class="account-page">
    <AppHeader />
    <iframe class="account-frame" :src="src" title="My account" />
    <!-- Quick "go to your order" circle, same as on the landing. -->
    <FloatingCart />
  </div>
</template>

<style scoped>
.account-page {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}
.account-frame {
  flex: 1;
  width: 100%;
  border: none;
  display: block;
}
</style>
