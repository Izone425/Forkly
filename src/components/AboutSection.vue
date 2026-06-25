<script setup></script>

<template>
  <section class="section about" id="about">
    <div class="container about-grid">
      <div class="about-copy">
        <h2 class="about-title">About Forkly</h2>
        <p class="about-text">
          Forkly is a single-restaurant ordering platform designed to simplify the
          food ordering experience.
        </p>
        <p class="about-text">
          It allows customers to browse menus, place orders, and track their food in
          real-time through a clean and efficient system.
        </p>
      </div>

      <!-- Decorative visual (background-image avoids static asset import). -->
      <div
        class="about-media"
        role="img"
        aria-label="Browsing the Forkly menu on a tablet"
      ></div>
    </div>
  </section>
</template>

<style scoped>
.about { background: var(--color-bg); }

.about-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  align-items: center;
  gap: 56px;
}

.about-title {
  margin: 0 0 18px;
  font-size: clamp(1.7rem, 4vw, 2.1rem);
  font-weight: 800;
  color: var(--color-ink);
  letter-spacing: -0.4px;
}
.about-text {
  margin: 0 0 16px;
  font-size: 1.12rem;
}
.about-text:last-child { margin-bottom: 0; }

.about-media {
  min-height: 360px;
  border-radius: var(--radius);
  background: url('/assets/image3.jpg') center / cover no-repeat;
  box-shadow: var(--shadow-lg);
  border: 1px solid var(--color-border);
}

@media (max-width: 820px) {
  .about-grid { grid-template-columns: 1fr; gap: 36px; }
  .about-copy { text-align: center; }
  .about-media { min-height: 240px; order: -1; }
}
</style>
