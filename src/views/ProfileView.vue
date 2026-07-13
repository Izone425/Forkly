<script setup>
import { ref, onMounted, computed } from 'vue'
import FormField from '../components/FormField.vue'
import { useRouter, RouterLink } from 'vue-router'
import {
  me,
  updateProfile,
  changePassword,
  uploadAvatar,
  addAddress,
  updateAddress,
  deleteAddress,
  setDefaultAddress,
  absoluteUrl,
  getToken,
} from '../services/authApi.js'
import { fetchOrderHistory } from '../services/orderGateway.js'
import {
  statusLabel,
  statusClass,
  paymentLabel,
  paymentClass,
  canPay,
  money,
} from '../utils/orderStatus.js'
import { useAuth } from '../stores/auth.js'

const router = useRouter()
const { setUser, logout } = useAuth()

const loading = ref(true)
const loadError = ref('')

// Profile form (name + phone only — addresses are managed separately below)
const form = ref({ fullName: '', phone: '' })
const email = ref('')
const roles = ref([])
const avatarUrl = ref('')

const savingProfile = ref(false)
const profileMsg = ref('')
const profileErr = ref('')

// Delivery addresses
const addresses = ref([])
const blankAddress = () => ({
  label: '', addressLine1: '', addressLine2: '',
  city: '', state: '', postcode: '', deliveryNotes: '', isDefault: false,
})
const addrForm = ref(blankAddress())
const editingId = ref(null)   // null = not editing; 0 = adding new; >0 = editing that id
const savingAddr = ref(false)
const addrErr = ref('')
const showAddrForm = computed(() => editingId.value !== null)
const MAX_ADDRESSES = 3
const canAddAddress = computed(() => addresses.value.length < MAX_ADDRESSES)

const uploadingAvatar = ref(false)
const avatarErr = ref('')

// Order history (read-only)
const orders = ref([])
const ordersLoading = ref(true)
const ordersErr = ref('')

function orderDate(iso) {
  const d = new Date(iso)
  return isNaN(d) ? '' : d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}

// Password form
const pw = ref({ current: '', next: '', confirm: '' })
const changingPw = ref(false)
const pwMsg = ref('')
const pwErr = ref('')

const avatarSrc = computed(() => absoluteUrl(avatarUrl.value))
const initial = computed(() => (form.value.fullName || email.value || '?').charAt(0).toUpperCase())

function applyUser(u) {
  form.value = {
    fullName: u.fullName || '',
    phone: u.phone || '',
  }
  email.value = u.email || ''
  // Hide the implicit "client" role; keep meaningful ones like "admin".
  roles.value = (u.roles || []).filter((r) => r.toLowerCase() !== 'client')
  avatarUrl.value = u.avatarUrl || ''
  addresses.value = u.addresses || []
}

// Notify the landing so the header (name + avatar) updates live.
function emitUpdated(u) {
  // Refresh the landing header (name + avatar) live.
  setUser({ ...u, name: u.fullName || u.email, avatarUrl: absoluteUrl(u.avatarUrl) })
}

onMounted(async () => {
  if (!getToken()) {
    loadError.value = 'Your session has expired. Please sign in again.'
    loading.value = false
    logout()
    router.push('/')
    return
  }
  let profile
  try {
    profile = await me()
    applyUser(profile)
  } catch {
    loadError.value = 'Your session has expired. Please sign in again.'
    logout()
    router.push('/')
    loading.value = false
    return
  }
  loading.value = false

  // Order history loads independently — a failure here shouldn't block the page.
  // Read from the Order service (source of truth), keyed by the user's id.
  try {
    orders.value = await fetchOrderHistory(Number(profile.id))
  } catch (err) {
    ordersErr.value = err.message
  } finally {
    ordersLoading.value = false
  }
})

async function onSaveProfile() {
  profileMsg.value = ''
  profileErr.value = ''
  savingProfile.value = true
  try {
    const u = await updateProfile(form.value)
    applyUser(u)
    profileMsg.value = 'Profile saved.'
    emitUpdated(u)
  } catch (err) {
    profileErr.value = err.message
  } finally {
    savingProfile.value = false
  }
}

