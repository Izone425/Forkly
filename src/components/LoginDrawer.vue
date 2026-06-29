<script setup>
import { ref, computed, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { config } from '../config.js'

// IZZUWAN user-management integration seam (login).
// The landing dispatches `forkly:open-login-drawer` (see services/authBridge.js)
// when Login is clicked. This drawer answers that: it embeds the Forkly-Auth app
// (config.loginUrl, e.g. http://localhost:5174/login) in an iframe with ?embed=1,
// and relays the auth app's postMessage success back as `forkly:login-success`
// (which authBridge.initAuthBridge() listens for to set the signed-in profile).
// The signed-in "My Account" view is a full page (/account), not this drawer.

const isOpen = ref(false)
const closeBtn = ref(null)

// Origin of the auth app — used to validate incoming postMessages.
const authOrigin = computed(() => {
  try {
    return new URL(config.loginUrl).origin
  } catch {
    return ''
  }
})

// iframe URL, rebuilt each open. embed=1 puts Forkly-Auth into postMessage mode.
const iframeSrc = computed(() => {
  if (!isOpen.value || !config.loginUrl) return 'about:blank'
  const url = new URL(config.loginUrl)
  url.searchParams.set('from', 'forkly-landing')
  url.searchParams.set('return_to', window.location.origin)
  url.searchParams.set('embed', '1')
  return url.toString()
})

function open() {
  isOpen.value = true
  nextTick(() => closeBtn.value?.focus())
}
function close() {
  isOpen.value = false
}

function onKeydown(event) {
  if (event.key === 'Escape' && isOpen.value) close()
}

function onMessage(event) {
  // Only trust messages from the auth app's origin.
  if (authOrigin.value && event.origin !== authOrigin.value) return
  const data = event.data || {}
  if (data.type === 'forkly-auth:success') {
    const user = data.user || {}
    window.dispatchEvent(
      new CustomEvent('forkly:login-success', {
        detail: {
          // Map the auth UserDto onto the shape the landing store expects (name).
          user: { ...user, name: user.fullName || user.name || user.email },
          token: data.token,
        },
      }),
    )
    close()
  }
}

onMounted(() => {
  window.addEventListener('forkly:open-login-drawer', open)
  window.addEventListener('keydown', onKeydown)
  window.addEventListener('message', onMessage)
})
onBeforeUnmount(() => {
  window.removeEventListener('forkly:open-login-drawer', open)
  window.removeEventListener('keydown', onKeydown)
  window.removeEventListener('message', onMessage)
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
        aria-label="Sign in to Forkly"
      >
        <header class="drawer-head">
          <span class="drawer-title">Sign in</span>
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

        <iframe
          v-if="isOpen"
          class="drawer-frame"
          :src="iframeSrc"
          title="Forkly sign in"
          sandbox="allow-scripts allow-forms allow-same-origin"
        />
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

.drawer-frame {
  flex: 1;
  width: 100%;
  border: none;
}

@media (max-width: 720px) {
  .drawer-panel { width: 100vw; }
}
</style>
