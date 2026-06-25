<script setup>
import { onMounted, ref } from 'vue'
import AddressFormModal from './AddressFormModal.vue'
import { useDeliveryAddress } from '../stores/address.js'
import { useAuth } from '../stores/auth.js'
import { getSavedAddresses, saveAddress } from '../services/userAddressApi.js'

const { state: addr, setSaved, select, addAddress, isSelected } = useDeliveryAddress()
const { state: auth } = useAuth()

const showModal = ref(false)

onMounted(async () => {
  if (addr.loaded) return
  // Saved addresses are owned by the User Service (Izzuwan module).
  // TODO: Fetch user saved addresses from User Service
  // Future REST: GET /users/{userId}/addresses
  // Future gRPC: UserService.GetSavedAddresses(userId)
  const list = await getSavedAddresses(auth.user?.id)
  setSaved(list)
})

async function onSubmit({ address, saveToProfile }) {
  if (saveToProfile) {
    // TODO: Save address to User Service
    // REST: POST /users/{userId}/addresses
    // gRPC: UserService.SaveAddress(address)
    const saved = await saveAddress(auth.user?.id, address)
    addAddress(saved, { persisted: true })
  } else {
    // Temporary address — used for this order only, not saved to the profile.
    addAddress({ ...address, id: 'addr-temp-' + Math.random().toString(36).slice(2, 8) }, {
      persisted: false,
    })
  }
  showModal.value = false
}
</script>

<template>
  <section class="delivery">
    <h3 class="delivery-title">Delivery Address</h3>

    <div class="addr-list">
      <label
        v-for="a in addr.saved"
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
            {{ a.label }}
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

    <button type="button" class="add-addr" @click="showModal = true">
      <span aria-hidden="true">+</span> Add New Address
    </button>

    <AddressFormModal :open="showModal" @close="showModal = false" @submit="onSubmit" />
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
</style>
