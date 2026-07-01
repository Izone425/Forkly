<script setup>
// User management: search, page, promote/demote admin, enable/disable accounts.
// All mutations return the updated row so we patch it in place without a refetch.
// Server enforces the guards (last-admin, self-action) and surfaces them as errors.
import { ref, computed, onMounted } from 'vue'
import { listUsers, setUserAdmin, setUserDisabled, getUser } from '../services/adminApi.js'
import { absoluteUrl } from '../services/authApi.js'
import { useToast } from '../stores/toast.js'

const { show } = useToast()

const users = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const search = ref('')
const loading = ref(false)
const error = ref('')
const busyId = ref(null) // row currently mutating

// User-detail modal (read-only view of one user's account).
const detailOpen = ref(false)
const detail = ref(null)
const detailLoading = ref(false)
const detailError = ref('')

const avatarSrc = computed(() => absoluteUrl(detail.value?.user.avatarUrl))

function initial(person) {
  return (person?.fullName || person?.email || '?').trim().charAt(0).toUpperCase()
}

function formatDate(iso) {
  if (!iso) return '—'
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '—' : d.toLocaleDateString()
}

async function openDetail(user) {
  detailOpen.value = true
  detail.value = null
  detailError.value = ''
  detailLoading.value = true
  try {
    detail.value = await getUser(user.id)
  } catch (err) {
    detailError.value = err.message
  } finally {
    detailLoading.value = false
  }
}

function closeDetail() {
  detailOpen.value = false
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const result = await listUsers({ search: search.value, page: page.value, pageSize: pageSize.value })
    users.value = result.items
    total.value = result.total
  } catch (err) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

function onSearch() {
  page.value = 1
  load()
}

function pageCount() {
  return Math.max(1, Math.ceil(total.value / pageSize.value))
}

function goTo(next) {
  const target = Math.min(Math.max(1, next), pageCount())
  if (target === page.value) return
  page.value = target
  load()
}

function patchRow(updated) {
  const i = users.value.findIndex((u) => u.id === updated.id)
  if (i !== -1) users.value[i] = updated
}

async function togglePromote(user) {
  busyId.value = user.id
  try {
    const updated = await setUserAdmin(user.id, !user.isAdmin)
    patchRow(updated)
    show(updated.isAdmin ? `${updated.fullName} is now an admin.` : `Removed admin from ${updated.fullName}.`)
  } catch (err) {
    show(err.message)
  } finally {
    busyId.value = null
  }
}

async function toggleDisable(user) {
  busyId.value = user.id
  try {
    const updated = await setUserDisabled(user.id, !user.isDisabled)
    patchRow(updated)
    show(updated.isDisabled ? `${updated.fullName} disabled.` : `${updated.fullName} enabled.`)
  } catch (err) {
    show(err.message)
  } finally {
    busyId.value = null
  }
}

onMounted(load)
</script>

