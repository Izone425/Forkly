<script setup>
// Full-page account creation route (/register). Registers via the API, signs the
// user in (token stored by authApi), then returns to the app.
import { ref } from 'vue'
import { RouterLink, useRouter, useRoute } from 'vue-router'
import AuthCard from '../components/AuthCard.vue'
import FormField from '../components/FormField.vue'
import { register } from '../services/authApi.js'
import { useAuth } from '../stores/auth.js'

const router = useRouter()
const route = useRoute()
const { signIn } = useAuth()

const fullName = ref('')
const email = ref('')
const password = ref('')
const confirm = ref('')

const errors = ref({})
const submitting = ref(false)
const formError = ref('')

function validate() {
  const e = {}
  if (!fullName.value.trim()) e.fullName = 'Please enter your name.'
  if (!email.value.trim()) e.email = 'Please enter your email.'
  if (password.value.length < 8) e.password = 'Use at least 8 characters.'
  if (confirm.value !== password.value) e.confirm = 'Passwords do not match.'
  errors.value = e
  return Object.keys(e).length === 0
}

async function onSubmit() {
  formError.value = ''
  if (!validate()) return
  submitting.value = true
  try {
    const auth = await register({
      fullName: fullName.value,
      email: email.value,
      password: password.value,
    })
    signIn(auth.user)
    const dest = typeof route.query.redirect === 'string' ? route.query.redirect : '/'
    router.push(dest)
  } catch (err) {
    formError.value = err.message
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <AuthCard title="Create your account" subtitle="Join Forkly to start ordering.">
    <form @submit.prevent="onSubmit">
      <p v-if="formError" class="notice notice-error">{{ formError }}</p>

      <div class="fields">
        <FormField
          v-model="fullName"
          label="Full name"
          autocomplete="name"
          placeholder="Jane Doe"
          :error="errors.fullName"
        />
        <FormField
          v-model="email"
          label="Email"
          type="email"
          autocomplete="email"
          placeholder="you@example.com"
          :error="errors.email"
        />
        <FormField
          v-model="password"
          label="Password"
          type="password"
          autocomplete="new-password"
          placeholder="At least 8 characters"
          :error="errors.password"
        />
        <FormField
          v-model="confirm"
          label="Confirm password"
          type="password"
          autocomplete="new-password"
          placeholder="Re-enter password"
          :error="errors.confirm"
        />
      </div>

      <button class="btn btn-primary btn-block" type="submit" :disabled="submitting">
        {{ submitting ? 'Creating account…' : 'Create account' }}
      </button>
    </form>

    <p class="switch">
      Already have an account?
      <RouterLink :to="{ path: '/login', query: $route.query }">Sign in</RouterLink>
    </p>
  </AuthCard>
</template>

<style scoped>
.fields { display: flex; flex-direction: column; gap: 16px; margin-bottom: 22px; }
.switch { margin: 20px 0 0; text-align: center; font-size: 0.9rem; color: var(--color-body); }
.notice { padding: 10px 14px; border-radius: var(--radius-sm); font-size: 0.88rem; margin: 0 0 18px; }
.notice-error { background: #fef2f2; color: var(--color-danger); border: 1px solid #fecaca; }
.notice-success { background: #f0fdf4; color: var(--color-success); border: 1px solid #bbf7d0; }
</style>
