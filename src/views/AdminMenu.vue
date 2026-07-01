<script setup>
// Menu management (Menu Service — Amirul). Full CRUD against Forkly.MenuService:
// list, create, edit, delete, and toggle availability. Buyer reads are public; the
// write actions here require an admin JWT (enforced server-side). When the Menu
// service isn't configured (VITE_MENU_API_BASE unset) the screen degrades to a
// read-only view of the bundled sample menu so it still renders.
import { ref, computed, onMounted } from 'vue'
import {
  isMenuApiConfigured,
  fetchAdminMenu,
  getCategories,
  createMenuItem,
  updateMenuItem,
  deleteMenuItem,
  setMenuItemAvailability,
  uploadMenuImage,
  menuImageUrl,
} from '../services/menuApi.js'
import { MENU as SAMPLE_MENU } from '../data/menu.js'
import { useToast } from '../stores/toast.js'

const { show } = useToast()

const items = ref([])
const categories = ref([])
const loading = ref(false)
const error = ref('')
const busyId = ref(null) // row currently mutating
const isLive = ref(false)

const configured = isMenuApiConfigured()

// Modal state
const modalOpen = ref(false)
const saving = ref(false)
const formError = ref('')
const editingId = ref(null)
const form = ref(blankForm())

// Picture upload state (stored as bytes in the DB, uploaded after the item is saved).
const imageFile = ref(null)       // the newly-selected File, if any
const imagePreview = ref('')      // object URL for the selected File
const existingImageUrl = ref('')  // absolutized current image, when editing

function blankForm() {
  return {
    categoryId: '',
    name: '',
    description: '',
    unitPrice: '',
    stockQuantity: 0,
    availability: true,
  }
}

// Preview the selected file if there is one, otherwise the item's current picture.
const previewSrc = computed(() => imagePreview.value || existingImageUrl.value)

function resetImageInput() {
  if (imagePreview.value) URL.revokeObjectURL(imagePreview.value)
  imageFile.value = null
  imagePreview.value = ''
}

function onImageChange(event) {
  const file = event.target.files?.[0]
  if (!file) return
  if (file.size > 10 * 1024 * 1024) {
    formError.value = 'Image must be 10 MB or smaller.'
    event.target.value = ''
    return
  }
  if (imagePreview.value) URL.revokeObjectURL(imagePreview.value)
  imageFile.value = file
  imagePreview.value = URL.createObjectURL(file)
  formError.value = ''
}

const modalTitle = computed(() => (editingId.value ? 'Edit menu item' : 'Add menu item'))

async function load() {
  if (!configured) {
    // Graceful fallback: show the bundled sample menu, read-only.
    items.value = SAMPLE_MENU.map((m) => ({
      id: m.id,
      category: m.category ?? 'Menu',
      name: m.name,
      description: m.description,
      unitPrice: m.price,
      imageUrl: m.image ?? null,
      stockQuantity: null,
      availability: true,
    }))
    isLive.value = false
    return
  }
  loading.value = true
  error.value = ''
  try {
    const [menu, cats] = await Promise.all([fetchAdminMenu(), getCategories()])
    items.value = menu
    categories.value = cats
    isLive.value = true
  } catch (err) {
    error.value = err.message
    items.value = []
    isLive.value = false
  } finally {
    loading.value = false
  }
}

function money(value) {
  return `RM ${Number(value).toFixed(2)}`
}

function openCreate() {
  editingId.value = null
  form.value = blankForm()
  if (categories.value.length) form.value.categoryId = categories.value[0].id
  resetImageInput()
  existingImageUrl.value = ''
  formError.value = ''
  modalOpen.value = true
}

function openEdit(item) {
  editingId.value = item.id
  form.value = {
    categoryId: item.categoryId ?? '',
    name: item.name,
    description: item.description ?? '',
    unitPrice: item.unitPrice,
    stockQuantity: item.stockQuantity ?? 0,
    availability: item.availability,
  }
  resetImageInput()
  existingImageUrl.value = menuImageUrl(item.imageUrl) ?? ''
  formError.value = ''
  modalOpen.value = true
}

