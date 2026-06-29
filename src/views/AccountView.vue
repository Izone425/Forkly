<script setup>
// Full-page "My Account" (/account). The profile UI now lives in-app (ProfileView,
// migrated from the former Forkly-Auth app) — no iframe. We just frame it with the
// landing header and the floating "go to your order" cart.
import { watch } from 'vue'
import { useRouter } from 'vue-router'
import AppHeader from '../components/AppHeader.vue'
import FloatingCart from '../components/FloatingCart.vue'
import ProfileView from './ProfileView.vue'
import { useAuth } from '../stores/auth.js'

const router = useRouter()
const { isLoggedIn } = useAuth()

// You can't be on "My Account" while logged out — leave for the landing whenever
// auth clears, no matter which control did it (header Logout, profile card, token
// expiry). This keeps the page from "remaining" after sign-out.
watch(isLoggedIn, (loggedIn) => {
  if (!loggedIn) router.push('/')
})
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
