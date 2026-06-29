<script setup>
// Labelled text input with v-model support and an optional inline error.
defineProps({
  label: { type: String, required: true },
  type: { type: String, default: 'text' },
  modelValue: { type: String, default: '' },
  autocomplete: { type: String, default: 'off' },
  placeholder: { type: String, default: '' },
  error: { type: String, default: '' },
})
defineEmits(['update:modelValue'])
</script>

<template>
  <label class="field">
    <span class="field-label">{{ label }}</span>
    <input
      class="field-input"
      :class="{ 'has-error': error }"
      :type="type"
      :value="modelValue"
      :autocomplete="autocomplete"
      :placeholder="placeholder"
      @input="$emit('update:modelValue', $event.target.value)"
    />
    <span v-if="error" class="field-error">{{ error }}</span>
  </label>
</template>

<style scoped>
/* min-width:0 lets the field shrink inside grid/flex cells (prevents overflow). */
.field { display: flex; flex-direction: column; gap: 6px; min-width: 0; }
.field-label { font-size: 0.85rem; font-weight: 600; color: var(--color-ink); }
.field-input {
  width: 100%;
  box-sizing: border-box;
  min-width: 0;
  font-family: inherit;
  font-size: 1rem;
  color: var(--color-ink);
  padding: 11px 14px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  background: #fff;
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
}
.field-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}
.field-input.has-error { border-color: var(--color-danger); }
.field-error { font-size: 0.8rem; color: var(--color-danger); }
</style>
