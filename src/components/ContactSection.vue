<script setup></script>

<template>
  <section class="section contact" id="contact">
    <div class="container">
      <h2 class="section-title">Contact Us</h2>
      <p class="section-sub">Need help? Reach out to us.</p>
      <ul class="contact-list">
        <li>
          <span class="contact-icon" aria-hidden="true">✉️</span>
          <a href="mailto:support@forkly.com">support@forkly.com</a>
        </li>
        <li>
          <span class="contact-icon" aria-hidden="true">📞</span>
          <a href="tel:0380709933">03-80709933</a>
        </li>
      </ul>
      <p class="contact-team">Forkly Support Team</p>
    </div>
  </section>
</template>

<style scoped>
.contact { background: var(--color-bg); }
.contact-list {
  list-style: none;
  margin: 0 auto;
  padding: 0;
  max-width: 380px;
  display: grid;
  gap: 14px;
}
.contact-list li {
  display: flex;
  align-items: center;
  gap: 14px;
  justify-content: center;
  font-size: 1.05rem;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  padding: 14px 20px;
}
.contact-list a:hover { color: var(--color-primary); }
.contact-icon { font-size: 1.2rem; }
.contact-team {
  margin: 28px 0 0;
  text-align: center;
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--color-muted);
}
</style>
