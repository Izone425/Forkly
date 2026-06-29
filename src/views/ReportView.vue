<script setup>
import { ref, computed, onMounted } from 'vue'
import { getMonthlySalesReport } from '../services/reportApi.js'

const report = ref(null)
const selectedMonth = ref(null)
const loading = ref(true)
const usingMock = ref(false)

onMounted(async () => {
  const { report: data, isLive } = await getMonthlySalesReport()
  report.value = data
  usingMock.value = !isLive
  const ms = report.value?.months ?? []
  if (ms.length) selectedMonth.value = ms[ms.length - 1].month
  loading.value = false
})

const currency = computed(() => report.value?.currency || 'MYR')
const months = computed(() => report.value?.months ?? [])

// Live API gives sales/total directly; mock gives unitPrice + qty. Support both.
const itemSales = (i) => (i.sales != null ? i.sales : (i.unitPrice ?? 0) * i.qty)
const monthTotal = (m) =>
  m.total != null ? m.total : m.items.reduce((s, i) => s + itemSales(i), 0)

const monthlyTotals = computed(() =>
  months.value.map((m) => ({ month: m.month, label: m.label, total: monthTotal(m), orders: m.orderCount })),
)
const maxMonthly = computed(() => Math.max(1, ...monthlyTotals.value.map((m) => m.total)))
const ytdTotal = computed(() => monthlyTotals.value.reduce((s, m) => s + m.total, 0))

const current = computed(() => months.value.find((m) => m.month === selectedMonth.value) || null)

const currentItems = computed(() => {
  if (!current.value) return []
  const items = current.value.items.map((i) => ({ name: i.name, qty: i.qty, sales: itemSales(i) }))
  const total = items.reduce((s, i) => s + i.sales, 0) || 1
  return items.map((i) => ({ ...i, pct: (i.sales / total) * 100 })).sort((a, b) => b.sales - a.sales)
})
const maxItemSales = computed(() => Math.max(1, ...currentItems.value.map((i) => i.sales)))

const currentTotal = computed(() => (current.value ? monthTotal(current.value) : 0))
const currentOrders = computed(() => current.value?.orderCount || 0)
const avgOrder = computed(() => (currentOrders.value ? currentTotal.value / currentOrders.value : 0))
const topItem = computed(() => currentItems.value[0]?.name || '—')

const money = (n) =>
  new Intl.NumberFormat('en-MY', { maximumFractionDigits: 0 }).format(Math.round(n))
