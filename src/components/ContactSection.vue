<script setup></script>

<template>
  <section class="section contact" id="contact">
    <div class="container">
      <h2 class="section-title">Contact Us</h2>
      <ul class="contact-list">
        <li>
          <span class="contact-icon" aria-hidden="true">✉️</span>
          <a href="mailto:support@forkly.com">support@forkly.com</a>
        </li>
        <li>
          <span class="contact-icon" aria-hidden="true">📞</span>
          <a href="tel:+6012xxxxxxx">+60 12-xxx xxxx</a>
        </li>
      </ul>
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
</style>
