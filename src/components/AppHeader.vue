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
      <!-- Section menu (left): a hamburger button that reveals the options on
           hover. Anchors smooth-scroll; Order opens the order page. -->
      <nav class="header-menu" aria-label="Sections">
        <button type="button" class="menu-btn" aria-label="Menu" aria-haspopup="true">
          <span class="menu-bar"></span>
          <span class="menu-bar"></span>
          <span class="menu-bar"></span>
        </button>

        <div class="menu-dropdown">
          <div class="menu-card">
            <a href="#about" class="menu-item">About Us</a>
            <a href="#menu" class="menu-item">Our Menu</a>
            <RouterLink to="/order" class="menu-item">Order</RouterLink>
            <a href="#contact" class="menu-item">Contact Us</a>
          </div>
        </div>
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

.header-menu { position: relative; justify-self: start; }

/* Hamburger button (3 lines). */
.menu-btn {
  display: inline-flex;
  flex-direction: column;
  justify-content: center;
  gap: 5px;
  width: 50px;
  height: 46px;
  padding: 0 13px;
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: 12px;
  cursor: pointer;
  box-shadow: var(--shadow-sm);
  transition: border-color 0.15s ease;
}
.menu-bar {
  height: 2.5px;
  border-radius: 2px;
  background: var(--color-ink);
  transition: background 0.15s ease;
}
.header-menu:hover .menu-btn,
.header-menu:focus-within .menu-btn { border-color: var(--color-primary); }
.header-menu:hover .menu-bar,
.header-menu:focus-within .menu-bar { background: var(--color-primary); }

/* Dropdown: hidden until the menu is hovered/focused. The outer wrapper keeps a
   transparent bridge (padding-top) so moving the pointer onto it stays hovered. */
.menu-dropdown {
  position: absolute;
  top: 100%;
  left: 0;
  padding-top: 8px;
  min-width: 200px;
  opacity: 0;
  visibility: hidden;
  transform: translateY(-6px);
  transition: opacity 0.15s ease, transform 0.15s ease, visibility 0.15s ease;
  z-index: 20;
}
.header-menu:hover .menu-dropdown,
.header-menu:focus-within .menu-dropdown {
  opacity: 1;
  visibility: visible;
  transform: translateY(0);
}
.menu-card {
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: 12px;
  box-shadow: var(--shadow-lg);
  padding: 6px;
  display: flex;
  flex-direction: column;
}
.menu-item {
  font-weight: 600;
  font-size: 0.96rem;
  color: var(--color-body);
  padding: 10px 14px;
  border-radius: 8px;
  transition: color 0.15s ease, background 0.15s ease;
}
.menu-item:hover { color: var(--color-primary); background: var(--color-primary-soft); }

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

@media (max-width: 720px) {
  .header-inner { padding-top: 14px; padding-bottom: 14px; }
  .header-login { padding: 11px 26px; font-size: 1rem; }
  .profile-name { display: none; }
}
</style>
