<script setup>
// The animated status icon: a chef shaking a pan over rising steam, a scooter driving
// with motion lines trailing it, sand falling through an hourglass. Shown beside the
// status badge on every row of the active-orders panel, and on the current step of the
// tracking timeline.
//
// Purely decorative — every caller already renders a visible text label (the badge, the
// step label), so this is aria-hidden and adds nothing to the accessible name.
//
// TWO LAYERS. The glyph owns the transform (it bobs, drives, flips); the fx layer stays
// put and draws particles around it. Hanging particles off the moving glyph would drag
// them along with it — the steam would wobble with the chef instead of rising off the pan.
//
// All the motion in the app lives here: every animated icon renders through this
// component, so the keyframes never need to be global.
import { computed } from 'vue'
import { phaseIcon } from '../utils/orderStatus.js'

const props = defineProps({
  phase: { type: String, default: null },
  // The tracking timeline animates only the step the order is actually on — five icons
  // moving at once is noise.
  animated: { type: Boolean, default: true },
})

const icon = computed(() => phaseIcon(props.phase))
</script>

<template>
  <span v-if="icon" class="status-icon" :class="[phase, { animated }]" aria-hidden="true">
    <span class="si-fx"></span>
    <span class="si-glyph">{{ icon }}</span>
  </span>
</template>

<style scoped>
/* =========================================================================
   Structure
   -------------------------------------------------------------------------
   Sizes are in `em` throughout, so every scene works unchanged at panel-row
   size (~1.1rem) and inside a timeline dot (~0.95rem).

   Particles are drawn in `currentColor`, so they adapt on their own: ink on a
   light panel row, white inside the timeline's blue current-step dot.

   Every particle rests at `opacity: 0` and NONE use `animation-fill-mode`.
   That's load-bearing: the reduced-motion guard in style.css collapses
   animations to 0.01ms / one iteration, after which an element snaps back to
   its base style. Give a particle a visible base state and a motion-sensitive
   visitor gets a scatter of frozen dots instead of a clean icon.
   ========================================================================= */
.status-icon {
  position: relative;
  display: inline-block;
  line-height: 1;
  font-size: inherit;
}

.si-glyph {
  display: inline-block;
  will-change: transform;
}

/* The particle stage: centred on the glyph, never intercepts a click. */
.si-fx {
  position: absolute;
  inset: 0;
  pointer-events: none;
}
.si-fx::before,
.si-fx::after {
  content: '';
  position: absolute;
  opacity: 0;
  background: currentColor;
}

/* =========================================================================
   unpaid — a card tapped against a reader, ripple spreading out
   ========================================================================= */
.status-icon.unpaid.animated .si-glyph { animation: si-tap 1.8s ease-in-out infinite; }
@keyframes si-tap {
  0%, 55%, 100% { transform: translateX(0) scale(1); }
  63% { transform: translateX(-1px) scale(0.94); }
  70% { transform: translateX(1px) scale(1.06); }
  78% { transform: translateX(0) scale(1); }
}

/* The ripple: a ring pushed out from the card at the moment of contact. */
.status-icon.unpaid.animated .si-fx::before {
  left: 50%;
  top: 50%;
  width: 1em;
  height: 1em;
  margin: -0.5em 0 0 -0.5em;
  border-radius: 50%;
  background: none;
  border: 1px solid currentColor;
  animation: si-ripple 1.8s ease-out infinite;
}
@keyframes si-ripple {
  0%, 60% { opacity: 0; transform: scale(0.5); }
  70% { opacity: 0.5; transform: scale(0.8); }
  100% { opacity: 0; transform: scale(1.7); }
}

/* =========================================================================
   confirmed — sand runs through the glass, then it flips over
   ========================================================================= */
.status-icon.confirmed.animated .si-glyph { animation: si-flip 3.4s ease-in-out infinite; }
@keyframes si-flip {
  0%, 72% { transform: rotate(0deg); }
  88%, 100% { transform: rotate(180deg); }
}

/* A grain falling through the waist of the hourglass, while it's upright. */
.status-icon.confirmed.animated .si-fx::before {
  left: 50%;
  top: 42%;
  width: 0.09em;
  height: 0.09em;
  margin-left: -0.045em;
  border-radius: 50%;
  animation: si-sand 0.85s linear infinite;
}
@keyframes si-sand {
  0% { opacity: 0; transform: translateY(0); }
  20%, 70% { opacity: 0.65; }
  100% { opacity: 0; transform: translateY(0.3em); }
}
/* The grain stops while the glass is turning over — sand doesn't fall sideways. */
.status-icon.confirmed.animated .si-fx { animation: si-sand-gate 3.4s step-end infinite; }
@keyframes si-sand-gate {
  0%, 72% { opacity: 1; }
  72.01%, 100% { opacity: 0; }
}

/* =========================================================================
   preparing — the chef shakes the pan, steam rises off it
   ========================================================================= */
.status-icon.preparing.animated .si-glyph { animation: si-cook 1.5s ease-in-out infinite; }
@keyframes si-cook {
  0%, 100% { transform: translateY(0) rotate(-6deg); }
  25% { transform: translateY(-2px) rotate(4deg); }
  50% { transform: translateY(0) rotate(-4deg); }
  75% { transform: translateY(-1px) rotate(5deg); }
}

