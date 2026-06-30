<script setup>
// User management: search, page, promote/demote admin, enable/disable accounts.
// All mutations return the updated row so we patch it in place without a refetch.
// Server enforces the guards (last-admin, self-action) and surfaces them as errors.
import { ref, onMounted } from 'vue'
import { listUsers, setUserAdmin, setUserDisabled } from '../services/adminApi.js'
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
            <td class="cell-name">{{ user.fullName }}</td>
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
</style>
