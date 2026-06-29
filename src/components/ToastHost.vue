<script setup>
import { useToast } from '../stores/toast.js'

const { state, dismiss } = useToast()
</script>

<template>
  <Teleport to="body">
    <div class="toast-host" aria-live="polite" aria-atomic="true">
      <TransitionGroup name="toast">
        <div v-for="t in state.items" :key="t.id" class="toast" @click="dismiss(t.id)">
          <span class="toast-check" aria-hidden="true">✓</span>
          <span class="toast-msg">{{ t.message }}</span>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<style scoped>
.toast-host {
  position: fixed;
  left: 50%;
  bottom: 28px;
  transform: translateX(-50%);
  z-index: 200;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
  pointer-events: none;
}
.toast {
  pointer-events: auto;
  display: inline-flex;
  align-items: center;
  gap: 10px;
  padding: 12px 20px;
  background: var(--color-ink);
  color: #fff;
  border-radius: 999px;
  box-shadow: var(--shadow-lg);
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
}
.toast-check {
  display: grid;
  place-items: center;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: var(--color-primary);
  color: #fff;
  font-size: 0.8rem;
  font-weight: 800;
}

.toast-enter-active,
.toast-leave-active { transition: opacity 0.25s ease, transform 0.25s ease; }
.toast-enter-from { opacity: 0; transform: translateY(12px); }
.toast-leave-to { opacity: 0; transform: translateY(12px); }
</style>