function closeModal() {
  if (saving.value) return
  resetImageInput()
  modalOpen.value = false
}

function patchRow(updated) {
  const i = items.value.findIndex((it) => it.id === updated.id)
  if (i !== -1) items.value[i] = updated
}

async function save() {
  formError.value = ''
  if (!form.value.name.trim()) {
    formError.value = 'Name is required.'
    return
  }
  if (!form.value.categoryId) {
    formError.value = 'Please choose a category.'
    return
  }
  const payload = {
    categoryId: Number(form.value.categoryId),
    name: form.value.name.trim(),
    description: form.value.description.trim(),
    unitPrice: Number(form.value.unitPrice) || 0,
    stockQuantity: Number(form.value.stockQuantity) || 0,
    availability: Boolean(form.value.availability),
  }

  saving.value = true
  try {
    const editing = editingId.value
    // Save the item first, then (if a picture was chosen) upload it to the DB. The
    // upload returns the fully-updated DTO, so we use whichever result is latest.
    let saved = editing
      ? await updateMenuItem(editing, payload)
      : await createMenuItem(payload)
    if (imageFile.value) {
      saved = await uploadMenuImage(saved.id, imageFile.value)
    }

    if (editing) {
      patchRow(saved)
      show(`Updated “${saved.name}”.`)
    } else {
      items.value.unshift(saved)
      show(`Added “${saved.name}”.`)
    }
    resetImageInput()
    modalOpen.value = false
  } catch (err) {
    formError.value = err.message
  } finally {
    saving.value = false
  }
}

async function toggleAvailability(item) {
  busyId.value = item.id
  try {
    const updated = await setMenuItemAvailability(item.id, !item.availability)
    patchRow(updated)
    show(updated.availability ? `“${updated.name}” is now available.` : `“${updated.name}” hidden from menu.`)
  } catch (err) {
    show(err.message)
  } finally {
    busyId.value = null
  }
}

async function remove(item) {
  if (!window.confirm(`Delete “${item.name}”? This cannot be undone.`)) return
  busyId.value = item.id
  try {
    await deleteMenuItem(item.id)
    items.value = items.value.filter((it) => it.id !== item.id)
    show(`Deleted “${item.name}”.`)
  } catch (err) {
    show(err.message)
  } finally {
    busyId.value = null
  }
}

onMounted(load)
</script>

