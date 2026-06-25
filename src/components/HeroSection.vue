<script setup></script>

<template>
  <section class="hero">
    <div class="container hero-inner">
      <h1 class="hero-title">Order Made Simple.<br /><span>Experience Dining the Smart Way.</span></h1>
      <p class="hero-sub">
        Welcome to Forkly — a fast and reliable single-restaurant ordering system
        designed for a seamless customer experience. Skip the queue and order with ease.
      </p>
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
  padding: 128px 0 136px;
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
  margin: 0 auto;
  font-size: 1.12rem;
  color: #d7deea;
  text-shadow: 0 1px 10px rgba(0, 0, 0, 0.3);
}

@media (max-width: 720px) {
  .hero { padding: 72px 0 80px; }
}
</style>
