<script setup></script>

<template>
  <footer class="site-footer">
    <div class="container footer-inner">
      <span class="footer-brand">🍴 Forkly</span>
      <p class="footer-copy">© 2026 Forkly. A microservices training project.</p>
    </div>
  </footer>
</template>

<style scoped>
.site-footer {
  background: var(--color-ink);
  color: #cbd5e1;
  padding: 28px 0;
}
.footer-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}
.footer-brand { color: #fff; font-weight: 800; }
.footer-copy { margin: 0; font-size: 0.9rem; color: var(--color-muted); }

@media (max-width: 720px) {
  .footer-inner { flex-direction: column; text-align: center; }
}
</style>