// ---- Delivery addresses ----
function openAddAddress() {
  addrErr.value = ''
  if (!canAddAddress.value) {
    addrErr.value = `You can save up to ${MAX_ADDRESSES} addresses. Delete one to add another.`
    return
  }
  addrForm.value = { ...blankAddress(), isDefault: addresses.value.length === 0 }
  editingId.value = 0
}

function openEditAddress(a) {
  addrErr.value = ''
  addrForm.value = {
    label: a.label || '',
    addressLine1: a.addressLine1 || '',
    addressLine2: a.addressLine2 || '',
    city: a.city || '',
    state: a.state || '',
    postcode: a.postcode || '',
    deliveryNotes: a.deliveryNotes || '',
    isDefault: a.isDefault,
  }
  editingId.value = a.id
}

function cancelAddr() {
  editingId.value = null
  addrErr.value = ''
}

async function onSaveAddress() {
  if (!addrForm.value.addressLine1.trim()) {
    addrErr.value = 'Address line 1 is required.'
    return
  }
  savingAddr.value = true
  addrErr.value = ''
  try {
    const u = editingId.value
      ? await updateAddress(editingId.value, addrForm.value)
      : await addAddress(addrForm.value)
    applyUser(u)
    emitUpdated(u)
    editingId.value = null
  } catch (err) {
    addrErr.value = err.message
  } finally {
    savingAddr.value = false
  }
}

async function onDeleteAddress(id) {
  try {
    const u = await deleteAddress(id)
    applyUser(u)
    emitUpdated(u)
    if (editingId.value === id) editingId.value = null
  } catch (err) {
    addrErr.value = err.message
  }
}

async function onSetDefault(id) {
  try {
    const u = await setDefaultAddress(id)
    applyUser(u)
    emitUpdated(u)
  } catch (err) {
    addrErr.value = err.message
  }
}

async function onAvatarChange(event) {
  const file = event.target.files?.[0]
  if (!file) return
  avatarErr.value = ''
  uploadingAvatar.value = true
  try {
    const u = await uploadAvatar(file)
    applyUser(u)
    emitUpdated(u)
  } catch (err) {
    avatarErr.value = err.message
  } finally {
    uploadingAvatar.value = false
    event.target.value = '' // allow re-selecting the same file
  }
}

async function onChangePassword() {
  pwMsg.value = ''
  pwErr.value = ''
  if (pw.value.next.length < 8) {
    pwErr.value = 'New password must be at least 8 characters.'
    return
  }
  if (pw.value.next !== pw.value.confirm) {
    pwErr.value = 'New passwords do not match.'
    return
  }
  changingPw.value = true
  try {
    await changePassword({ currentPassword: pw.value.current, newPassword: pw.value.next })
    pwMsg.value = 'Password changed.'
    pw.value = { current: '', next: '', confirm: '' }
  } catch (err) {
    pwErr.value = err.message
  } finally {
    changingPw.value = false
  }
}

function onLogout() {
  logout()
  router.push('/')
}
</script>

