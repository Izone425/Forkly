<script setup>
// Static preview only — no interaction, no data fetching.
const items = [
  { emoji: '🍔', name: 'Burger', price: 'RM10' },
  { emoji: '🍟', name: 'Fries', price: 'RM5' },
  { emoji: '☕', name: 'Coffee', price: 'RM6' },
]
</script>

<template>
  <section class="section menu" id="menu">
    <div class="container">
      <h2 class="section-title">Menu Preview</h2>
      <p class="section-sub">A taste of what's on the menu.</p>

      <div class="menu-grid">
        <article v-for="item in items" :key="item.name" class="menu-card">
          <div class="menu-emoji" aria-hidden="true">{{ item.emoji }}</div>
          <h3 class="menu-name">{{ item.name }}</h3>
          <p class="menu-price">{{ item.price }}</p>
        </article>
      </div>

      <p class="menu-note">Preview only — full menu available after login.</p>
    </div>
  </section>
</template>

<style scoped>
.menu { background: var(--color-surface); }
.menu-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 24px;
}
.menu-card {
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  padding: 36px 20px;
  text-align: center;
  box-shadow: var(--shadow);
  transition: transform 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
}
.menu-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--shadow-lg);
  border-color: #cdd9f5;
}
.menu-emoji { font-size: 2.6rem; line-height: 1; margin-bottom: 16px; }
.menu-name { margin: 0 0 6px; font-size: 1.15rem; color: var(--color-ink); font-weight: 700; }
.menu-price { margin: 0; font-size: 1.3rem; font-weight: 800; color: var(--color-primary); }
.menu-note { margin-top: 36px; text-align: center; font-size: 0.9rem; color: var(--color-muted); }

@media (max-width: 720px) {
  .menu-grid { grid-template-columns: 1fr; }
}
</style>
