<script setup>
// Full-page sign-in route (/login). The same form is reused inside the slide-in
// LoginDrawer; this page just wraps it in the centered AuthCard shell.
import { useRouter, useRoute } from 'vue-router'
import AuthCard from '../components/AuthCard.vue'
import LoginForm from '../components/LoginForm.vue'

const router = useRouter()
const route = useRoute()

function onSuccess(user) {
  // Route by role: admins -> admin area, crew -> kitchen, everyone else follows
  // ?redirect or goes home.
  if (user?.roles?.includes('admin')) {
    router.push('/admin')
    return
  }
  if (user?.roles?.includes('crew')) {
    router.push('/kitchen')
    return
  }
  const dest = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
  router.push(dest)
}
</script>

<template>
  <AuthCard title="Welcome back" subtitle="Sign in to your Forkly account.">
    <LoginForm @success="onSuccess" />
  </AuthCard>
</template>