<template>
  <section class="users">
    <header class="users-head">
      <h1 class="users-title">Users</h1>
      <form class="users-search" @submit.prevent="onSearch">
        <input
          v-model="search"
          type="search"
          class="users-search-input"
          placeholder="Search name or email…"
          aria-label="Search users"
        />
        <button type="submit" class="btn btn-primary">Search</button>
      </form>
    </header>

    <p v-if="error" class="notice notice-error">{{ error }}</p>

    <div class="users-table-wrap">
      <table class="users-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Role</th>
            <th>Status</th>
            <th class="col-actions">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="5" class="users-empty">Loading…</td>
          </tr>
          <tr v-else-if="!users.length">
            <td colspan="5" class="users-empty">No users found.</td>
          </tr>
          <tr v-for="user in users" v-else :key="user.id">
            <td class="cell-name">
              <button type="button" class="link-name" @click="openDetail(user)" :title="`View ${user.fullName}`">
                {{ user.fullName }}
              </button>
            </td>
            <td class="cell-muted">{{ user.email }}</td>
            <td>
              <span class="badge" :class="user.isAdmin ? 'badge-admin' : 'badge-client'">
                {{ user.isAdmin ? 'Admin' : 'Client' }}
              </span>
            </td>
            <td>
              <span class="badge" :class="user.isDisabled ? 'badge-off' : 'badge-on'">
                {{ user.isDisabled ? 'Disabled' : 'Active' }}
              </span>
            </td>
            <td class="col-actions">
              <button
                type="button"
                class="row-btn"
                :disabled="busyId === user.id"
                @click="togglePromote(user)"
              >
                {{ user.isAdmin ? 'Revoke admin' : 'Make admin' }}
              </button>
              <button
                type="button"
                class="row-btn"
                :class="{ danger: !user.isDisabled }"
                :disabled="busyId === user.id"
                @click="toggleDisable(user)"
              >
                {{ user.isDisabled ? 'Enable' : 'Disable' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <footer class="users-foot">
      <span class="users-count">{{ total }} user{{ total === 1 ? '' : 's' }}</span>
      <div class="users-pager">
        <button type="button" class="row-btn" :disabled="page <= 1 || loading" @click="goTo(page - 1)">
          Prev
        </button>
        <span class="users-page">Page {{ page }} / {{ pageCount() }}</span>
        <button type="button" class="row-btn" :disabled="page >= pageCount() || loading" @click="goTo(page + 1)">
          Next
        </button>
      </div>
    </footer>

    <!-- Read-only user detail -->
    <div v-if="detailOpen" class="modal-root" @click.self="closeDetail">
      <div class="modal-panel" role="dialog" aria-modal="true" aria-label="User details">
        <header class="modal-head">
          <h2 class="modal-title">User details</h2>
          <button type="button" class="modal-close" @click="closeDetail">×</button>
        </header>

        <div class="detail-body">
          <p v-if="detailLoading" class="detail-status">Loading…</p>
          <p v-else-if="detailError" class="notice notice-error">{{ detailError }}</p>

          <template v-else-if="detail">
            <div class="detail-summary">
              <img v-if="avatarSrc" :src="avatarSrc" :alt="detail.user.fullName" class="detail-avatar" />
              <span v-else class="detail-avatar detail-avatar-fallback" aria-hidden="true">{{ initial(detail.user) }}</span>
              <div class="detail-id">
                <p class="detail-name">{{ detail.user.fullName }}</p>
                <p class="detail-email">{{ detail.user.email }}</p>
                <div class="detail-badges">
                  <span class="badge" :class="detail.isAdmin ? 'badge-admin' : 'badge-client'">
                    {{ detail.isAdmin ? 'Admin' : 'Client' }}
                  </span>
                  <span class="badge" :class="detail.isDisabled ? 'badge-off' : 'badge-on'">
                    {{ detail.isDisabled ? 'Disabled' : 'Active' }}
                  </span>
                </div>
              </div>
            </div>

            <dl class="detail-meta">
              <div class="detail-row">
                <dt>Phone</dt>
                <dd>{{ detail.user.phone || '—' }}</dd>
              </div>
              <div class="detail-row">
                <dt>Member since</dt>
                <dd>{{ formatDate(detail.createdAt) }}</dd>
              </div>
            </dl>

            <h3 class="detail-subhead">Delivery addresses</h3>
            <p v-if="!detail.user.addresses.length" class="detail-status">No addresses saved.</p>
            <ul v-else class="addr-list">
              <li v-for="a in detail.user.addresses" :key="a.id" class="addr-card" :class="{ 'is-default': a.isDefault }">
                <div class="addr-body">
                  <p class="addr-label">
                    <span v-if="a.label">{{ a.label }}</span>
                    <span v-if="a.isDefault" class="addr-default-tag">Default</span>
                  </p>
                  <p class="addr-line">
                    {{ a.addressLine1 }}<template v-if="a.addressLine2">, {{ a.addressLine2 }}</template>
                  </p>
                  <p class="addr-line muted">{{ [a.city, a.state, a.postcode].filter(Boolean).join(', ') }}</p>
                  <p v-if="a.deliveryNotes" class="addr-line muted">📝 {{ a.deliveryNotes }}</p>
                </div>
              </li>
            </ul>
          </template>
        </div>
      </div>
    </div>
  </section>
</template>

<style scoped>
.users { max-width: 1040px; }
.users-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 18px;
  flex-wrap: wrap;
}
.users-title { margin: 0; font-size: 1.5rem; color: var(--color-ink); }
.users-search { display: inline-flex; gap: 8px; }
.users-search-input {
  font-family: inherit;
  font-size: 0.95rem;
  padding: 9px 13px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  min-width: 240px;
}
.users-search-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}

.users-table-wrap {
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: 14px;
  overflow: hidden;
  box-shadow: var(--shadow-sm);
}
.users-table { width: 100%; border-collapse: collapse; }
.users-table th,
.users-table td {
  text-align: left;
  padding: 13px 16px;
  border-bottom: 1px solid var(--color-border);
  font-size: 0.93rem;
}
.users-table th {
  font-size: 0.78rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--color-muted);
  background: var(--color-bg, #f6f7fb);
}
.users-table tbody tr:last-child td { border-bottom: none; }
.cell-name { font-weight: 700; color: var(--color-ink); }
.cell-muted { color: var(--color-muted); }
.users-empty { text-align: center; color: var(--color-muted); padding: 28px; }
.col-actions { white-space: nowrap; text-align: right; }