<template>
  <div class="account">
    <h1 class="page-title">My Account</h1>
    <p v-if="loading" class="state">Loading…</p>
    <p v-else-if="loadError" class="state state-error">{{ loadError }}</p>

    <div v-else class="grid">
      <!-- Left: profile summary -->
      <aside class="card summary">
        <div class="avatar-wrap" :class="{ 'is-uploading': uploadingAvatar }">
          <img v-if="avatarSrc" :src="avatarSrc" alt="Avatar" class="avatar" />
          <span v-else class="avatar avatar-fallback">{{ initial }}</span>

          <!-- Edit affordance on the picture itself: a corner badge that opens the
               file picker. Reuses the existing hidden input + onAvatarChange flow. -->
          <label class="avatar-edit" :class="{ disabled: uploadingAvatar }"
                 :aria-label="uploadingAvatar ? 'Uploading photo' : 'Change profile photo'"
                 :title="uploadingAvatar ? 'Uploading…' : 'Change photo'">
            <span v-if="uploadingAvatar" class="avatar-spinner" aria-hidden="true"></span>
            <svg v-else class="avatar-cam" viewBox="0 0 24 24" width="16" height="16"
                 fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z" />
              <circle cx="12" cy="13" r="4" />
            </svg>
            <input type="file" accept="image/png,image/jpeg,image/webp" hidden
                   :disabled="uploadingAvatar" @change="onAvatarChange" />
          </label>
        </div>

        <h2 class="sum-name">{{ form.fullName || 'Your account' }}</h2>
        <p class="sum-email">{{ email }}</p>
        <div v-if="roles.length" class="roles">
          <span v-for="r in roles" :key="r" class="role-pill">{{ r }}</span>
        </div>

        <p v-if="avatarErr" class="field-error center">{{ avatarErr }}</p>
        <p class="hint">PNG, JPEG or WebP · up to 10 MB</p>

        <button type="button" class="btn btn-ghost btn-block logout" @click="onLogout">
          Log out
        </button>
      </aside>

      <!-- Middle: details + delivery addresses -->
      <div class="col-stack">
        <form class="card" @submit.prevent="onSaveProfile">
          <h3 class="card-title">Details</h3>
          <div class="fields">
            <div class="row-2">
              <FormField v-model="form.fullName" label="Full name" autocomplete="name" />
              <FormField v-model="form.phone" label="Phone" type="tel" autocomplete="tel"
                         placeholder="+60 12-345 6789" />
            </div>
          </div>
          <p v-if="profileErr" class="notice notice-error">{{ profileErr }}</p>
          <p v-else-if="profileMsg" class="notice notice-success">{{ profileMsg }}</p>
          <button class="btn btn-primary btn-block" type="submit" :disabled="savingProfile">
            {{ savingProfile ? 'Saving…' : 'Save changes' }}
          </button>
        </form>

        <section class="card">
          <div class="card-head">
            <h3 class="card-title">Delivery addresses</h3>
            <button
              v-if="!showAddrForm"
              type="button"
              class="btn btn-ghost btn-sm"
              :disabled="!canAddAddress"
              :title="canAddAddress ? '' : 'Maximum of 3 saved addresses reached'"
              @click="openAddAddress"
            >
              + Add address
            </button>
          </div>

          <p v-if="!addresses.length && !showAddrForm" class="hint">
            No addresses yet. Add one to start ordering.
          </p>

          <ul v-if="addresses.length" class="addr-list">
            <li v-for="a in addresses" :key="a.id" class="addr-card" :class="{ 'is-default': a.isDefault }">
              <label class="addr-default" :title="a.isDefault ? 'Default address' : 'Set as default'">
                <input type="radio" name="default-address" :checked="a.isDefault"
                       :disabled="a.isDefault" @change="onSetDefault(a.id)" />
                <span>{{ a.isDefault ? 'Default' : 'Set default' }}</span>
              </label>
              <div class="addr-body">
                <p v-if="a.label" class="addr-label">{{ a.label }}</p>
                <p class="addr-line">
                  {{ a.addressLine1 }}<template v-if="a.addressLine2">, {{ a.addressLine2 }}</template>
                </p>
                <p class="addr-line muted">{{ [a.city, a.state, a.postcode].filter(Boolean).join(', ') }}</p>
                <p v-if="a.deliveryNotes" class="addr-line muted">📝 {{ a.deliveryNotes }}</p>
              </div>
              <div class="addr-actions">
                <button type="button" class="linklike" @click="openEditAddress(a)">Edit</button>
                <button type="button" class="linklike danger" @click="onDeleteAddress(a.id)">Delete</button>
              </div>
            </li>
          </ul>

          <form v-if="showAddrForm" class="addr-form" @submit.prevent="onSaveAddress">
            <FormField v-model="addrForm.label" label="Label (optional)" placeholder="Home, Office…" />
            <FormField v-model="addrForm.addressLine1" label="Address line 1" placeholder="Street / building" />
            <FormField v-model="addrForm.addressLine2" label="Address line 2" placeholder="Unit / floor (optional)" />
            <div class="row-3">
              <FormField v-model="addrForm.city" label="City" />
              <FormField v-model="addrForm.state" label="State" />
              <FormField v-model="addrForm.postcode" label="Postcode" />
            </div>
            <label class="field">
              <span class="field-label">Delivery notes</span>
              <textarea v-model="addrForm.deliveryNotes" class="field-input" rows="2"
                        placeholder="e.g. Leave at the guardhouse" />
            </label>
            <label class="check">
              <input type="checkbox" v-model="addrForm.isDefault"
                     :disabled="editingId === 0 && addresses.length === 0" />
              <span>Set as default delivery address</span>
            </label>
            <p v-if="addrErr" class="notice notice-error">{{ addrErr }}</p>
            <div class="addr-form-actions">
              <button type="button" class="btn btn-ghost" @click="cancelAddr">Cancel</button>
              <button type="submit" class="btn btn-primary" :disabled="savingAddr">
                {{ savingAddr ? 'Saving…' : (editingId ? 'Save address' : 'Add address') }}
              </button>
            </div>
          </form>

          <p v-if="addrErr && !showAddrForm" class="notice notice-error">{{ addrErr }}</p>
        </section>
      </div>

      <!-- Right: security -->
      <form class="card" @submit.prevent="onChangePassword">
        <h3 class="card-title">Security</h3>
        <div class="fields">
          <FormField v-model="pw.current" label="Current password" type="password"
                     autocomplete="current-password" />
          <FormField v-model="pw.next" label="New password" type="password"
                     autocomplete="new-password" placeholder="At least 8 characters" />
          <FormField v-model="pw.confirm" label="Confirm new password" type="password"
                     autocomplete="new-password" />
        </div>
        <p v-if="pwErr" class="notice notice-error">{{ pwErr }}</p>
        <p v-else-if="pwMsg" class="notice notice-success">{{ pwMsg }}</p>
        <button class="btn btn-primary btn-block" type="submit" :disabled="changingPw">
          {{ changingPw ? 'Updating…' : 'Update password' }}
        </button>
      </form>

      <!-- Full-width: order history -->
      <section class="card orders-card">
        <h3 class="card-title">Order history</h3>

        <p v-if="ordersLoading" class="hint">Loading orders…</p>
        <p v-else-if="ordersErr" class="notice notice-error">{{ ordersErr }}</p>
        <p v-else-if="!orders.length" class="hint">No orders yet. Your past orders will appear here.</p>

        <ul v-else class="order-list">
          <li v-for="o in orders" :key="o.id" class="order-card">
            <div class="order-head">
              <span class="order-ref">{{ o.reference || ('#' + o.id) }}</span>
              <span class="order-date">{{ orderDate(o.placedAt) }}</span>
              <div class="order-statuses">
                <span v-if="o.paymentStatus !== 'Paid'" class="status-group">
                  <span class="status-caption">Payment</span>
                  <span class="badge" :class="paymentClass(o.paymentStatus)">{{ paymentLabel(o.paymentStatus) }}</span>
                </span>
                <span v-if="o.paymentStatus === 'Paid'" class="status-group">
                  <span class="status-caption">Order</span>
                  <span class="badge" :class="statusClass(o.status)">{{ statusLabel(o.status) }}</span>
                </span>
              </div>
            </div>
            <ul class="order-items">
              <li v-for="(it, i) in o.items" :key="i" class="order-item">
                <span class="oi-name">{{ it.name }} <span class="oi-qty">×{{ it.quantity }}</span></span>
                <span class="oi-price">{{ money(it.unitPrice * it.quantity, o.currency) }}</span>
              </li>
            </ul>
            <div class="order-foot">
              <span>Total</span>
              <span class="order-total">{{ money(o.total, o.currency) }}</span>
            </div>
            <div v-if="canPay(o)" class="order-actions">
              <RouterLink class="btn btn-primary btn-pay" :to="{ name: 'payment', params: { orderId: o.id } }">
                Pay now
              </RouterLink>
            </div>
          </li>
        </ul>
      </section>
    </div>
  </div>
