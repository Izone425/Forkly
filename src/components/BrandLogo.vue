<script setup>
// Reusable brand logo. Renders /assets/forkly-logo.png and falls back to a
// "[ FORKLY LOGO ]" text box if the image is missing — so the file is not
// required to exist yet. Drop the real logo at public/assets/forkly-logo.png.
import { ref } from 'vue'

defineProps({
  // Show the "Forkly" wordmark + tagline next to the logo box.
  showText: { type: Boolean, default: true },
  tagline: { type: String, default: 'Order Simple. Live Easy.' },
})

const imageOk = ref(true)

// Bound (not a literal src=) so Vite doesn't try to resolve the file at build
// time. A missing logo then 404s at runtime and triggers the text fallback.
const logoSrc = '/assets/forkly-logo.png'
</script>

<template>
  <span class="brand">
    <span class="logo-box">
      <img
        v-show="imageOk"
        class="logo-img"
        :src="logoSrc"
        alt="Forkly logo"
        @error="imageOk = false"
      />
      <span v-show="!imageOk" class="logo-fallback">[ FORKLY LOGO ]</span>
    </span>

    <span v-if="showText" class="brand-text">
      <span class="brand-name">🍴 Forkly</span>
      <span v-if="tagline" class="brand-tagline">{{ tagline }}</span>
    </span>
  </span>
</template>

<style scoped>
.brand {
  display: inline-flex;
  align-items: center;
  gap: 14px;
}

.logo-box {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 52px;
  height: 52px;
  padding: 0 10px;
  border: 1px solid var(--color-border);
  border-radius: 12px;
  background: var(--color-primary-soft);
}
.logo-img { max-height: 36px; max-width: 120px; display: block; }
.logo-fallback {
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.5px;
  color: var(--color-primary);
  white-space: nowrap;
}

.brand-text { display: flex; flex-direction: column; line-height: 1.25; }
.brand-name { font-size: 1.2rem; font-weight: 800; color: var(--color-ink); letter-spacing: -0.2px; }
.brand-tagline { font-size: 0.8rem; color: var(--color-muted); font-weight: 500; }

@media (max-width: 720px) {
  .brand-tagline { display: none; }
}
</style>
