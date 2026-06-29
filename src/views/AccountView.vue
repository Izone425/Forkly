<script setup>
import { computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import AppHeader from '../components/AppHeader.vue'
import FloatingCart from '../components/FloatingCart.vue'
import { config } from '../config.js'
import { useAuth } from '../stores/auth.js'

// Full-page "My Account": embeds Forkly-Auth's /profile page (the account UI owned by
// the user-management module). No ?embed=1 so it renders the full layout — but since
// it runs inside an iframe, Forkly-Auth still posts profile-updated / logout to us.

const router = useRouter()
const { setUser, logout } = useAuth()

const authOrigin = computed(() => {
  try {
    return new URL(config.loginUrl).origin
  } catch {
    return ''
  }
})

const src = computed(() => {
  if (!config.loginUrl) return 'about:blank'
  const url = new URL(config.loginUrl)
  url.pathname = '/profile'
  url.searchParams.set('from', 'forkly-landing')
  url.searchParams.set('return_to', window.location.origin)
  return url.toString()
})

function onMessage(event) {
  if (authOrigin.value && event.origin !== authOrigin.value) return
  const data = event.data || {}
  if (data.type === 'forkly-auth:profile-updated') {
    const u = data.user || {}
    setUser({ ...u, name: u.fullName || u.name || u.email })
  } else if (data.type === 'forkly-auth:logout') {
    logout()
    router.push('/')
  }
}

onMounted(() => window.addEventListener('message', onMessage))
onBeforeUnmount(() => window.removeEventListener('message', onMessage))
</script>

<template>
  <div class="account-page">
    <AppHeader />
    <iframe class="account-frame" :src="src" title="My account" />
    <!-- Quick "go to your order" circle, same as on the landing. -->
    <FloatingCart />
  </div>
</template>

<style scoped>
.account-page {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}
.account-frame {
  flex: 1;
  width: 100%;
  border: none;
  display: block;
}
</style>