</template>

<style scoped>
.account {
  max-width: 1120px;
  margin: 0 auto;
  padding: 24px;
}
.page-title { margin: 0 0 20px; font-size: 1.4rem; font-weight: 800; color: var(--color-ink); }
.state { text-align: center; padding: 48px 0; color: var(--color-muted); }
.state-error { color: var(--color-danger); }

.grid {
  display: grid;
  grid-template-columns: 280px minmax(0, 1fr) 320px;
  gap: 20px;
  align-items: stretch;
}

.card {
  background: var(--color-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius);
  box-shadow: var(--shadow-sm);
  padding: 18px;
  display: flex;
  flex-direction: column;
}
.card-title { margin: 0 0 14px; font-size: 1rem; font-weight: 700; color: var(--color-ink); }

/* Left summary */
.summary { align-items: center; text-align: center; }
.avatar {
  width: 96px; height: 96px; border-radius: 50%;
  object-fit: cover; border: 1px solid var(--color-border);
  display: inline-flex; align-items: center; justify-content: center;
}
.avatar-fallback {
  background: var(--color-primary-soft); color: var(--color-primary);
  font-weight: 800; font-size: 2.2rem;
}
.sum-name { margin: 14px 0 2px; font-size: 1.1rem; font-weight: 800; color: var(--color-ink); }
.sum-email { margin: 0 0 10px; font-size: 0.85rem; color: var(--color-muted); word-break: break-all; }
.roles { display: flex; flex-wrap: wrap; gap: 6px; justify-content: center; margin-bottom: 16px; }
.role-pill {
  font-size: 0.72rem; font-weight: 700; text-transform: capitalize;
  color: var(--color-primary); background: var(--color-primary-soft);
  border-radius: 999px; padding: 3px 10px;
}
.avatar-wrap { position: relative; display: inline-block; }
.avatar-wrap.is-uploading .avatar { opacity: 0.6; }
.avatar-edit {
  position: absolute; bottom: 4px; right: 4px;
  width: 32px; height: 32px; border-radius: 50%;
  display: inline-flex; align-items: center; justify-content: center;
  background: var(--color-primary); color: #fff;
  border: 2px solid var(--color-bg);
  box-shadow: var(--shadow-sm); cursor: pointer;
  transition: transform 0.15s ease, filter 0.15s ease, box-shadow 0.15s ease;
}
.avatar-edit:hover { transform: translateY(-1px); filter: brightness(0.93); }
.avatar-edit.disabled { cursor: not-allowed; opacity: 0.6; pointer-events: none; }
.avatar-cam { display: block; }
.avatar-spinner {
  width: 14px; height: 14px; border-radius: 50%;
  border: 2px solid rgba(255, 255, 255, 0.5); border-top-color: #fff;
  animation: avatar-spin 0.6s linear infinite;
}
@keyframes avatar-spin { to { transform: rotate(360deg); } }
.hint { margin: 6px 0 0; font-size: 0.76rem; color: var(--color-muted); }
.logout { margin-top: auto; }