/* Two puffs, offset by half a cycle, so the steam reads as continuous rather than
   as one dot pulsing. They rise off the fx layer, so the chef's bob doesn't drag them. */
.status-icon.preparing.animated .si-fx::before,
.status-icon.preparing.animated .si-fx::after {
  top: -0.1em;
  width: 0.15em;
  height: 0.15em;
  border-radius: 50%;
  animation: si-steam 1.6s ease-out infinite;
}
.status-icon.preparing.animated .si-fx::before { left: 38%; }
.status-icon.preparing.animated .si-fx::after { left: 58%; animation-delay: 0.8s; }
@keyframes si-steam {
  0% { opacity: 0; transform: translateY(0) scale(0.5); }
  30% { opacity: 0.45; }
  100% { opacity: 0; transform: translateY(-0.55em) translateX(0.08em) scale(1.5); }
}

/* =========================================================================
   ready — the plate lands on the pass, the bell dings, the food steams
   ========================================================================= */
.status-icon.ready.animated .si-glyph { animation: si-ding 2.2s ease-in-out infinite; }
@keyframes si-ding {
  0%, 62%, 100% { transform: scale(1) translateY(0); }
  70% { transform: scale(1.16) translateY(-1px); }
  78% { transform: scale(0.97) translateY(0); }
  86% { transform: scale(1.03); }
}

/* The ding: a ring pinging outward from the plate. */
.status-icon.ready.animated .si-fx::after {
  left: 50%;
  top: 50%;
  width: 1em;
  height: 1em;
  margin: -0.5em 0 0 -0.5em;
  border-radius: 50%;
  background: none;
  border: 1px solid currentColor;
  animation: si-ping 2.2s ease-out infinite;
}
@keyframes si-ping {
  0%, 62% { opacity: 0; transform: scale(0.6); }
  72% { opacity: 0.45; transform: scale(0.95); }
  100% { opacity: 0; transform: scale(1.6); }
}

/* Steam off the hot plate — slower and fainter than the chef's, it's just resting. */
.status-icon.ready.animated .si-fx::before {
  left: 47%;
  top: -0.05em;
  width: 0.13em;
  height: 0.13em;
  border-radius: 50%;
  animation: si-steam-soft 2.2s ease-out infinite;
}
@keyframes si-steam-soft {
  0% { opacity: 0; transform: translateY(0) scale(0.6); }
  30% { opacity: 0.3; }
  100% { opacity: 0; transform: translateY(-0.45em) scale(1.3); }
}

/* =========================================================================
   out — the scooter drives, motion lines streaming behind it
   ========================================================================= */
.status-icon.out.animated .si-glyph { animation: si-ride 2s ease-in-out infinite; }
@keyframes si-ride {
  0%, 100% { transform: translateX(-2px) translateY(0) rotate(-1deg); }
  40% { transform: translateX(2px) translateY(-1px) rotate(1deg); }
  50% { transform: translateX(2px) translateY(0) rotate(1deg); }
  60% { transform: translateX(2px) translateY(-1px) rotate(1deg); }
}

/* Two speed lines trailing off the back, staggered so they read as a stream. */
.status-icon.out.animated .si-fx::before,
.status-icon.out.animated .si-fx::after {
  left: -0.15em;
  height: 0.06em;
  width: 0.3em;
  border-radius: 999px;
  animation: si-speed 1s linear infinite;
}
.status-icon.out.animated .si-fx::before { top: 38%; }
.status-icon.out.animated .si-fx::after { top: 62%; width: 0.2em; animation-delay: 0.5s; }
@keyframes si-speed {
  0% { opacity: 0; transform: translateX(0.25em) scaleX(0.4); }
  30% { opacity: 0.5; }
  100% { opacity: 0; transform: translateX(-0.3em) scaleX(1); }
}

/* =========================================================================
   delivered — it lands, with a burst. Played ONCE. Never looped: the order is
   finished, and a permanent party in the corner of the timeline is a nag.
   ========================================================================= */
.status-icon.delivered.animated .si-glyph { animation: si-celebrate 0.75s cubic-bezier(0.34, 1.56, 0.64, 1) 1; }
@keyframes si-celebrate {
  0% { transform: scale(0.4) rotate(-14deg); opacity: 0; }
  60% { transform: scale(1.22) rotate(6deg); opacity: 1; }
  100% { transform: scale(1) rotate(0deg); opacity: 1; }
}

/* Confetti: two specks thrown out and away as it pops. */
.status-icon.delivered.animated .si-fx::before,
.status-icon.delivered.animated .si-fx::after {
  left: 50%;
  top: 50%;
  width: 0.12em;
  height: 0.12em;
  border-radius: 1px;
  animation: si-confetti 0.75s ease-out 1;
}
.status-icon.delivered.animated .si-fx::before { --cx: -0.55em; --cy: -0.45em; }
.status-icon.delivered.animated .si-fx::after { --cx: 0.5em; --cy: -0.5em; animation-delay: 0.08s; }
@keyframes si-confetti {
  0% { opacity: 0; transform: translate(0, 0) scale(0.4) rotate(0deg); }
  30% { opacity: 0.9; }
  100% { opacity: 0; transform: translate(var(--cx), var(--cy)) scale(1) rotate(140deg); }
}

/* =========================================================================
   cancelled — deliberately still. Nothing is happening, so nothing waves.
   ========================================================================= */
.status-icon.cancelled { opacity: 0.6; }
</style>