<template>
  <section class="menu">
    <header class="menu-head">
      <div class="menu-head-l">
        <h1 class="menu-title">Menu</h1>
        <span class="menu-source" :class="isLive ? 'live' : 'mock'">
          {{ isLive ? 'Live · Menu service' : 'Sample data' }}
        </span>
      </div>
      <button v-if="configured" type="button" class="btn btn-primary" @click="openCreate">
        + Add item
      </button>
    </header>

    <p v-if="!configured" class="notice notice-info">
      Menu service not connected (set <code>VITE_MENU_API_BASE</code>). Showing the bundled sample
      menu read-only.
    </p>
    <p v-else-if="error" class="notice notice-error">
      Couldn’t reach the Menu service ({{ error }}).
    </p>

    <div class="menu-table-wrap">
      <table class="menu-table">
        <thead>
          <tr>
            <th class="col-img"></th>
            <th>Name</th>
            <th>Category</th>
            <th class="col-num">Price</th>
            <th class="col-num">Stock</th>
            <th>Status</th>
            <th class="col-actions">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="loading">
            <td colspan="7" class="menu-empty">Loading…</td>
          </tr>
          <tr v-else-if="!items.length">
            <td colspan="7" class="menu-empty">No menu items yet.</td>
          </tr>
          <tr v-for="item in items" v-else :key="item.id">
            <td class="col-img">
              <img v-if="item.imageUrl" :src="menuImageUrl(item.imageUrl)" :alt="item.name" class="menu-thumb" />
              <span v-else class="menu-thumb menu-thumb-empty" aria-hidden="true">🍽</span>
            </td>
            <td>
              <span class="cell-name">{{ item.name }}</span>
              <span class="cell-desc">{{ item.description }}</span>
            </td>
            <td class="cell-muted">{{ item.category || '—' }}</td>
            <td class="col-num">{{ money(item.unitPrice) }}</td>
            <td class="col-num">{{ item.stockQuantity ?? '—' }}</td>
            <td>
              <span class="badge" :class="item.availability ? 'badge-on' : 'badge-off'">
                {{ item.availability ? 'Available' : 'Hidden' }}
              </span>
            </td>
            <td class="col-actions">
              <template v-if="configured">
                <button type="button" class="row-btn" :disabled="busyId === item.id" @click="openEdit(item)">
                  Edit
                </button>
                <button type="button" class="row-btn" :disabled="busyId === item.id" @click="toggleAvailability(item)">
                  {{ item.availability ? 'Hide' : 'Show' }}
                </button>
                <button type="button" class="row-btn danger" :disabled="busyId === item.id" @click="remove(item)">
                  Delete
                </button>
              </template>
              <button v-else type="button" class="row-btn" disabled title="Connect the Menu service to edit">
                Edit
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <footer class="menu-foot">
      <span class="menu-count">{{ items.length }} item{{ items.length === 1 ? '' : 's' }}</span>
    </footer>

    <!-- Create / edit modal -->
    <div v-if="modalOpen" class="modal-root" @click.self="closeModal">
      <div class="modal-panel" role="dialog" aria-modal="true">
        <header class="modal-head">
          <h2 class="modal-title">{{ modalTitle }}</h2>
          <button type="button" class="modal-close" :disabled="saving" @click="closeModal">×</button>
        </header>

        <form class="modal-form" @submit.prevent="save">
          <label class="field">
            <span class="field-label">Category</span>
            <select v-model="form.categoryId" class="field-input" required>
              <option value="" disabled>Choose a category…</option>
              <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
          </label>

          <label class="field">
            <span class="field-label">Name</span>
            <input v-model="form.name" type="text" class="field-input" maxlength="200" required />
          </label>

          <label class="field">
            <span class="field-label">Description</span>
            <textarea v-model="form.description" class="field-input" rows="2" maxlength="1000"></textarea>
          </label>

          <div class="field-row">
            <label class="field">
              <span class="field-label">Unit price (RM)</span>
              <input v-model="form.unitPrice" type="number" min="0" step="0.01" class="field-input" required />
            </label>
            <label class="field">
              <span class="field-label">Stock quantity</span>
              <input v-model="form.stockQuantity" type="number" min="0" step="1" class="field-input" />
            </label>
          </div>

          <label class="field">
            <span class="field-label">Picture</span>
            <input
              type="file"
              class="field-input"
              accept="image/png,image/jpeg,image/webp"
              @change="onImageChange"
            />
            <span class="field-hint">PNG, JPEG or WebP, up to 10 MB. Stored in the database.</span>
          </label>

          <div v-if="previewSrc" class="img-preview">
            <img :src="previewSrc" alt="Preview" />
          </div>

          <label class="field-check">
            <input v-model="form.availability" type="checkbox" />
            <span>Available to buyers</span>
          </label>

          <p v-if="formError" class="notice notice-error">{{ formError }}</p>

          <div class="modal-actions">
            <button type="button" class="btn btn-ghost" :disabled="saving" @click="closeModal">Cancel</button>
            <button type="submit" class="btn btn-primary" :disabled="saving">
              {{ saving ? 'Saving…' : editingId ? 'Save changes' : 'Create item' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </section>
</template>

<style scoped>
.menu { max-width: 1100px; }
.menu-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 18px;
  flex-wrap: wrap;
}
.menu-head-l { display: flex; align-items: center; gap: 14px; }
.menu-title { margin: 0; font-size: 1.5rem; color: var(--color-ink); }
.menu-source { font-size: 0.76rem; font-weight: 700; padding: 3px 10px; border-radius: 999px; }
.menu-source.live { color: #047857; background: #d1fae5; }
.menu-source.mock { color: var(--color-muted); background: var(--color-bg, #eef1f6); }

.menu-table-wrap {
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: 14px;
  overflow: hidden;
  box-shadow: var(--shadow-sm);
}
.menu-table { width: 100%; border-collapse: collapse; }
.menu-table th,
.menu-table td {
  text-align: left;
  padding: 12px 16px;
  border-bottom: 1px solid var(--color-border);
  font-size: 0.93rem;
  vertical-align: middle;
}
.menu-table th {
  font-size: 0.78rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--color-muted);
  background: var(--color-bg, #f6f7fb);
}
.menu-table tbody tr:last-child td { border-bottom: none; }
.col-img { width: 56px; }
.col-num { text-align: right; white-space: nowrap; }
.col-actions { white-space: nowrap; text-align: right; }

.menu-thumb {
  width: 44px;
  height: 44px;
  border-radius: 10px;
  object-fit: cover;
  display: block;
  background: var(--color-primary-soft);
}
.menu-thumb-empty { display: grid; place-items: center; font-size: 1.3rem; }

.cell-name { display: block; font-weight: 700; color: var(--color-ink); }
.cell-desc {
  display: block;
  font-size: 0.82rem;
  color: var(--color-muted);
  max-width: 360px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.cell-muted { color: var(--color-muted); }
.menu-empty { text-align: center; color: var(--color-muted); padding: 28px; }

.badge { display: inline-block; font-size: 0.76rem; font-weight: 700; padding: 3px 10px; border-radius: 999px; }
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

.menu-foot { display: flex; align-items: center; justify-content: space-between; margin-top: 16px; }
.menu-count { color: var(--color-muted); font-size: 0.9rem; }

.notice { padding: 10px 14px; border-radius: var(--radius-sm); font-size: 0.88rem; margin: 0 0 16px; }
.notice-error { background: #fef2f2; color: var(--color-danger); border: 1px solid #fecaca; }
.notice-info { background: #eff6ff; color: #1d4ed8; border: 1px solid #bfdbfe; }
.notice-info code { font-family: ui-monospace, monospace; font-size: 0.85em; }

/* Modal */
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
.modal-close:disabled { opacity: 0.5; cursor: not-allowed; }

.modal-form { padding: 18px 20px 22px; display: flex; flex-direction: column; gap: 14px; }
.field { display: flex; flex-direction: column; gap: 6px; }
.field-row { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.field-label { font-size: 0.82rem; font-weight: 700; color: var(--color-body); }
.field-hint { font-size: 0.78rem; color: var(--color-muted); }
.field-input {
  font-family: inherit;
  font-size: 0.95rem;
  padding: 9px 12px;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  width: 100%;
}
.field-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-soft);
}
textarea.field-input { resize: vertical; }
.field-check { display: inline-flex; align-items: center; gap: 8px; font-size: 0.9rem; color: var(--color-body); }

.img-preview {
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  overflow: hidden;
  max-height: 160px;
}
.img-preview img { width: 100%; height: 160px; object-fit: cover; display: block; }

.modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 4px; }
.btn-ghost {
  background: #fff;
  color: var(--color-body);
  border: 1px solid var(--color-border);
  padding: 9px 16px;
  border-radius: 12px;
  font-weight: 600;
  cursor: pointer;
}
.btn-ghost:disabled { opacity: 0.5; cursor: not-allowed; }

@media (max-width: 720px) {
  .cell-desc { max-width: 160px; }
  .field-row { grid-template-columns: 1fr; }
}
</style>
