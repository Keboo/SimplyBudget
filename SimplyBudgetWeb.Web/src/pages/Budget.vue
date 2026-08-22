<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { BudgetResponse, BudgetCategoryDto, ExpenseCategoryDto, ExpenseCategoryMonthlyExpensesDto } from '@/types'
import { formatCents, formatMonth, parseMonth } from '@/utils/currency'
import { useRouter } from 'vue-router'
import AddTransactionDialog from '@/components/AddTransactionDialog.vue'

const snackbar = useSnackbarStore()
const router = useRouter()

const currentMonth = new Date()
const budget = ref<BudgetResponse | null>(null)
const loading = ref(false)
const dialogOpen = ref(false)
const categories = ref<ExpenseCategoryDto[]>([])
const categoryChartDialogOpen = ref(false)
const categoryChartLoading = ref(false)
const categoryChart = ref<ExpenseCategoryMonthlyExpensesDto | null>(null)

interface Group {
  groupName: string
  items: BudgetCategoryDto[]
}

function compareCategoryName(a: BudgetCategoryDto, b: BudgetCategoryDto): number {
  return (a.name ?? '').localeCompare(b.name ?? '', undefined, { sensitivity: 'base' })
}

const grouped = computed<Group[]>(() => {
  const map = new Map<string, BudgetCategoryDto[]>()
  for (const cat of budget.value?.categories ?? []) {
    const key = cat.categoryName?.trim() || 'Uncategorized'
    if (!map.has(key)) map.set(key, [])
    map.get(key)!.push(cat)
  }
  return Array.from(map.entries()).map(([groupName, items]) => ({
    groupName,
    items: [...items].sort(compareCategoryName),
  }))
})

const categoryChartMaxAmount = computed(() => {
  const chart = categoryChart.value
  if (!chart) return 1

  const maxMonthlyExpense = chart.months.reduce((max, month) => Math.max(max, month.amount), 0)
  return Math.max(maxMonthlyExpense, chart.budgetedAmount, 1)
})

const budgetLinePercent = computed(() => {
  const chart = categoryChart.value
  if (!chart) return 0
  return Math.min(100, (chart.budgetedAmount / categoryChartMaxAmount.value) * 100)
})

const budgetLineLabel = computed(() => {
  const chart = categoryChart.value
  if (!chart) return ''
  return chart.usePercentage
    ? `${chart.budgetedPercentage}% budget`
    : `${formatCents(chart.budgetedAmount)} budget`
})

async function fetchBudget() {
  loading.value = true
  try {
    const month = formatMonth(currentMonth)
    budget.value = await apiClient.get<BudgetResponse>(`/api/budget?month=${month}-01`)
  } catch {
    snackbar.enqueueSnackbar('Failed to load budget', { variant: 'error' })
  } finally {
    loading.value = false
  }
}

async function fetchCategories() {
  try {
    categories.value = await apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories')
  } catch { /* ignore */ }
}

onMounted(() => {
  void fetchBudget()
  void fetchCategories()
})

function onDialogSuccess() {
  void fetchBudget()
  dialogOpen.value = false
}

function getBarHeightPercent(amount: number): number {
  if (amount <= 0) return 0
  return Math.max((amount / categoryChartMaxAmount.value) * 100, 2)
}

function formatChartMonth(month: string): string {
  return parseMonth(month).toLocaleString('default', { month: 'short' })
}

function formatChartTooltip(month: string, amount: number): string {
  const date = parseMonth(month)
  const label = date.toLocaleString('default', { month: 'long', year: 'numeric' })
  return `${label}: ${formatCents(amount)}`
}

async function openCategoryChart(category: BudgetCategoryDto) {
  categoryChartDialogOpen.value = true
  categoryChartLoading.value = true
  categoryChart.value = null

  try {
    const month = formatMonth(currentMonth)
    categoryChart.value = await apiClient.get<ExpenseCategoryMonthlyExpensesDto>(
      `/api/expense-categories/${category.id}/monthly-expenses?month=${month}-01&months=12`,
    )
  } catch {
    snackbar.enqueueSnackbar('Failed to load category chart', { variant: 'error' })
  } finally {
    categoryChartLoading.value = false
  }
}

function openCategoryHistory(category: BudgetCategoryDto) {
  void router.push({ name: 'history', query: { categoryId: String(category.id) } })
}
</script>

