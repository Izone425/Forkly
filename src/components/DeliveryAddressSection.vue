<script setup>
import { onMounted, ref, watch } from 'vue'
import AddressFormModal from './AddressFormModal.vue'
import { useDeliveryAddress, MAX_SAVED_ADDRESSES } from '../stores/address.js'
import { useAuth } from '../stores/auth.js'
import { addAddress, absoluteUrl } from '../services/authApi.js'

const { state: addr, all, savedCount, canSaveMore, setPersisted, select, isSelected, addTemporary } =
  useDeliveryAddress()
const { state: auth, setUser } = useAuth()

const showModal = ref(false)
const saving = ref(false)
const error = ref('')

// Always reflect the signed-in user's REAL saved addresses (from the User
// service via auth.user). No mock fallback: an empty profile shows an empty list
// and the user must add an address before they can check out.
function syncFromProfile() {
  setPersisted(auth.user?.addresses || [])
}
onMounted(syncFromProfile)
watch(() => auth.user?.addresses, syncFromProfile, { deep: true })

async function onSubmit({ address, saveToProfile }) {
  error.value = ''

  // The 3-address cap applies to SAVED addresses only; order-only is always fine.
  if (saveToProfile && !canSaveMore.value) {
    error.value = `You can save up to ${MAX_SAVED_ADDRESSES} addresses. Remove one in your profile, or uncheck “save” to use this address for this order only.`
    return
  }

  // Order-only address — kept in the cart for this order, not persisted.
  if (!saveToProfile) {
    addTemporary(address)
    showModal.value = false
    return
  }

  // Persist to the User service (Izzuwan). It owns the address book — we never
  // store addresses on our side. It returns the full updated user.
  saving.value = true
  try {
    const beforeIds = new Set((auth.user?.addresses || []).map((a) => a.id))
    const payload = {
      label: address.label,
      addressLine1: address.addressLine1,
      addressLine2: address.addressLine2,
      city: address.city,
      state: address.state,
      postcode: address.postcode,
      deliveryNotes: address.notes,
      isDefault: savedCount.value === 0, // first saved address becomes the default
    }
    const updated = await addAddress(payload)

    // Keep the auth store (profile + header) in sync, then select the new address.
    setUser({ ...updated, name: updated.fullName || updated.email, avatarUrl: absoluteUrl(updated.avatarUrl) })
    setPersisted(updated.addresses)
    const added = (updated.addresses || []).find((a) => !beforeIds.has(a.id))
    if (added) select(addr.persisted.find((a) => a.id === added.id) || added)

    showModal.value = false
  } catch (e) {
    error.value = e?.message || 'Could not save the address. Please try again.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <section class="delivery">
    <h3 class="delivery-title">Delivery Address</h3>

    <p v-if="!all.length" class="addr-empty">
      You have no saved delivery address yet. Add one below to place your order.
    </p>

    <div v-else class="addr-list">
      <label
        v-for="a in all"
        :key="a.id"
        class="addr-option"
        :class="{ selected: isSelected(a) }"
      >
        <input
          class="addr-radio"
          type="radio"
          name="delivery-address"
          :checked="isSelected(a)"
          @change="select(a)"
        />
        <span class="addr-body">
          <span class="addr-label">
            {{ a.label || 'Address' }}
            <span v-if="a.isDefault" class="addr-tag">Default</span>
            <span v-else-if="a.temporary" class="addr-tag temp">This order</span>
          </span>
          <span class="addr-lines">
            {{ a.addressLine1 }}<template v-if="a.addressLine2">, {{ a.addressLine2 }}</template><br />
            {{ a.postcode }} {{ a.city }}<template v-if="a.state">, {{ a.state }}</template>
          </span>
        </span>
      </label>
    </div>

    <p v-if="error" class="addr-error" role="alert">{{ error }}</p>

    <button type="button" class="add-addr" @click="showModal = true">
      <span aria-hidden="true">+</span> Add New Address
    </button>

    <p v-if="!canSaveMore" class="addr-limit">
      You've saved the maximum of {{ MAX_SAVED_ADDRESSES }} addresses. A new one can still be used for this order only.
    </p>

    <AddressFormModal
      :open="showModal"
      :can-save="canSaveMore"
      :saving="saving"
      @close="showModal = false"
      @submit="onSubmit"
    />
  </section>
</template>

<style scoped>
.delivery {
  margin: 18px 0;
  padding-top: 18px;
  border-top: 1px solid var(--color-border);
}
.delivery-title {
  margin: 0 0 12px;
  font-size: 0.82rem;
  font-weight: 700;
  letter-spacing: 0.8px;
  text-transform: uppercase;
  color: var(--color-muted);
}

.addr-empty {
  margin: 0 0 6px;
  padding: 12px 14px;
  font-size: 0.88rem;
  color: var(--color-body);
  background: var(--color-surface);
  border: 1px dashed var(--color-border);
  border-radius: var(--radius-sm);
}

.addr-list { display: grid; gap: 10px; }
.addr-option {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 12px 14px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: border-color 0.15s ease, background 0.15s ease;
}
.addr-option:hover { border-color: #cdd9f5; }
.addr-option.selected { border-color: var(--color-primary); background: var(--color-primary-soft); }

.addr-radio { margin-top: 3px; width: 16px; height: 16px; accent-color: var(--color-primary); flex: none; }

.addr-body { display: flex; flex-direction: column; gap: 3px; }
.addr-label {
  font-weight: 700;
  color: var(--color-ink);
  font-size: 0.95rem;
  display: inline-flex;
  align-items: center;
  gap: 8px;
}
.addr-tag {
  font-size: 0.66rem;
  font-weight: 700;
  letter-spacing: 0.4px;
  text-transform: uppercase;
  color: var(--color-primary);
  background: #fff;
  border: 1px solid #cdd9f5;
  border-radius: 999px;
  padding: 1px 8px;
}
.addr-tag.temp { color: #b45309; border-color: #fcd34d; background: #fffbeb; }
.addr-lines { font-size: 0.85rem; color: var(--color-body); line-height: 1.45; }

.addr-error { margin: 10px 0 0; color: #d33; font-size: 0.85rem; }

.add-addr {
  margin-top: 12px;
  width: 100%;
  background: #fff;
  border: 1px dashed var(--color-border);
  color: var(--color-primary);
  font-family: inherit;
  font-size: 0.92rem;
  font-weight: 700;
  padding: 11px 0;
  border-radius: var(--radius-sm);
  cursor: pointer;
  transition: border-color 0.15s ease, background 0.15s ease;
}
.add-addr:hover { border-color: var(--color-primary); background: var(--color-primary-soft); }

.addr-limit { margin: 8px 0 0; font-size: 0.78rem; color: var(--color-muted); text-align: center; }
</style>
