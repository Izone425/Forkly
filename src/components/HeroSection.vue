<script setup>
import { useLoginAction } from '../composables/useLoginAction.js'

// `message` is shown only when the login service isn't wired up yet.
const { message, onLogin } = useLoginAction()
</script>

<template>
  <section class="hero">
    <div class="container hero-inner">
      <p class="eyebrow">Single-Restaurant Ordering System</p>
      <h1 class="hero-title">Order Simple.<br /><span>Live Easy.</span></h1>
      <p class="hero-sub">
        Welcome to Forkly. Browse the menu, place your order, and skip the queue —
        all from one clean, fast interface.
      </p>

      <!-- PRIMARY CALL TO ACTION — hands off to the login service. -->
      <button type="button" class="btn btn-primary btn-lg" @click="onLogin()">
        Login
      </button>

      <p v-if="message" class="hero-status" role="status">{{ message }}</p>
      <p v-else class="hero-hint">New here? Logging in is the first step to start ordering.</p>
    </div>
  </section>
</template>

<style scoped>
.hero {
  position: relative;
  background:
    radial-gradient(1100px 420px at 50% -140px, rgba(37, 99, 235, 0.1), transparent),
    var(--color-surface);
  text-align: center;
  padding: 104px 0 112px;
  border-bottom: 1px solid var(--color-border);
}
.eyebrow {
  display: inline-block;
  margin: 0 0 20px;
  padding: 6px 16px;
  font-size: 0.78rem;
  font-weight: 600;
  letter-spacing: 0.6px;
  text-transform: uppercase;
  color: var(--color-primary-dark);
  background: var(--color-primary-soft);
  border: 1px solid var(--color-border);
  border-radius: 999px;
}
.hero-title {
  margin: 0 0 20px;
  font-size: clamp(2.3rem, 6vw, 3.6rem);
  font-weight: 800;
  color: var(--color-ink);
  line-height: 1.1;
  letter-spacing: -1px;
}
.hero-title span { color: var(--color-primary); }
.hero-sub {
  max-width: 600px;
  margin: 0 auto 40px;
  font-size: 1.12rem;
  color: var(--color-body);
}
.hero-hint { margin-top: 20px; font-size: 0.9rem; color: var(--color-muted); }
.hero-status {
  margin-top: 20px;
  display: inline-block;
  padding: 10px 18px;
  font-size: 0.9rem;
  font-weight: 500;
  color: var(--color-primary-dark);
  background: var(--color-primary-soft);
  border: 1px solid var(--color-border);
  border-radius: 999px;
}

@media (max-width: 720px) {
  .hero { padding: 72px 0 80px; }
  .btn-lg { width: 100%; max-width: 320px; padding: 16px 0; }
}
</style>
