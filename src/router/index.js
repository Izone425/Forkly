import { createRouter, createWebHistory } from 'vue-router'
import LandingView from '../views/LandingView.vue'

// Page-level routing for the Forkly frontend.
// `/order` is the post-login ordering experience (login itself is handled by a
// separate service — see services/authGateway.js). No auth guards here yet.
const routes = [
  { path: '/', name: 'landing', component: LandingView },
  {
    path: '/order',
    name: 'order',
    // Lazy-loaded so the landing page stays lean.
    component: () => import('../views/OrderView.vue'),
  },
]

export default createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior() {
    return { top: 0 }
  },
})
