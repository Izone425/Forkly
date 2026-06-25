<script setup>
import BrandLogo from './BrandLogo.vue'
import { useLoginAction } from '../composables/useLoginAction.js'

const { onLogin } = useLoginAction()
</script>

<template>
  <header class="site-header">
    <div class="container header-inner">
      <a href="#" class="brand-link" aria-label="Forkly home">
        <BrandLogo />
      </a>

      <!-- Secondary login entry; primary CTA lives in the hero.
           Hands off to the login service (see services/authGateway.js). -->
      <button type="button" class="btn btn-ghost header-login" @click="onLogin()">
        Login
      </button>
    </div>
  </header>
</template>

<style scoped>
.site-header {
  position: sticky;
  top: 0;
  z-index: 10;
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(10px);
  border-bottom: 1px solid var(--color-border);
}
.header-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding-top: 14px;
  padding-bottom: 14px;
}
.brand-link { display: inline-flex; }

@media (max-width: 720px) {
  .header-login { display: none; }
}
</style>