const money2 = (n) =>
  new Intl.NumberFormat('en-MY', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(n)

const barColors = ['#2563eb', '#7c3aed', '#059669', '#e11d48', '#d97706', '#0ea5e9']
</script>

<template>
  <div class="report">
    <header class="rep-head">
      <div class="rep-head-row">
        <div>
          <p class="eyebrow">Forkly Admin · Sales</p>
          <h1>Monthly Sales Report</h1>
        </div>
        <span v-if="usingMock" class="mock-badge" title="Showing sample data until the Order report API is connected">
          ● Mock data
        </span>
      </div>
      <p class="rep-sub">Total sales and per-item breakdown by month. Click a month to drill in.</p>
    </header>

    <div v-if="loading" class="state">Loading report…</div>

    <div v-else class="rep-body">
      <!-- KPI cards -->
      <section class="kpis">
        <div class="kpi">
          <span class="kpi-label">Sales · {{ current?.label }}</span>
          <span class="kpi-value">{{ currency }} {{ money(currentTotal) }}</span>
          <span class="kpi-foot">YTD {{ currency }} {{ money(ytdTotal) }}</span>
        </div>
        <div class="kpi">
          <span class="kpi-label">Orders · {{ current?.label }}</span>
          <span class="kpi-value">{{ money(currentOrders) }}</span>
          <span class="kpi-foot">across all menu items</span>
        </div>
        <div class="kpi">
          <span class="kpi-label">Avg order value</span>
          <span class="kpi-value">{{ currency }} {{ money2(avgOrder) }}</span>
          <span class="kpi-foot">sales ÷ orders</span>
        </div>
        <div class="kpi">
          <span class="kpi-label">Top item · {{ current?.label }}</span>
          <span class="kpi-value sm">{{ topItem }}</span>
          <span class="kpi-foot">best seller by revenue</span>
        </div>
      </section>

      <!-- Monthly total sales bar chart -->
      <section class="card">
        <div class="card-head">
          <h2>Total sales per month</h2>
          <span class="card-note">{{ currency }}</span>
        </div>
        <div class="bars" role="list">
          <button
            v-for="m in monthlyTotals"
            :key="m.month"
            type="button"
            class="bar-col"
            :class="{ active: m.month === selectedMonth }"
            role="listitem"
            :aria-label="`${m.label}: ${currency} ${money(m.total)}`"
            @click="selectedMonth = m.month"
          >
            <span class="bar-val">{{ money(m.total) }}</span>
            <span class="bar" :style="{ height: (m.total / maxMonthly) * 160 + 'px' }"></span>
            <span class="bar-label">{{ m.label }}</span>
          </button>
        </div>
      </section>

      <!-- Per-item breakdown for the selected month -->
      <section class="card">
        <div class="card-head">
          <h2>Sales by menu item · {{ current?.label }}</h2>
          <span class="card-note">{{ currentOrders }} orders</span>
        </div>

        <div class="items">
          <div v-for="(it, idx) in currentItems" :key="it.name" class="item-row">
            <span class="item-name">{{ it.name }}</span>
            <div class="item-track">
              <span
                class="item-fill"
                :style="{ width: (it.sales / maxItemSales) * 100 + '%', background: barColors[idx % barColors.length] }"
              ></span>
            </div>
            <span class="item-sales">{{ currency }} {{ money(it.sales) }}</span>
            <span class="item-pct">{{ it.pct.toFixed(1) }}%</span>
          </div>
        </div>

        <table class="tbl">
          <thead>
            <tr><th>Menu item</th><th class="r">Units sold</th><th class="r">Sales ({{ currency }})</th><th class="r">Share</th></tr>
          </thead>
          <tbody>
            <tr v-for="it in currentItems" :key="it.name">
              <td>{{ it.name }}</td>
              <td class="r">{{ money(it.qty) }}</td>
              <td class="r">{{ money(it.sales) }}</td>
              <td class="r">{{ it.pct.toFixed(1) }}%</td>
            </tr>
          </tbody>
          <tfoot>
            <tr><td>Total</td><td class="r">—</td><td class="r">{{ money(currentTotal) }}</td><td class="r">100%</td></tr>
          </tfoot>
        </table>
      </section>
    </div>
  </div>
</template>

<style scoped>
.report { max-width: 1080px; margin: 0 auto; padding: 32px 24px 72px; }

.rep-head { margin-bottom: 24px; }
.rep-head-row { display: flex; align-items: flex-start; justify-content: space-between; gap: 16px; }
.eyebrow { margin: 0 0 4px; font-size: 0.72rem; font-weight: 700; letter-spacing: 0.14em; text-transform: uppercase; color: var(--color-primary); }
.rep-head h1 { margin: 0; font-size: clamp(1.5rem, 4vw, 2rem); font-weight: 800; color: var(--color-ink); letter-spacing: -0.4px; }
.rep-sub { margin: 8px 0 0; color: var(--color-muted); }
.mock-badge { flex: none; font-size: 0.74rem; font-weight: 700; color: #b45309; background: #fffbeb; border: 1px solid #fde68a; padding: 5px 11px; border-radius: 999px; }

.state { text-align: center; color: var(--color-muted); padding: 60px 0; }

/* KPI cards */
.kpis { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 20px; }
.kpi { background: var(--color-bg); border: 1px solid var(--color-border); border-radius: var(--radius); padding: 18px 20px; box-shadow: var(--shadow-sm); display: flex; flex-direction: column; gap: 4px; }
.kpi-label { font-size: 0.78rem; color: var(--color-muted); font-weight: 600; }
.kpi-value { font-size: 1.7rem; font-weight: 800; color: var(--color-ink); letter-spacing: -0.5px; line-height: 1.1; }
.kpi-value.sm { font-size: 1.15rem; }
.kpi-foot { font-size: 0.74rem; color: var(--color-muted); }

/* Cards */
.card { background: var(--color-bg); border: 1px solid var(--color-border); border-radius: var(--radius); padding: 22px 24px 26px; box-shadow: var(--shadow-sm); margin-bottom: 20px; }
.card-head { display: flex; align-items: baseline; justify-content: space-between; margin-bottom: 18px; }
.card-head h2 { margin: 0; font-size: 1.05rem; font-weight: 700; color: var(--color-ink); }
.card-note { font-size: 0.78rem; color: var(--color-muted); font-weight: 600; }

/* Monthly bar chart */
.bars { display: flex; align-items: flex-end; gap: 14px; min-height: 210px; padding-top: 8px; }
.bar-col { flex: 1; display: flex; flex-direction: column; align-items: center; gap: 8px; background: none; border: none; cursor: pointer; padding: 0; }
.bar-val { font-size: 0.74rem; font-weight: 700; color: var(--color-muted); }
.bar { width: 100%; max-width: 64px; border-radius: 8px 8px 0 0; background: #c7d6f5; transition: background 0.15s ease, transform 0.15s ease; }
.bar-col:hover .bar { background: #9bb6ee; }
.bar-col.active .bar { background: var(--color-primary); }
.bar-col.active .bar-val { color: var(--color-primary); }
.bar-label { font-size: 0.82rem; font-weight: 600; color: var(--color-body); }

/* Item breakdown bars */
.items { display: flex; flex-direction: column; gap: 12px; margin-bottom: 22px; }
.item-row { display: grid; grid-template-columns: 150px 1fr 100px 56px; align-items: center; gap: 12px; }
.item-name { font-size: 0.9rem; font-weight: 600; color: var(--color-ink); }
.item-track { background: var(--color-surface); border-radius: 999px; height: 14px; overflow: hidden; }
.item-fill { display: block; height: 100%; border-radius: 999px; transition: width 0.3s ease; }
.item-sales { font-size: 0.86rem; font-weight: 700; color: var(--color-ink); text-align: right; }
.item-pct { font-size: 0.8rem; color: var(--color-muted); text-align: right; }

/* Table */
.tbl { width: 100%; border-collapse: collapse; font-size: 0.88rem; }
.tbl th { text-align: left; font-size: 0.72rem; text-transform: uppercase; letter-spacing: 0.05em; color: var(--color-muted); padding: 8px 10px; border-bottom: 1px solid var(--color-border); }
.tbl td { padding: 9px 10px; border-bottom: 1px solid var(--color-border); color: var(--color-body); }
.tbl .r { text-align: right; }
.tbl tfoot td { font-weight: 800; color: var(--color-ink); border-bottom: none; }

@media (max-width: 860px) {
  .kpis { grid-template-columns: repeat(2, 1fr); }
  .item-row { grid-template-columns: 110px 1fr 84px; }
  .item-pct { display: none; }
}
@media (max-width: 520px) {
  .kpis { grid-template-columns: 1fr; }
}
</style>
