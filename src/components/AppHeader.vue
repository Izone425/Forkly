<script setup>
import BrandLogo from './BrandLogo.vue'
import { useLoginAction } from '../composables/useLoginAction.js'
import { useAuth } from '../stores/auth.js'

// Single login entry point for the whole app. Hands off to the user-management
// service (izone-user-management-FE). When it returns a signed-in user, the
// header shows the profile instead (see auth store).
const { onLogin } = useLoginAction()
const { state: auth, isLoggedIn, initials, logout } = useAuth()
</script>

<template>
  <header class="site-header">
    <div class="container header-inner">
      <a href="#" class="brand-link" aria-label="Forkly home">
        <BrandLogo size="lg" />
      </a>

      <!-- Logged out: one prominent Login button. -->
      <button
        v-if="!isLoggedIn"
        type="button"
        class="btn btn-primary header-login"
        @click="onLogin()"
      >
        Login
      </button>

      <!-- Logged in: user profile (populated by the user-management flow). -->
      <div v-else class="profile">
        <span class="profile-avatar" aria-hidden="true">{{ initials }}</span>
        <span class="profile-name">{{ auth.user.name }}</span>
        <button type="button" class="profile-logout" @click="logout">Logout</button>
      </div>
    </div>
  </header>
</template>

<style scoped>
.site-header {
  position: sticky;
  top: 0;
  z-index: 10;
  background: rgba(255, 255, 255, 0.9);
  backdrop-filter: blur(10px);
  border-bottom: 1px solid var(--color-border);
}
.header-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  padding-top: 22px;
  padding-bottom: 22px;
}
.brand-link { display: inline-flex; }

/* Bigger, prominent login button. */
.header-login {
  font-size: 1.05rem;
  padding: 13px 36px;
}

.profile {
  display: inline-flex;
  align-items: center;
  gap: 12px;
}
.profile-avatar {
  width: 42px;
  height: 42px;
  display: grid;
  place-items: center;
  border-radius: 50%;
  background: var(--color-primary-soft);
  color: var(--color-primary);
  font-weight: 800;
  font-size: 0.95rem;
}
.profile-name { font-weight: 700; color: var(--color-ink); }
.profile-logout {
  border: 1px solid var(--color-border);
  background: #fff;
  color: var(--color-body);
  font-family: inherit;
  font-size: 0.9rem;
  font-weight: 600;
  padding: 8px 16px;
  border-radius: 10px;
  cursor: pointer;
}
.profile-logout:hover { border-color: var(--color-primary); color: var(--color-primary); }

@media (max-width: 720px) {
  .header-inner { padding-top: 16px; padding-bottom: 16px; }
  .header-login { padding: 11px 26px; font-size: 1rem; }
  .profile-name { display: none; }
}
</style>
