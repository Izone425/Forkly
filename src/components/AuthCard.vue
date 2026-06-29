<script setup>
// Centered card shell shared by the login and register screens.
defineProps({
  title: { type: String, required: true },
  subtitle: { type: String, default: '' },
  role: { type: String, default: '' },
})
</script>

<template>
  <div class="auth-wrap">
    <div class="auth-card">
      <div class="brand">🍴 Forkly</div>
      <span v-if="role" class="role-pill">{{ role }} access</span>
      <h1 class="auth-title">{{ title }}</h1>
      <p v-if="subtitle" class="auth-sub">{{ subtitle }}</p>
      <slot />
    </div>
    <p class="auth-foot">Forkly — Order Simple. Live Easy.</p>
  </div>
</template>

<style scoped>
.auth-wrap {
  min-height: 100svh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 18px;
  padding: 32px 20px;
}
.auth-card {
  width: 100%;
  max-width: 420px;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  box-shadow: var(--shadow-lg);
  padding: 36px 32px;
}
.brand { font-size: 1.3rem; font-weight: 800; color: var(--color-ink); margin-bottom: 18px; }
.role-pill {
  display: inline-block;
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: capitalize;
  letter-spacing: 0.3px;
  color: var(--color-primary);
  background: var(--color-primary-soft);
  border-radius: 999px;
  padding: 3px 10px;
}
.auth-title {
  margin: 12px 0 6px;
  font-size: 1.5rem;
  font-weight: 800;
  color: var(--color-ink);
  letter-spacing: -0.3px;
}
.auth-sub { margin: 0 0 22px; color: var(--color-muted); font-size: 0.95rem; }
.auth-foot { color: var(--color-muted); font-size: 0.8rem; }
</style>
