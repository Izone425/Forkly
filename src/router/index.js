import { createRouter, createWebHistory } from 'vue-router'
import LandingView from '../views/LandingView.vue'
import { useAuth } from '../stores/auth.js'

// Page-level routing for the Forkly frontend.
// Login/register/account are served in-app (no separate auth service). The Login
// button also opens a slide-in drawer; these routes are for direct navigation.
const routes = [
  { path: '/', name: 'landing', component: LandingView },
  {
    path: '/order',
    name: 'order',
    // Lazy-loaded so the landing page stays lean.
    component: () => import('../views/OrderView.vue'),
  },
  {
    path: '/login',
    name: 'login',
    component: () => import('../views/LoginView.vue'),
  },
  {
    path: '/register',
    name: 'register',
    component: () => import('../views/RegisterView.vue'),
  },
  {
    path: '/account',
    name: 'account',
    // Full-page "My Account" (in-app profile UI).
    component: () => import('../views/AccountView.vue'),
  },
  {
    // Standalone admin sales report (kept for backwards-compatible deep links).
    // The guarded entry point is /admin/reports below; the old /admin/report alias
    // was removed so there's no admin-looking URL that skips the role guard.
    path: '/report',
    name: 'report',
    component: () => import('../views/ReportView.vue'),
  },
  {
    // Admin area. The parent carries the role guard (meta.requiresAdmin); children
    // render inside AdminLayout's <RouterView>. Admins are sent here on login.
    path: '/admin',
    component: () => import('../views/AdminLayout.vue'),
    meta: { requiresAdmin: true },
    children: [
      { path: '', name: 'admin-dashboard', component: () => import('../views/AdminDashboard.vue') },
      { path: 'users', name: 'admin-users', component: () => import('../views/AdminUsers.vue') },
      { path: 'orders', name: 'admin-orders', component: () => import('../views/AdminOrders.vue') },
      { path: 'menu', name: 'admin-menu', component: () => import('../views/AdminMenu.vue') },
      // Reuse the existing sales report page unchanged as the reports child.
      { path: 'reports', name: 'admin-reports', component: () => import('../views/ReportView.vue') },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior(to, from, savedPosition) {
    // Scroll to an #anchored section (offset for the sticky header). Works even
    // when navigating from another page (e.g. Order -> landing #about).
    if (to.hash) {
      return { el: to.hash, top: 90, behavior: 'smooth' }
    }
    if (savedPosition) return savedPosition
    return { top: 0 }
  },
})

// Admin route guard. UX-only — the backend enforces the real gate ([Authorize(
// Roles="admin")] on every admin endpoint). We await whenReady() so a hard refresh
// of an /admin/* URL waits for session hydration instead of bouncing a real admin.
router.beforeEach(async (to) => {
  if (!to.matched.some((r) => r.meta.requiresAdmin)) return true

  const { state, isAdmin, whenReady } = useAuth()
  await whenReady()

  if (!state.user) return { name: 'login', query: { redirect: to.fullPath } }
  if (!isAdmin.value) return { path: '/' }
  return true
})

export default router