/* Forms */
.fields { display: flex; flex-direction: column; gap: 10px; margin-bottom: 14px; }
.row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
.row-3 { display: grid; grid-template-columns: 1fr 1fr 1fr; gap: 10px; }

.field { display: flex; flex-direction: column; gap: 5px; }
.field-label { font-size: 0.82rem; font-weight: 600; color: var(--color-ink); }
.field-input {
  font-family: inherit; font-size: 0.95rem; color: var(--color-ink);
  padding: 9px 12px; border: 1px solid var(--color-border);
  border-radius: var(--radius-sm); background: #fff;
}
.field-input:focus { outline: none; border-color: var(--color-primary); box-shadow: 0 0 0 3px var(--color-primary-soft); }
textarea.field-input { resize: vertical; }
.field-error { font-size: 0.8rem; color: var(--color-danger); margin: 4px 0 0; }
.field-error.center { text-align: center; }

.notice { padding: 9px 12px; border-radius: var(--radius-sm); font-size: 0.85rem; margin: 0 0 12px; }
.notice-error { background: #fef2f2; color: var(--color-danger); border: 1px solid #fecaca; }
.notice-success { background: #f0fdf4; color: var(--color-success); border: 1px solid #bbf7d0; }

/* Save / Update buttons sit at the bottom of their card */
.card .btn-primary { margin-top: auto; }

/* Middle column stacks the Details and Delivery-addresses cards */
.col-stack { display: flex; flex-direction: column; gap: 20px; min-width: 0; }

.card-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-bottom: 14px; }
.card-head .card-title { margin: 0; }
.btn-sm { padding: 6px 12px; font-size: 0.82rem; }

/* Address list */
.addr-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 10px; }
.addr-card {
  display: grid;
  grid-template-columns: 1fr auto;
  grid-template-areas: "default actions" "body body";
  gap: 6px 12px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  padding: 12px 14px;
}
.addr-card.is-default { border-color: var(--color-primary); background: var(--color-primary-soft); }
.addr-default {
  grid-area: default; display: inline-flex; align-items: center; gap: 6px;
  font-size: 0.8rem; font-weight: 600; color: var(--color-primary); cursor: pointer;
}
.addr-card.is-default .addr-default input { cursor: default; }
.addr-actions { grid-area: actions; display: inline-flex; gap: 12px; align-items: center; }
.addr-body { grid-area: body; }
.addr-label { margin: 0 0 2px; font-weight: 700; color: var(--color-ink); font-size: 0.9rem; }
.addr-line { margin: 0; font-size: 0.9rem; color: var(--color-ink); }
.addr-line.muted { color: var(--color-muted); font-size: 0.84rem; }

