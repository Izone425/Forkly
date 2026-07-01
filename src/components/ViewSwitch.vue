<script setup>
// Admin-only view switch: a segmented pill that flips between the end-user
// (customer) shell at "/" and the admin shell at "/admin". Self-gates on
// isAdmin, so it renders nothing for signed-out users and normal clients.
//
// "Which view" is derived purely from the current route (no store state) — the
// admin experience simply *is* the /admin/* route tree. Clicking the active side
// is a harmless no-op (same route).
import { computed } from 'vue'
import { RouterLink, useRoute } from 'vue-router'
import { useAuth } from '../stores/auth.js'

const route = useRoute()
const { isAdmin } = useAuth()

const isAdminView = computed(() => route.path.startsWith('/admin'))
</script>

<template>
  <div v-if="isAdmin" class="view-switch" role="group" aria-label="Switch view">
    <RouterLink
      to="/"
      class="view-switch-seg"
      :class="{ active: !isAdminView }"
      :aria-current="!isAdminView ? 'page' : undefined"
    >
      User
    </RouterLink>
    <RouterLink
      to="/admin"
      class="view-switch-seg"
      :class="{ active: isAdminView }"
      :aria-current="isAdminView ? 'page' : undefined"
    >
      Admin
    </RouterLink>
  </div>
</template>

<style scoped>
.view-switch {
  display: inline-flex;
  align-items: center;
  gap: 2px;
  padding: 3px;
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: 999px;
  box-shadow: var(--shadow-sm);
}
.view-switch-seg {
  font-weight: 700;
  font-size: 0.88rem;
  line-height: 1;
  color: var(--color-body);
  padding: 8px 16px;
  border-radius: 999px;
  text-decoration: none;
  transition: color 0.15s ease, background 0.15s ease;
}
.view-switch-seg:hover { color: var(--color-primary); background: var(--color-primary-soft); }
.view-switch-seg.active {
  color: #fff;
  background: var(--color-primary);
}
.view-switch-seg.active:hover { color: #fff; }

@media (max-width: 720px) {
  .view-switch-seg { padding: 7px 13px; font-size: 0.82rem; }
}
</style>
