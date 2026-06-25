<script setup>
import { RouterLink } from 'vue-router'
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
      <!-- Section nav (left). Anchors smooth-scroll; Order opens the order page. -->
      <nav class="header-nav" aria-label="Sections">
        <a href="#about" class="nav-link">About Us</a>
        <a href="#menu" class="nav-link">Our Menu</a>
        <RouterLink to="/order" class="nav-link">Order</RouterLink>
        <a href="#contact" class="nav-link">Contact Us</a>
      </nav>

      <!-- Logo (centered). The transparent logo already includes the wordmark. -->
      <a href="#" class="brand-link" aria-label="Forkly home">
        <BrandLogo size="lg" :show-text="false" />
      </a>

      <!-- Login / profile (right). -->
      <div class="header-end">
        <button
          v-if="!isLoggedIn"
          type="button"
          class="btn btn-primary header-login"
          @click="onLogin()"
        >
          Login
        </button>

        <div v-else class="profile">
          <span class="profile-avatar" aria-hidden="true">{{ initials }}</span>
          <span class="profile-name">{{ auth.user.name }}</span>
          <button type="button" class="profile-logout" @click="logout">Logout</button>
        </div>
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
  display: grid;
  grid-template-columns: 1fr auto 1fr; /* nav | logo | login */
  align-items: center;
  gap: 16px;
  padding-top: 18px;
  padding-bottom: 18px;
}

.header-nav {
  display: flex;
  align-items: center;
  gap: 8px;
  justify-self: start;
}
.nav-link {
  font-weight: 600;
  font-size: 0.98rem;
  color: var(--color-body);
  padding: 9px 14px;
  border-radius: 10px;
  transition: color 0.15s ease, background 0.15s ease;
}
.nav-link:hover { color: var(--color-primary); background: var(--color-primary-soft); }

.brand-link { justify-self: center; display: inline-flex; }

.header-end { justify-self: end; }

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

/* On narrow screens drop the text nav; logo stays centered, login on the right. */
@media (max-width: 860px) {
  .header-nav { display: none; }
}
@media (max-width: 720px) {
  .header-inner { padding-top: 14px; padding-bottom: 14px; }
  .header-login { padding: 11px 26px; font-size: 1rem; }
  .profile-name { display: none; }
}
</style>
