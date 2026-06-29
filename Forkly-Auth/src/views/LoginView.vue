<script setup>
import { ref } from 'vue'
import { RouterLink } from 'vue-router'
import AuthCard from '../components/AuthCard.vue'
import FormField from '../components/FormField.vue'
import { login } from '../services/api.js'
import { useHandoff } from '../composables/useHandoff.js'

const { role, embedded, completeHandoff } = useHandoff()

const email = ref('')
const password = ref('')
const submitting = ref(false)
const formError = ref('')
const done = ref(false)
const signingIn = ref(false)

async function onSubmit() {
  formError.value = ''
  submitting.value = true
  try {
    const auth = await login({ email: email.value, password: password.value })
    const handed = completeHandoff(auth.token, auth.user)
    if (handed && embedded) {
      // In the landing's drawer — parent will close us; show a brief state.
      signingIn.value = true
    } else if (!handed) {
      // Reached the auth app directly (no return_to) — confirm in place.
      done.value = true
    }
  } catch (err) {
    formError.value = err.message
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <AuthCard
    title="Welcome back"
    subtitle="Sign in to your Forkly account."
    :role="role"
  >
    <p v-if="signingIn" class="notice notice-success">Signing you in…</p>
    <p v-else-if="done" class="notice notice-success">
      You're signed in. (No return destination was provided.)
    </p>

    <form v-else @submit.prevent="onSubmit">
      <p v-if="formError" class="notice notice-error">{{ formError }}</p>

      <div class="fields">
        <FormField
          v-model="email"
          label="Email"
          type="email"
          autocomplete="email"
          placeholder="you@example.com"
        />
        <FormField
          v-model="password"
          label="Password"
          type="password"
          autocomplete="current-password"
          placeholder="Your password"
        />
      </div>

      <button class="btn btn-primary btn-block" type="submit" :disabled="submitting">
        {{ submitting ? 'Signing in…' : 'Sign in' }}
      </button>
    </form>

    <p class="switch">
      New to Forkly?
      <RouterLink :to="{ path: '/register', query: $route.query }">Create an account</RouterLink>
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
