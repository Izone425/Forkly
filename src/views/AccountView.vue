<script setup>
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
