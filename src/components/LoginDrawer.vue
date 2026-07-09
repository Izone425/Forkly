<script setup>
// Slide-in sign-in drawer. The landing dispatches `forkly:open-login-drawer`
// (see services/authBridge.js) when Login is clicked; this drawer answers it by
// rendering the in-app LoginForm. On success the auth store is already updated,
// so CartSummary's watch(isLoggedIn) resumes a pending checkout — we just close.
import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { useRouter } from 'vue-router'
import LoginForm from './LoginForm.vue'
import RegisterForm from './RegisterForm.vue'

const router = useRouter()
const isOpen = ref(false)
const mode = ref('login') // 'login' | 'register' — swapped in-place, no navigation
const closeBtn = ref(null)

function open() {
  mode.value = 'login' // always open on sign-in
  isOpen.value = true
  nextTick(() => closeBtn.value?.focus())
}
function close() {
  isOpen.value = false
}

function onSuccess(user) {
  // Profile is set by the form via the auth store. Close the drawer, and send
  // admins to the admin area and crew to the kitchen (clients stay where they
  // were, e.g. mid-checkout).
  close()
  if (user?.roles?.includes('admin')) router.push('/admin')
  else if (user?.roles?.includes('crew')) router.push('/kitchen')
}

function onKeydown(event) {
  if (event.key === 'Escape' && isOpen.value) close()
}

onMounted(() => {
  window.addEventListener('forkly:open-login-drawer', open)
  window.addEventListener('keydown', onKeydown)
})
onBeforeUnmount(() => {
  window.removeEventListener('forkly:open-login-drawer', open)
  window.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <Teleport to="body">
    <div class="drawer-root" :class="{ open: isOpen }" aria-hidden="false">
      <div class="drawer-backdrop" @click="close" />

      <aside
        class="drawer-panel"
        role="dialog"
        aria-modal="true"
        :aria-label="mode === 'register' ? 'Create your Forkly account' : 'Sign in to Forkly'"
      >
        <header class="drawer-head">
          <span class="drawer-title">{{ mode === 'register' ? 'Create your account' : 'Sign in' }}</span>
          <button
            ref="closeBtn"
            type="button"
            class="drawer-close"
            aria-label="Close"
            @click="close"
          >
            &times;
          </button>
        </header>

        <div class="drawer-body">
          <p class="drawer-intro">
            {{ mode === 'register'
              ? 'Create a Forkly account to continue.'
              : 'Sign in to your Forkly account to continue.' }}
          </p>
          <!-- The switch is hidden on the forms; the drawer swaps them in-place
               below so a pending checkout is never dropped by navigation. -->
          <LoginForm v-if="isOpen && mode === 'login'" :show-switch="false" @success="onSuccess" />
          <RegisterForm v-else-if="isOpen && mode === 'register'" :show-switch="false" @success="onSuccess" />

          <p v-if="mode === 'login'" class="drawer-foot">
            New to Forkly?
            <button type="button" class="drawer-link" @click="mode = 'register'">Create an account</button>
          </p>
          <p v-else class="drawer-foot">
            Already have an account?
            <button type="button" class="drawer-link" @click="mode = 'login'">Sign in</button>
          </p>
        </div>
      </aside>
    </div>
  </Teleport>
</template>

<style scoped>
.drawer-root {
  position: fixed;
  inset: 0;
  z-index: 1000;
  pointer-events: none;
  visibility: hidden;
}
.drawer-root.open { pointer-events: auto; visibility: visible; }

.drawer-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(15, 23, 42, 0.45);
  opacity: 0;
  transition: opacity 0.25s ease;
}
.drawer-root.open .drawer-backdrop { opacity: 1; }

.drawer-panel {
  position: absolute;
  top: 0;
  right: 0;
  height: 100%;
  width: min(440px, 100vw);
  background: #fff;
  box-shadow: -8px 0 30px rgba(15, 23, 42, 0.18);
  display: flex;
  flex-direction: column;
  transform: translateX(100%);
  transition: transform 0.28s cubic-bezier(0.4, 0, 0.2, 1);
}
.drawer-root.open .drawer-panel { transform: translateX(0); }

.drawer-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid var(--color-border, #e5e7eb);
}
.drawer-title { font-weight: 700; color: var(--color-ink, #0f172a); }
.drawer-close {
  border: none;
  background: transparent;
  font-size: 1.6rem;
  line-height: 1;
  color: var(--color-muted, #64748b);
  cursor: pointer;
  padding: 0 4px;
  border-radius: 8px;
}
.drawer-close:hover { color: var(--color-ink, #0f172a); }

.drawer-body {
  flex: 1;
  overflow-y: auto;
  padding: 24px 22px;
}
.drawer-intro {
  margin: 0 0 20px;
  color: var(--color-muted, #64748b);
  font-size: 0.95rem;
}
.drawer-foot {
  margin: 20px 0 0;
  text-align: center;
  font-size: 0.9rem;
  color: var(--color-body, #475569);
}
.drawer-link {
  border: none;
  background: none;
  padding: 0;
  font: inherit;
  color: var(--color-primary, #2563eb);
  font-weight: 600;
  cursor: pointer;
  text-decoration: underline;
}
.drawer-link:hover { text-decoration: none; }

@media (max-width: 720px) {
  .drawer-panel { width: 100vw; }
}
</style>
