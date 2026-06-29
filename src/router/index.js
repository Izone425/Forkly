import { createRouter, createWebHistory } from 'vue-router'
import LandingView from '../views/LandingView.vue'

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
]

export default createRouter({
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