.linklike {
  background: none; border: none; padding: 0; font: inherit; font-size: 0.82rem;
  color: var(--color-primary); cursor: pointer; text-decoration: underline;
}
.linklike.danger { color: var(--color-danger); }

/* Add / edit address form */
.addr-form { display: flex; flex-direction: column; gap: 10px; margin-top: 12px; }
.check { display: inline-flex; align-items: center; gap: 8px; font-size: 0.85rem; color: var(--color-ink); cursor: pointer; }
.addr-form-actions { display: flex; justify-content: flex-end; gap: 10px; }
.addr-form-actions .btn-primary { margin-top: 0; }

/* Order history (full-width row in the grid) */
.orders-card { grid-column: 1 / -1; }
.order-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 12px; }
.order-card { border: 1px solid var(--color-border); border-radius: var(--radius-sm); padding: 14px 16px; }
.order-head { display: flex; align-items: center; gap: 12px; margin-bottom: 10px; flex-wrap: wrap; }
.order-ref { font-weight: 700; color: var(--color-ink); }
.order-date { font-size: 0.84rem; color: var(--color-muted); }
.order-statuses { margin-left: auto; display: flex; align-items: center; gap: 14px; flex-wrap: wrap; }
.status-group { display: inline-flex; align-items: center; gap: 6px; }
.status-caption {
  font-size: 0.68rem; font-weight: 700; text-transform: uppercase;
  letter-spacing: 0.4px; color: var(--color-muted);
}
/* .badge / .badge-* are shared — see style.css */
.order-items { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 4px; }
.order-item { display: flex; justify-content: space-between; font-size: 0.9rem; color: var(--color-ink); }
.oi-qty { color: var(--color-muted); }
.oi-price { color: var(--color-body, var(--color-ink)); }
.order-foot {
  display: flex; justify-content: space-between; align-items: center;
  margin-top: 10px; padding-top: 10px; border-top: 1px solid var(--color-border);
  font-size: 0.9rem; font-weight: 600; color: var(--color-ink);
}
.order-total { font-weight: 800; color: var(--color-primary); }
.order-actions { margin-top: 10px; display: flex; justify-content: flex-end; }
.btn-pay { display: inline-block; padding: 8px 18px; font-size: 0.9rem; text-decoration: none; }

@media (max-width: 900px) {
  .grid { grid-template-columns: 1fr; }
  .logout { margin-top: 16px; }
}
</style>
