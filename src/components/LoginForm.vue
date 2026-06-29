<script setup>
// Email + password sign-in form. Shared by the slide-in LoginDrawer (compact) and
// the full /login page (wrapped in AuthCard). On success it stores the session via
// the auth store and emits `success` so the host can close/redirect.
import { ref } from 'vue'
import { RouterLink } from 'vue-router'
import FormField from './FormField.vue'
import { login } from '../services/authApi.js'
import { useAuth } from '../stores/auth.js'

defineProps({
  // Show the "New to Forkly? Create an account" switch (hidden in the drawer,
  // where navigating away would drop the in-progress checkout).
  showSwitch: { type: Boolean, default: true },
})
const emit = defineEmits(['success'])

const { signIn } = useAuth()

const email = ref('')
const password = ref('')
const submitting = ref(false)
const formError = ref('')

async function onSubmit() {
  formError.value = ''
  submitting.value = true
  try {
    const auth = await login({ email: email.value, password: password.value })
    signIn(auth.user)
    emit('success', auth.user)
  } catch (err) {
    formError.value = err.message
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <form @submit.prevent="onSubmit">
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

    <p v-if="showSwitch" class="switch">
      New to Forkly?
      <RouterLink :to="{ path: '/register', query: $route.query }">Create an account</RouterLink>
    </p>
  </form>
</template>

<style scoped>
.fields { display: flex; flex-direction: column; gap: 16px; margin-bottom: 22px; }
.switch { margin: 20px 0 0; text-align: center; font-size: 0.9rem; color: var(--color-body); }
.notice { padding: 10px 14px; border-radius: var(--radius-sm); font-size: 0.88rem; margin: 0 0 18px; }
.notice-error { background: #fef2f2; color: var(--color-danger); border: 1px solid #fecaca; }
</style>
