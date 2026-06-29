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

// Load state of the embedded auth app, so a missing/again-not-running Forkly-Auth
// shows a helpful message instead of a blank white panel.
//   'loading' → waiting for the iframe to load
//   'ok'      → the auth app loaded
//   'error'   → no loginUrl, or the auth app didn't load in time (likely not running)
const status = ref('loading')
const frameKey = ref(0) // bump to force the iframe to re-request (Retry)
let loadTimer = null
const LOAD_TIMEOUT = 6000 // ms — a refused connection never fires the iframe's load event

function clearTimer() {
  if (loadTimer) {
    clearTimeout(loadTimer)
    loadTimer = null
  }
}

// Start (or restart) watching for the auth app to load. If it doesn't load
// within the timeout, assume Forkly-Auth isn't running on its port.
function watchLoad() {
  clearTimer()
  status.value = 'loading'
  loadTimer = setTimeout(() => {
    if (status.value === 'loading') status.value = 'error'
  }, LOAD_TIMEOUT)
}

// Fires when the iframe finishes loading the auth app. A connection refused
// (auth app down) does NOT fire this in Chromium, so the timeout above catches it.
function onFrameLoad() {
  if (config.loginUrl) {
    status.value = 'ok'
    clearTimer()
  }
}

function retry() {
  frameKey.value++ // remount the iframe so it re-requests the auth app
  watchLoad()
}

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
  // No login URL configured at all → tell the user straight away.
  if (!config.loginUrl) {
    status.value = 'error'
  } else {
    watchLoad()
  }
  nextTick(() => closeBtn.value?.focus())
}
function close() {
  isOpen.value = false
  clearTimer()
}

function onKeydown(event) {
  if (event.key === 'Escape' && isOpen.value) close()
}

function onMessage(event) {
  // Only trust messages from the auth app's origin.
  if (authOrigin.value && event.origin !== authOrigin.value) return
  const data = event.data || {}
  // A message from the configured auth origin means it loaded fine.
  if (config.loginUrl) {
    status.value = 'ok'
    clearTimer()
  }
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
  clearTimer()
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

        <div class="drawer-body">
          <iframe
            v-if="isOpen"
            :key="frameKey"
            class="drawer-frame"
            :src="iframeSrc"
            title="Forkly sign in"
            sandbox="allow-scripts allow-forms allow-same-origin"
            @load="onFrameLoad"
          />

          <!-- While the embedded auth app is loading. -->
          <div v-if="isOpen && status === 'loading'" class="drawer-overlay">
            <span class="drawer-spinner" aria-hidden="true" />
            <p class="overlay-text">Connecting to the login service…</p>
          </div>

          <!-- Auth app not reachable (not running, or no URL configured). -->
          <div
            v-if="isOpen && status === 'error'"
            class="drawer-overlay drawer-overlay--error"
            role="alert"
          >
            <p class="overlay-title">Login service isn't running</p>
            <p class="overlay-text">
              Start <strong>Forkly-Auth</strong> on
              <code>http://localhost:5174</code>.<br />
              Run <code>./start-all.ps1</code> from the project root, or use the
              <strong>All Forkly</strong> profile in Visual Studio.
            </p>
            <button type="button" class="overlay-retry" @click="retry">Retry</button>
          </div>
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
  position: relative;
}
.drawer-frame {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  border: none;
}

/* Overlay sits on top of the iframe (covers a blank/refused-connection frame). */
.drawer-overlay {
  position: absolute;
  inset: 0;
  z-index: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 14px;
  padding: 28px;
  background: #fff;
  text-align: center;
}
.overlay-title {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 800;
  color: var(--color-ink, #0f172a);
}
.overlay-text {
  margin: 0;
  font-size: 0.9rem;
  line-height: 1.6;
  color: var(--color-body, #475569);
}
.drawer-overlay code {
  font-size: 0.82rem;
  background: var(--color-surface, #f1f5f9);
  border: 1px solid var(--color-border, #e5e7eb);
  border-radius: 6px;
  padding: 1px 6px;
}
.overlay-retry {
  margin-top: 4px;
  border: none;
  background: var(--color-primary, #2563eb);
  color: #fff;
  font-family: inherit;
  font-size: 0.92rem;
  font-weight: 700;
  padding: 9px 22px;
  border-radius: 8px;
  cursor: pointer;
}
.overlay-retry:hover { filter: brightness(1.05); }

.drawer-spinner {
  width: 26px;
  height: 26px;
  border: 3px solid rgba(37, 99, 235, 0.25);
  border-top-color: var(--color-primary, #2563eb);
  border-radius: 50%;
  animation: drawer-spin 0.8s linear infinite;
}
@keyframes drawer-spin { to { transform: rotate(360deg); } }

@media (max-width: 720px) {
  .drawer-panel { width: 100vw; }
}
</style>
