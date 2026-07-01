<script setup>
import { RouterLink, useRouter, useRoute } from 'vue-router'
import BrandLogo from './BrandLogo.vue'
import ViewSwitch from './ViewSwitch.vue'
import { openLoginDrawer } from '../services/authBridge.js'
import { useAuth } from '../stores/auth.js'
import { useCart } from '../stores/cart.js'

// Shared header for the landing and order pages.
//  - showLogin: show the Login button / profile (landing).
//  - showCart:  show the cart indicator (order page).
defineProps({
  showLogin: { type: Boolean, default: true },
  showCart: { type: Boolean, default: false },
})

const router = useRouter()
const route = useRoute()
const { state: auth, isLoggedIn, initials, logout } = useAuth()
const { count } = useCart()

// Sign out and return to the landing (mirrors the profile page's Log out).
function onLogout() {
  logout()
  if (route.path !== '/') router.push('/')
}
</script>

<template>
  <header class="site-header">
    <div class="container header-inner">
      <!-- Left: hamburger menu + logo, side by side. -->
      <div class="header-left">
        <nav class="header-menu" aria-label="Sections">
          <button type="button" class="menu-btn" aria-label="Menu" aria-haspopup="true">
            <span class="menu-bar"></span>
            <span class="menu-bar"></span>
            <span class="menu-bar"></span>
          </button>

          <div class="menu-dropdown">
            <div class="menu-card">
              <RouterLink to="/#about" class="menu-item">About Us</RouterLink>
              <RouterLink to="/#menu" class="menu-item">Our Menu</RouterLink>
              <RouterLink to="/order" class="menu-item">Order</RouterLink>
              <RouterLink to="/#contact" class="menu-item">Contact Us</RouterLink>
            </div>
          </div>
        </nav>

        <RouterLink to="/" class="brand-link" aria-label="Forkly home">
          <BrandLogo size="lg" :show-text="false" />
        </RouterLink>
      </div>

      <!-- Right: cart (order page) and/or Login / profile (landing). -->
      <div class="header-end">
        <div
          v-if="showCart"
          class="header-cart"
          :class="{ active: count > 0 }"
          aria-live="polite"
        >
          <span class="header-cart-icon" aria-hidden="true">🛒</span>
          <span>{{ count }} {{ count === 1 ? 'item' : 'items' }}</span>
        </div>

        <template v-if="showLogin">
          <button
            v-if="!isLoggedIn"
            type="button"
            class="btn btn-primary header-login"
            @click="openLoginDrawer()"
          >
            Login
          </button>

          <div v-else class="profile">
            <ViewSwitch />
            <RouterLink to="/account" class="profile-trigger" title="My account">
              <span class="profile-avatar" aria-hidden="true">{{ initials }}</span>
              <span class="profile-name">{{ auth.user.name }}</span>
            </RouterLink>
            <button type="button" class="profile-logout" @click="onLogout">Logout</button>
          </div>
        </template>
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
  padding-top: 18px;
  padding-bottom: 18px;
}

.header-left { display: flex; align-items: center; gap: 18px; }

.header-menu { position: relative; }

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

/* Dropdown: hidden until the menu is hovered/focused. The wrapper keeps a
   transparent bridge (padding-top) so moving onto it stays hovered. */
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

.brand-link { display: inline-flex; }

.header-end { display: flex; align-items: center; gap: 14px; }

/* Cart indicator (order page). */
.header-cart {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 9px 18px;
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--color-body);
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: 999px;
  box-shadow: var(--shadow-sm);
}
.header-cart.active { color: var(--color-primary); border-color: #cdd9f5; }
.header-cart-icon { font-size: 1.1rem; }

/* Bigger, prominent login button. */
.header-login {
  font-size: 1.05rem;
  padding: 13px 36px;
}

.profile { display: inline-flex; align-items: center; gap: 12px; }
/* Avatar + name act as one button that opens the "My Account" drawer. */
.profile-trigger {
  display: inline-flex;
  align-items: center;
  gap: 12px;
  border: 1px solid transparent;
  background: transparent;
  font: inherit;
  text-decoration: none;
  padding: 4px 10px 4px 4px;
  border-radius: 999px;
  cursor: pointer;
  transition: background 0.15s ease, border-color 0.15s ease;
}
.profile-trigger:hover { background: var(--color-primary-soft); border-color: #cdd9f5; }
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
  .header-left { gap: 12px; }
  .header-login { padding: 11px 26px; font-size: 1rem; }
  .profile-name { display: none; }
}
</style>
