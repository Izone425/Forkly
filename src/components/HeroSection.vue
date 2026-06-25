<script setup>
import { useLoginAction } from '../composables/useLoginAction.js'

// `message` is shown only when the login service isn't wired up yet.
const { message, onLogin } = useLoginAction()
</script>

<template>
  <section class="hero">
    <div class="container hero-inner">
      <p class="eyebrow">Single-Restaurant Ordering System</p>
      <h1 class="hero-title">Order Made Simple.<br /><span>Experience Dining the Smart Way.</span></h1>
      <p class="hero-sub">
        Welcome to Forkly — a fast and reliable single-restaurant ordering system
        designed for a seamless customer experience. Skip the queue and order with ease.
      </p>

      <!-- PRIMARY CALL TO ACTION — hands off to the login service. -->
      <button type="button" class="btn btn-primary btn-lg" @click="onLogin()">
        Login
      </button>

      <p v-if="message" class="hero-status" role="status">{{ message }}</p>
      <p v-else class="hero-hint">New here? Login to start your ordering experience.</p>
    </div>
  </section>
</template>

<style scoped>
.hero {
  position: relative;
  /* Dark gradient veil over the food photo keeps the white text readable
     while the dish stays subtly visible on the right. */
  background:
    linear-gradient(
      90deg,
      rgba(9, 14, 28, 0.92) 0%,
      rgba(9, 14, 28, 0.76) 58%,
      rgba(9, 14, 28, 0.55) 100%
    ),
    url('/assets/image2.jpg') center / cover no-repeat;
  text-align: center;
  padding: 132px 0 140px;
}
.eyebrow {
  display: inline-block;
  margin: 0 0 20px;
  padding: 6px 16px;
  font-size: 0.78rem;
  font-weight: 600;
  letter-spacing: 0.6px;
  text-transform: uppercase;
  color: #bcd2ff;
  background: rgba(37, 99, 235, 0.22);
  border: 1px solid rgba(255, 255, 255, 0.18);
  border-radius: 999px;
  backdrop-filter: blur(4px);
}
.hero-title {
  margin: 0 0 20px;
  font-size: clamp(2.3rem, 6vw, 3.6rem);
  font-weight: 800;
  color: #ffffff;
  line-height: 1.1;
  letter-spacing: -1px;
  text-shadow: 0 2px 16px rgba(0, 0, 0, 0.35);
}
.hero-title span { color: #8ab4ff; }
.hero-sub {
  max-width: 600px;
  margin: 0 auto 40px;
  font-size: 1.12rem;
  color: #d7deea;
  text-shadow: 0 1px 10px rgba(0, 0, 0, 0.3);
}
.hero-hint { margin-top: 20px; font-size: 0.9rem; color: rgba(255, 255, 255, 0.72); }
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
