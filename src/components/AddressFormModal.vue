<script setup>
import { reactive, ref, computed, watch } from 'vue'

const props = defineProps({
  open: { type: Boolean, default: false },
  // Whether the profile still has room to SAVE a new address (cap is 3).
  canSave: { type: Boolean, default: true },
  saving: { type: Boolean, default: false },
})
const emit = defineEmits(['close', 'submit'])

const form = reactive({
  label: 'Home',
  addressLine1: '',
  addressLine2: '',
  postcode: '',
  city: '',
  state: '',
  notes: '',
})
const saveToProfile = ref(false)

// Can't tick "save" when the profile is already at the 3-address cap.
watch(
  () => props.canSave,
  (ok) => { if (!ok) saveToProfile.value = false },
  { immediate: true },
)

const valid = computed(
  () =>
    form.label &&
    form.addressLine1.trim() &&
    form.postcode.trim() &&
    form.city.trim() &&
    form.state.trim(),
)

function reset() {
  Object.assign(form, {
    label: 'Home', addressLine1: '',
    addressLine2: '', postcode: '', city: '', state: '', notes: '',
  })
  saveToProfile.value = false
}

function close() {
  if (props.saving) return
  emit('close')
}

function submit() {
  if (!valid.value || props.saving) return
  emit('submit', { address: { ...form }, saveToProfile: saveToProfile.value })
  // Parent closes the modal on success; reset for the next open.
  reset()
}
</script>

<template>
  <Teleport to="body">
    <div v-if="open" class="modal-overlay" @click.self="close">
      <div class="modal" role="dialog" aria-modal="true" aria-label="Add new address">
        <div class="modal-head">
          <h3 class="modal-title">Add New Address</h3>
          <button type="button" class="modal-close" aria-label="Close" @click="close">×</button>
        </div>

        <form class="addr-form" @submit.prevent="submit">
          <div class="field">
            <label for="af-label">Address Label</label>
            <select id="af-label" v-model="form.label">
              <option>Home</option>
              <option>Office</option>
              <option>Other</option>
            </select>
          </div>

          <div class="field">
            <label for="af-l1">Address Line 1</label>
            <input id="af-l1" v-model="form.addressLine1" type="text" placeholder="House no., street" />
          </div>

          <div class="field">
            <label for="af-l2">Address Line 2 <span class="opt">(optional)</span></label>
            <input id="af-l2" v-model="form.addressLine2" type="text" placeholder="Unit, building" />
          </div>

          <div class="field-row">
            <div class="field">
              <label for="af-post">Postcode</label>
              <input id="af-post" v-model="form.postcode" type="text" inputmode="numeric" placeholder="43000" />
            </div>
            <div class="field">
              <label for="af-city">City</label>
              <input id="af-city" v-model="form.city" type="text" placeholder="Kajang" />
            </div>
          </div>

          <div class="field">
            <label for="af-state">State</label>
            <input id="af-state" v-model="form.state" type="text" placeholder="Selangor" />
          </div>

          <div class="field">
            <label for="af-notes">Delivery Notes <span class="opt">(optional)</span></label>
            <textarea id="af-notes" v-model="form.notes" rows="2" placeholder="e.g. Leave at the guardhouse"></textarea>
          </div>

          <label class="save-toggle" :class="{ disabled: !canSave }">
            <input v-model="saveToProfile" type="checkbox" :disabled="!canSave" />
            <span v-if="canSave">Save this address to my profile for future orders</span>
            <span v-else>You've reached the maximum of 3 saved addresses — this one will be used for this order only.</span>
          </label>

          <div class="addr-actions">
            <button type="button" class="btn btn-ghost" :disabled="saving" @click="close">Cancel</button>
            <button type="submit" class="btn btn-primary" :disabled="!valid || saving">
              {{ saving ? 'Saving…' : (saveToProfile ? 'Save & Use' : 'Use Address') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 100;
  background: rgba(15, 23, 42, 0.5);
  backdrop-filter: blur(2px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}
.modal {
  width: 100%;
  max-width: 480px;
  max-height: 90vh;
  overflow-y: auto;
  background: var(--color-bg);
  border-radius: var(--radius);
  box-shadow: var(--shadow-lg);
  padding: 26px;
}
.modal-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 18px;
}
.modal-title { margin: 0; font-size: 1.3rem; font-weight: 800; color: var(--color-ink); }
.modal-close {
  border: none;
  background: var(--color-surface);
  color: var(--color-muted);
  width: 32px;
  height: 32px;
  border-radius: 50%;
  font-size: 1.3rem;
  line-height: 1;
  cursor: pointer;
}
.modal-close:hover { background: #fde8e8; color: #d33; }

.addr-form { display: grid; gap: 14px; }
.field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.field { display: flex; flex-direction: column; min-width: 0; }
.field input,
.field select,
.field textarea { width: 100%; }
.field label {
  margin-bottom: 6px;
  font-size: 0.82rem;
  font-weight: 600;
  color: var(--color-ink);
}
.opt { color: var(--color-muted); font-weight: 500; }
.field input,
.field select,
.field textarea {
  font-family: inherit;
  font-size: 0.95rem;
  color: var(--color-ink);
  padding: 10px 12px;
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  transition: border-color 0.15s ease, box-shadow 0.15s ease;
  resize: vertical;
}
.field input:focus,
.field select:focus,
.field textarea:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.15);
}

.save-toggle {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 0.9rem;
  color: var(--color-body);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  padding: 12px 14px;
  cursor: pointer;
}
.save-toggle input { width: 16px; height: 16px; }
.save-toggle.disabled { color: var(--color-muted); cursor: default; }

.addr-actions { display: flex; gap: 10px; margin-top: 4px; }
.addr-actions .btn { flex: 1; padding: 12px 0; text-align: center; }
.addr-actions .btn:disabled { opacity: 0.6; cursor: default; transform: none; }

@media (max-width: 520px) {
  .field-row { grid-template-columns: 1fr; }
}
</style>
