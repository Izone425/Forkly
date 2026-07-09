<script setup>
// Full-page account creation route (/register). The same form is reused inside the
// slide-in LoginDrawer; this page just wraps it in the centered AuthCard shell.
import { useRouter, useRoute } from 'vue-router'
import AuthCard from '../components/AuthCard.vue'
import RegisterForm from '../components/RegisterForm.vue'

const router = useRouter()
const route = useRoute()

function onSuccess() {
  // authApi has stored the token and the auth store is updated; return to the app.
  const dest = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
  router.push(dest)
}
</script>

<template>
  <AuthCard title="Create your account" subtitle="Join Forkly to start ordering.">
    <RegisterForm @success="onSuccess" />
  </AuthCard>
</template>
