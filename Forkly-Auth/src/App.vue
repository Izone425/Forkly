<script setup>
import { RouterView } from 'vue-router'

// When loaded inside the landing's drawer iframe (?embed=1), mark the document so
// the layout slims down to fit the drawer (see .embed rules in style.css).
if (new URLSearchParams(window.location.search).get('embed') === '1') {
  document.documentElement.classList.add('embed')
}
</script>

<template>
  <RouterView />
</template>
