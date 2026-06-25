<script setup>
import AppHeader from '../components/AppHeader.vue'
import HeroSection from '../components/HeroSection.vue'
import AboutSection from '../components/AboutSection.vue'
import MenuPreview from '../components/MenuPreview.vue'
import ContactSection from '../components/ContactSection.vue'
import AppFooter from '../components/AppFooter.vue'
import FloatingCart from '../components/FloatingCart.vue'
</script>

<template>
  <AppHeader />
  <main>
    <HeroSection />
    <AboutSection />
    <MenuPreview />
    <ContactSection />
  </main>
  <AppFooter />

  <!-- Floating cart (bottom-left): opens the order page with items kept. -->
  <FloatingCart />
</template>
