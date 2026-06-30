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
// Full-page "My Account" (/account). The profile UI now lives in-app (ProfileView,
// migrated from the former Forkly-Auth app) — no iframe. We just frame it with the
// landing header and the floating "go to your order" cart.
import AppHeader from '../components/AppHeader.vue'
import FloatingCart from '../components/FloatingCart.vue'
import ProfileView from './ProfileView.vue'
</script>

<template>
  <div class="account-page">
    <AppHeader />
    <main class="account-main">
      <ProfileView />
    </main>
    <FloatingCart />
  </div>
</template>

<style scoped>
.account-page {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}
.account-main {
  flex: 1;
  width: 100%;
}
</style>