.badge {
  display: inline-block;
  font-size: 0.76rem;
  font-weight: 700;
  padding: 3px 10px;
  border-radius: 999px;
}
.badge-admin { color: var(--color-primary); background: var(--color-primary-soft); }
.badge-client { color: var(--color-muted); background: var(--color-bg, #eef1f6); }
.badge-on { color: #047857; background: #d1fae5; }
.badge-off { color: var(--color-danger); background: #fee2e2; }

.row-btn {
  font: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--color-body);
  background: #fff;
  border: 1px solid var(--color-border);
  padding: 7px 13px;
  border-radius: 9px;
  cursor: pointer;
  margin-left: 8px;
  transition: border-color 0.15s ease, color 0.15s ease;
}
.row-btn:hover:not(:disabled) { border-color: var(--color-primary); color: var(--color-primary); }
.row-btn.danger:hover:not(:disabled) { border-color: var(--color-danger); color: var(--color-danger); }
.row-btn:disabled { opacity: 0.5; cursor: not-allowed; }

.users-foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-top: 16px;
}
.users-count { color: var(--color-muted); font-size: 0.9rem; }
.users-pager { display: inline-flex; align-items: center; gap: 12px; }
.users-page { font-size: 0.9rem; color: var(--color-body); }

.notice { padding: 10px 14px; border-radius: var(--radius-sm); font-size: 0.88rem; margin: 0 0 16px; }
.notice-error { background: #fef2f2; color: var(--color-danger); border: 1px solid #fecaca; }

/* Clickable username (button styled as a link). */
.link-name {
  font: inherit;
  font-weight: 700;
  color: var(--color-ink);
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  text-align: left;
}
.link-name:hover { color: var(--color-primary); text-decoration: underline; }
.link-name:focus-visible { outline: 2px solid var(--color-primary); outline-offset: 2px; border-radius: 2px; }

/* User-detail modal (mirrors AdminMenu.vue). */
.modal-root {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, 0.45);
  display: grid;
  place-items: center;
  padding: 20px;
  z-index: 50;
}
.modal-panel {
  background: #fff;
  border-radius: 16px;
  width: 100%;
  max-width: 520px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: var(--shadow-lg);
}
.modal-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18px 20px;
  border-bottom: 1px solid var(--color-border);
}
.modal-title { margin: 0; font-size: 1.15rem; color: var(--color-ink); }
.modal-close {
  border: none;
  background: none;
  font-size: 1.5rem;
  line-height: 1;
  color: var(--color-muted);
  cursor: pointer;
}

.detail-body { padding: 18px 20px 22px; }
.detail-status { color: var(--color-muted); font-size: 0.9rem; margin: 4px 0; }

.detail-summary { display: flex; align-items: center; gap: 14px; margin-bottom: 18px; }
.detail-avatar {
  width: 64px;
  height: 64px;
  border-radius: 50%;
  object-fit: cover;
  flex-shrink: 0;
}
.detail-avatar-fallback {
  display: grid;
  place-items: center;
  font-size: 1.6rem;
  font-weight: 700;
  color: var(--color-primary);
  background: var(--color-primary-soft);
}
.detail-id { min-width: 0; }
.detail-name { margin: 0; font-size: 1.1rem; font-weight: 700; color: var(--color-ink); }
.detail-email { margin: 2px 0 8px; color: var(--color-muted); font-size: 0.9rem; word-break: break-all; }
.detail-badges { display: flex; gap: 8px; flex-wrap: wrap; }

.detail-meta {
  margin: 0 0 20px;
  display: grid;
  gap: 10px;
  border-top: 1px solid var(--color-border);
  padding-top: 16px;
}
.detail-row { display: flex; justify-content: space-between; gap: 16px; font-size: 0.92rem; }
.detail-row dt { color: var(--color-muted); margin: 0; }
.detail-row dd { margin: 0; color: var(--color-body); font-weight: 600; text-align: right; }

.detail-subhead {
  margin: 0 0 10px;
  font-size: 0.82rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--color-muted);
}
.addr-list { list-style: none; margin: 0; padding: 0; display: grid; gap: 10px; }
.addr-card {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  padding: 12px 14px;
}
.addr-card.is-default { border-color: var(--color-primary); background: var(--color-primary-soft); }
.addr-body { min-width: 0; }
.addr-label { margin: 0 0 4px; font-weight: 700; color: var(--color-ink); display: flex; align-items: center; gap: 8px; }
.addr-default-tag {
  font-size: 0.72rem;
  font-weight: 700;
  color: var(--color-primary);
  background: #fff;
  border: 1px solid var(--color-primary);
  border-radius: 999px;
  padding: 1px 8px;
}
.addr-line { margin: 2px 0; font-size: 0.9rem; color: var(--color-body); }
.addr-line.muted { color: var(--color-muted); }
</style>
