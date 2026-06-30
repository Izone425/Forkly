<script setup>
// Admin landing: quick entry cards to each management area. Kept simple — live
// counts can be layered on later by calling adminApi.
import { RouterLink } from 'vue-router'
import { useAuth } from '../stores/auth.js'

const { state: auth } = useAuth()

const cards = [
  { to: { name: 'admin-users' }, title: 'Users', desc: 'View accounts, grant or revoke admin, disable users.', icon: '◉' },
  { to: { name: 'admin-orders' }, title: 'Orders', desc: 'Browse every order and update its status.', icon: '🧾' },
  { to: { name: 'admin-reports' }, title: 'Sales reports', desc: 'Monthly sales and per-item breakdown.', icon: '📈' },
  { to: { name: 'admin-menu' }, title: 'Menu', desc: 'Review menu items served to customers.', icon: '🍽' },
]
</script>

<template>
  <section class="dash">
    <header class="dash-head">
      <p class="dash-eyebrow">Forkly Admin</p>
      <h1 class="dash-title">Welcome back{{ auth.user ? `, ${auth.user.name}` : '' }}</h1>
      <p class="dash-sub">Manage users, orders, reports and the menu from one place.</p>
    </header>

    <div class="dash-grid">
      <RouterLink v-for="card in cards" :key="card.title" :to="card.to" class="dash-card">
        <span class="dash-card-icon" aria-hidden="true">{{ card.icon }}</span>
        <span class="dash-card-title">{{ card.title }}</span>
        <span class="dash-card-desc">{{ card.desc }}</span>
      </RouterLink>
    </div>
  </section>
</template>

<style scoped>
.dash { max-width: 980px; }
.dash-head { margin-bottom: 26px; }
.dash-eyebrow {
  margin: 0 0 6px;
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 1px;
  text-transform: uppercase;
  color: var(--color-primary);
}
.dash-title { margin: 0 0 8px; font-size: 1.7rem; color: var(--color-ink); }
.dash-sub { margin: 0; color: var(--color-muted); }

.dash-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 16px;
}
.dash-card {
  display: flex;
  flex-direction: column;
  gap: 8px;
  padding: 20px;
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: 14px;
  box-shadow: var(--shadow-sm);
  text-decoration: none;
  transition: border-color 0.15s ease, transform 0.15s ease, box-shadow 0.15s ease;
}
.dash-card:hover {
  border-color: var(--color-primary);
  transform: translateY(-2px);
  box-shadow: var(--shadow-lg);
}
.dash-card-icon { font-size: 1.5rem; }
.dash-card-title { font-weight: 800; color: var(--color-ink); }
.dash-card-desc { font-size: 0.9rem; color: var(--color-muted); line-height: 1.45; }
</style>