<template>
  <div>
    <v-card v-if="budget" class="pa-4 mb-4">
      <div class="d-flex flex-wrap" style="gap: 16px;">
        <span class="text-h5">Total Budget: <strong>{{ formatCents(budget.totalBudget) }}</strong></span>
        <span class="text-h5">Total In Accounts: <strong>{{ formatCents(budget.totalAccountAmount) }}</strong></span>
      </div>
    </v-card>

    <div v-if="loading" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate aria-label="Loading budget" />
    </div>
    <div v-else>
      <v-expansion-panels v-for="group in grouped" :key="group.groupName" :model-value="0" class="mb-2">
        <v-expansion-panel>
          <v-expansion-panel-title>
            <span class="font-weight-bold">{{ group.groupName }}</span>
          </v-expansion-panel-title>
          <v-expansion-panel-text class="pa-0">
            <v-list density="compact">
              <v-list-item
                v-for="cat in group.items"
                :key="cat.id"
                class="border-b category-clickable-row"
                @click="openCategoryHistory(cat)"
              >
                <v-list-item-title>{{ cat.name ?? '(unnamed)' }}</v-list-item-title>
                <v-list-item-subtitle>
                  <div class="budget-chip-row d-flex flex-wrap mt-1" style="gap: 4px;">
                    <v-chip size="small">Budget: {{ cat.usePercentage ? `${cat.budgetedPercentage}%` : formatCents(cat.budgetedAmount) }}</v-chip>
                    <v-chip size="small" color="error" variant="outlined">Spent: {{ formatCents(cat.monthlyExpenses) }}</v-chip>
                    <v-chip size="small" :color="cat.currentBalance >= 0 ? 'success' : 'error'">Balance: {{ formatCents(cat.currentBalance) }}</v-chip>
                    <v-chip size="small" variant="outlined" class="avg-spend-chip">3mo avg: {{ formatCents(cat.threeMonthAverage) }}</v-chip>
                    <v-chip size="small" variant="outlined" class="avg-spend-chip">6mo avg: {{ formatCents(cat.sixMonthAverage) }}</v-chip>
                    <v-chip size="small" variant="outlined" class="avg-spend-chip">12mo avg: {{ formatCents(cat.twelveMonthAverage) }}</v-chip>
                  </div>
                </v-list-item-subtitle>
                <template #append>
                  <v-btn
                    icon="mdi-chart-bar"
                    variant="text"
                    size="small"
                    aria-label="Open category chart"
                    @click.stop="openCategoryChart(cat)"
                  />
                </template>
              </v-list-item>
            </v-list>
          </v-expansion-panel-text>
        </v-expansion-panel>
      </v-expansion-panels>
    </div>

    <v-btn
      icon="mdi-plus"
      color="primary"
      aria-label="Add Transaction"
      style="position: fixed; bottom: 32px; right: 32px; z-index: 100;"
      @click="dialogOpen = true"
    />

    <AddTransactionDialog
      v-model="dialogOpen"
      :categories="categories"
      @success="onDialogSuccess"
    />

    <v-dialog v-model="categoryChartDialogOpen" max-width="980">
      <v-card>
        <v-card-title>{{ categoryChart?.name ?? 'Category' }} spending (last 12 months)</v-card-title>
        <v-card-text>
          <div v-if="categoryChartLoading" class="d-flex justify-center pa-8">
            <v-progress-circular indeterminate aria-label="Loading category chart" />
          </div>
          <div v-else-if="categoryChart">
            <v-chip size="small" color="primary" variant="outlined" class="mb-3">
              Budget line: {{ budgetLineLabel }}
            </v-chip>
            <div class="d-flex flex-wrap mb-3" style="gap: 6px;">
              <v-chip size="small" variant="outlined">3mo avg: {{ formatCents(categoryChart.months.slice(-3).reduce((sum, point) => sum + point.amount, 0) / 3) }}</v-chip>
              <v-chip size="small" variant="outlined">6mo avg: {{ formatCents(categoryChart.months.slice(-6).reduce((sum, point) => sum + point.amount, 0) / 6) }}</v-chip>
              <v-chip size="small" variant="outlined">12mo avg: {{ formatCents(categoryChart.months.reduce((sum, point) => sum + point.amount, 0) / 12) }}</v-chip>
            </div>
            <div class="expense-chart">
              <div class="expense-chart-plot">
                <div class="expense-chart-budget-line" :style="{ bottom: `${budgetLinePercent}%` }">
                  <span class="expense-chart-budget-label">{{ budgetLineLabel }}</span>
                </div>
                <div class="expense-chart-bars">
                  <div
                    v-for="point in categoryChart.months"
                    :key="point.month"
                    class="expense-chart-month"
                    :title="formatChartTooltip(point.month, point.amount)"
                  >
                    <div class="expense-chart-bar-wrapper">
                      <div class="expense-chart-bar" :style="{ height: `${getBarHeightPercent(point.amount)}%` }" />
                    </div>
                  </div>
                </div>
              </div>
              <div class="expense-chart-axis">
                <span
                  v-for="point in categoryChart.months"
                  :key="`${point.month}-label`"
                  class="expense-chart-month-label"
                >
                  {{ formatChartMonth(point.month) }}
                </span>
              </div>
            </div>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="categoryChartDialogOpen = false">Close</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<style scoped>
.category-clickable-row {
  cursor: pointer;
}

.expense-chart {
  position: relative;
  height: 320px;
  border: 1px solid rgba(var(--v-theme-on-surface), 0.12);
  border-radius: 8px;
  padding: 16px 12px 8px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.expense-chart-plot {
  position: relative;
  flex: 1;
  min-height: 0;
}

.expense-chart-budget-line {
  position: absolute;
  left: 0;
  right: 0;
  border-top: 2px dashed rgb(var(--v-theme-error));
  pointer-events: none;
  z-index: 1;
}

.expense-chart-budget-label {
  position: absolute;
  right: 0;
  transform: translateY(-100%);
  color: rgb(var(--v-theme-error));
  font-size: 0.75rem;
  padding: 0 4px;
  background-color: rgba(var(--v-theme-surface), 0.9);
}

.expense-chart-bars {
  position: relative;
  height: 100%;
  display: grid;
  grid-template-columns: repeat(12, minmax(0, 1fr));
  gap: 8px;
  align-items: end;
  z-index: 0;
}

.expense-chart-month {
  height: 100%;
  display: flex;
  align-items: end;
}

.expense-chart-bar-wrapper {
  height: 100%;
  width: 100%;
  display: flex;
  align-items: end;
}

.expense-chart-bar {
  width: 100%;
  border-radius: 4px 4px 0 0;
  background-color: rgb(var(--v-theme-primary));
}

.expense-chart-axis {
  display: grid;
  grid-template-columns: repeat(12, minmax(0, 1fr));
  gap: 8px;
}

.expense-chart-month-label {
  display: block;
  text-align: center;
  font-size: 0.75rem;
  color: rgba(var(--v-theme-on-surface), 0.75);
}
</style>
<style scoped>
@media (max-width: 720px) {
  .avg-spend-chip {
    display: none;
  }
}
</style>
