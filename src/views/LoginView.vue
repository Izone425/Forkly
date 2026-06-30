<script setup>
// Full-page sign-in route (/login). The same form is reused inside the slide-in
// LoginDrawer; this page just wraps it in the centered AuthCard shell.
import { useRouter, useRoute } from 'vue-router'
import AuthCard from '../components/AuthCard.vue'
import LoginForm from '../components/LoginForm.vue'

const router = useRouter()
const route = useRoute()

function onSuccess(user) {
  // Admins land in the admin area; everyone else follows ?redirect or goes home.
  if (user?.roles?.includes('admin')) {
    router.push('/admin')
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
