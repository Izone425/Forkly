<script setup>
// Admin area shell: persistent side nav + topbar, with the active admin child
// rendered in the <RouterView>. The route guard (router/index.js) already ensures
// only admins reach here; we re-check isAdmin defensively for a clean fallback.
import { onMounted } from 'vue'
import { RouterLink, RouterView, useRouter } from 'vue-router'
import BrandLogo from '../components/BrandLogo.vue'
import { useAuth } from '../stores/auth.js'
import { useToast } from '../stores/toast.js'

const router = useRouter()
const { state: auth, isAdmin, logout } = useAuth()
const { state: toasts, dismiss } = useToast()

const links = [
  { to: { name: 'admin-dashboard' }, label: 'Dashboard', icon: '▦' },
  { to: { name: 'admin-users' }, label: 'Users', icon: '◉' },
  { to: { name: 'admin-orders' }, label: 'Orders', icon: '🧾' },
  { to: { name: 'admin-reports' }, label: 'Sales reports', icon: '📈' },
  { to: { name: 'admin-menu' }, label: 'Menu', icon: '🍽' },
]

function onLogout() {
  logout()
  router.push('/')
}

onMounted(() => {
  // Defensive: the guard handles this, but if state changes underfoot, bounce out.
  if (!isAdmin.value) router.replace('/')
})
</script>

<template>
  <div class="admin-shell">
    <aside class="admin-sidebar">
      <RouterLink to="/admin" class="admin-brand">
        <BrandLogo size="md" :show-text="false" />
        <span class="admin-brand-text">Admin</span>
      </RouterLink>

      <nav class="admin-nav" aria-label="Admin sections">
        <RouterLink
          v-for="link in links"
          :key="link.label"
          :to="link.to"
          class="admin-nav-link"
        >
          <span class="admin-nav-icon" aria-hidden="true">{{ link.icon }}</span>
          {{ link.label }}
        </RouterLink>
      </nav>

      <div class="admin-sidebar-foot">
        <RouterLink to="/" class="admin-back">← Back to site</RouterLink>
      </div>
    </aside>

    <div class="admin-main">
      <header class="admin-topbar">
        <span class="admin-topbar-title">Forkly Admin</span>
        <div class="admin-topbar-end">
          <span v-if="auth.user" class="admin-user">{{ auth.user.name }}</span>
          <button type="button" class="admin-logout" @click="onLogout">Logout</button>
        </div>
      </header>

      <main class="admin-content">
        <RouterView />
      </main>
    </div>

    <!-- Lightweight toast host for admin actions (no global host exists). -->
    <div class="admin-toasts" aria-live="polite">
      <button
        v-for="t in toasts.items"
        :key="t.id"
        type="button"
        class="admin-toast"
        @click="dismiss(t.id)"
      >
        {{ t.message }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.admin-shell {
  display: grid;
  grid-template-columns: 248px 1fr;
  min-height: 100vh;
  background: var(--color-bg, #f6f7fb);
}

.admin-sidebar {
  display: flex;
  flex-direction: column;
  gap: 22px;
  padding: 22px 18px;
  background: #fff;
  border-right: 1px solid var(--color-border);
  position: sticky;
  top: 0;
  height: 100vh;
}
.admin-brand {
  display: inline-flex;
  align-items: center;
  gap: 10px;
  text-decoration: none;
}
.admin-brand-text {
  font-weight: 800;
  letter-spacing: 0.5px;
  text-transform: uppercase;
  font-size: 0.78rem;
  color: var(--color-primary);
  background: var(--color-primary-soft);
  padding: 3px 8px;
  border-radius: 6px;
}

.admin-nav { display: flex; flex-direction: column; gap: 4px; }
.admin-nav-link {
  display: flex;
  align-items: center;
  gap: 11px;
  padding: 10px 13px;
  border-radius: 10px;
  font-weight: 600;
  font-size: 0.95rem;
  color: var(--color-body);
  text-decoration: none;
  transition: color 0.15s ease, background 0.15s ease;
}
.admin-nav-link:hover { color: var(--color-primary); background: var(--color-primary-soft); }
.admin-nav-link.router-link-exact-active {
  color: var(--color-primary);
  background: var(--color-primary-soft);
}
.admin-nav-icon { font-size: 1rem; width: 18px; text-align: center; }

.admin-sidebar-foot { margin-top: auto; }
.admin-back {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--color-muted);
  text-decoration: none;
}
.admin-back:hover { color: var(--color-primary); }

.admin-main { display: flex; flex-direction: column; min-width: 0; }
.admin-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 28px;
  background: rgba(255, 255, 255, 0.9);
  backdrop-filter: blur(10px);
  border-bottom: 1px solid var(--color-border);
  position: sticky;
  top: 0;
  z-index: 5;
}
.admin-topbar-title { font-weight: 800; color: var(--color-ink); }
.admin-topbar-end { display: inline-flex; align-items: center; gap: 14px; }
.admin-user { font-weight: 600; color: var(--color-body); }
.admin-logout {
  border: 1px solid var(--color-border);
  background: #fff;
  color: var(--color-body);
  font-family: inherit;
  font-size: 0.88rem;
  font-weight: 600;
  padding: 8px 16px;
  border-radius: 10px;
  cursor: pointer;
}
.admin-logout:hover { border-color: var(--color-primary); color: var(--color-primary); }

.admin-content { padding: 28px; min-width: 0; }

.admin-toasts {
  position: fixed;
  bottom: 22px;
  right: 22px;
  display: flex;
  flex-direction: column;
  gap: 10px;
  z-index: 1000;
}
.admin-toast {
  font: inherit;
  font-size: 0.9rem;
  font-weight: 600;
  color: #fff;
  background: var(--color-ink, #0f172a);
  border: none;
  padding: 12px 18px;
  border-radius: 10px;
  box-shadow: var(--shadow-lg);
  cursor: pointer;
  text-align: left;
}

@media (max-width: 860px) {
  .admin-shell { grid-template-columns: 1fr; }
  .admin-sidebar {
    position: static;
    height: auto;
    flex-direction: row;
    flex-wrap: wrap;
    align-items: center;
    gap: 12px;
  }
  .admin-nav { flex-direction: row; flex-wrap: wrap; }
  .admin-sidebar-foot { margin: 0; }
  .admin-content { padding: 18px; }
}
</style>
