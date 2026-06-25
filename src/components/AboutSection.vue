<script setup></script>

<template>
  <section class="section about" id="about">
    <div class="container about-grid">
      <div class="about-copy">
        <h2 class="about-title">About Us</h2>
        <p class="about-text">
          Forkly is a single-restaurant ordering platform designed to simplify the
          food ordering experience.
        </p>
        <p class="about-text">
          It allows customers to browse menus, place orders, and track their food in
          real-time through a clean and efficient system.
        </p>
      </div>

      <!-- Forkly logo (background-image avoids a static asset import). -->
      <div class="about-logo" role="img" aria-label="Forkly logo"></div>
    </div>
  </section>
</template>

<style scoped>
.about { background: var(--color-bg); }

.about-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  align-items: center;
  gap: 48px;
}

.about-copy { text-align: justify; }
.about-title {
  margin: 0 0 18px;
  font-size: clamp(1.7rem, 4vw, 2.1rem);
  font-weight: 800;
  color: var(--color-ink);
  letter-spacing: -0.4px;
  text-align: center; /* heading stays centered; paragraphs justified */
}
.about-text {
  margin: 0 0 16px;
  font-size: 1.12rem;
}
.about-text:last-child { margin-bottom: 0; }

/* Transparent logo, shown whole (contain) and blended into the section. */
.about-logo {
  min-height: 220px;
  background: url('/assets/forkly-transparent-logo.png') center / contain no-repeat;
}

@media (max-width: 820px) {
  .about-grid { grid-template-columns: 1fr; gap: 24px; }
  .about-logo { min-height: 160px; order: -1; }
}
</style>
