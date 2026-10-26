<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import { useDisplay } from 'vuetify'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import { useAuthStore } from '@/stores/auth'
import type { AccountDto, BudgetResponse, BudgetCategoryDto, ExpenseCategoryDto, ExpenseCategoryMonthlyExpensesDto } from '@/types'
import { formatCents, formatMonth, parseMonth } from '@/utils/currency'
import { useRouter } from 'vue-router'
import AddTransactionDialog from '@/components/AddTransactionDialog.vue'
import MonthPickerNav from '@/components/MonthPickerNav.vue'
import { useMonthQueryParam } from '@/composables/useMonthQueryParam'
import { createMonthUpdatesHubClient } from '@/services/monthUpdatesHub'
import { useContextualLoad } from '@/composables/useContextualLoad'
import LoadFeedback from '@/components/LoadFeedback.vue'

const snackbar = useSnackbarStore()
const router = useRouter()
const authStore = useAuthStore()
const { xs: phone } = useDisplay()

const { currentMonth } = useMonthQueryParam()
const { data: budget, loading, error: loadError, hasLoaded, load: loadBudget } = useContextualLoad<BudgetResponse | null>(null, 'Failed to load budget')
const { data: accounts, loading: accountLoading, error: accountError, hasLoaded: accountsLoaded, load: loadAccounts } = useContextualLoad<AccountDto[]>([], 'Failed to load account balances')
const dialogOpen = ref(false)
const { data: categories, loading: categoriesLoading, error: categoriesError, hasLoaded: categoriesLoaded, load: loadCategories } = useContextualLoad<ExpenseCategoryDto[]>([], 'Failed to load categories')
const categoryChartDialogOpen = ref(false)
const categoryChartLoading = ref(false)
const categoryChart = ref<ExpenseCategoryMonthlyExpensesDto | null>(null)
let realtimeRefreshInFlight = false
let realtimeRefreshQueued = false

const monthLabel = computed(() =>
  currentMonth.value.toLocaleString('default', { month: 'long', year: 'numeric' }),
)

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

const chartThreeMonthAverage = computed(() => getCategoryChartAverage(3))
const chartSixMonthAverage = computed(() => getCategoryChartAverage(6))
const chartTwelveMonthAverage = computed(() => getCategoryChartAverage(12))

async function fetchBudget() {
  const month = formatMonth(currentMonth.value)
  await loadBudget(month, () => apiClient.get<BudgetResponse>(`/api/budget?month=${month}-01`))
}

async function fetchAccountBalances() {
  const month = `${formatMonth(currentMonth.value)}-01`
  const params = new URLSearchParams({ month })
  await loadAccounts(month, async () => await apiClient.get<AccountDto[]>(`/api/accounts?${params}`) ?? [])
}

async function fetchCategories() {
  await loadCategories('categories', () => apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories'))
}

async function refreshMonthInBackground() {
  if (realtimeRefreshInFlight) {
    realtimeRefreshQueued = true
    return
  }

  realtimeRefreshInFlight = true
  try {
    do {
      realtimeRefreshQueued = false
      await Promise.all([fetchBudget(), fetchAccountBalances()])
    } while (realtimeRefreshQueued)
  } finally {
    realtimeRefreshInFlight = false
  }
}

const monthUpdatesHub = createMonthUpdatesHubClient(
  () => authStore.getToken(),
  (month) => {
    if (month === formatMonth(currentMonth.value)) {
      void refreshMonthInBackground()
    }
  },
)

onMounted(() => {
  void fetchBudget()
  void fetchAccountBalances()
  void fetchCategories()
  void monthUpdatesHub.start(formatMonth(currentMonth.value))
})

watch(currentMonth, () => {
  void fetchBudget()
  void fetchAccountBalances()
  void monthUpdatesHub.setMonth(formatMonth(currentMonth.value))
})

onBeforeUnmount(() => {
  void monthUpdatesHub.stop()
})

function onDialogSuccess() {
  void fetchBudget()
  void fetchAccountBalances()
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

function getCategoryChartAverage(monthCount: number): number {
  const chart = categoryChart.value
  if (!chart) return 0

  return chart.months.slice(-monthCount).reduce((sum, point) => sum + point.amount, 0) / monthCount
}

function getAverageChipColor(usePercentage: boolean, budgetedAmount: number, average: number): 'error' | undefined {
  return !usePercentage && average > budgetedAmount ? 'error' : undefined
}

const expandedAverages = ref(new Set<number>())

function isAveragesExpanded(categoryId: number): boolean {
  return expandedAverages.value.has(categoryId)
}

function toggleAverages(categoryId: number) {
  const next = new Set(expandedAverages.value)
  if (!next.delete(categoryId)) next.add(categoryId)
  expandedAverages.value = next
}

async function openCategoryChart(category: BudgetCategoryDto) {
  categoryChartDialogOpen.value = true
  categoryChartLoading.value = true
  categoryChart.value = null

  try {
    const month = formatMonth(currentMonth.value)
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
    <v-card class="pa-4 mb-4 d-flex flex-wrap align-center" style="gap: 16px;">
      <MonthPickerNav v-model="currentMonth" />
    </v-card>

    <LoadFeedback label="budget" :error="loadError" :loading="loading" :stale="hasLoaded" @retry="fetchBudget" />
    <LoadFeedback label="categories" :error="categoriesError" :loading="categoriesLoading" :stale="categoriesLoaded" @retry="fetchCategories" />
    <v-card v-if="budget" class="pa-4 mb-4">
      <div v-if="phone">
        <div class="text-body-2">Total budget for {{ monthLabel }}</div>
        <strong class="text-h5">{{ formatCents(budget.totalBudget) }}</strong>
        <div class="text-body-2">{{ budget.categories.filter(category => category.currentBalance < 0).length }} categories over budget</div>
        <details class="budget-details">
          <summary>Income and account totals</summary>
          <div>Estimated Monthly Income: {{ formatCents(budget.estimatedMonthlyIncome) }}</div>
          <div>Total In Accounts: {{ formatCents(budget.totalAccountAmount) }}</div>
        </details>
      </div>
      <div v-else class="d-flex flex-wrap" style="gap: 16px;">
        <span class="text-h5">Total Budget: <strong>{{ formatCents(budget.totalBudget) }}</strong></span>
        <span class="text-h5">Estimated Monthly Income: <strong>{{ formatCents(budget.estimatedMonthlyIncome) }}</strong></span>
        <span class="text-h5">Total In Accounts: <strong>{{ formatCents(budget.totalAccountAmount) }}</strong></span>
      </div>
    </v-card>

    <LoadFeedback label="account balances" :error="accountError" :loading="accountLoading" :stale="accountsLoaded" @retry="fetchAccountBalances" />
    <v-card class="pa-4 mb-4">
      <details :open="!phone" class="budget-details">
      <summary class="text-subtitle-2">Account balances through {{ monthLabel }}</summary>
      <div v-if="accountLoading" class="d-flex justify-center py-2">
        <v-progress-circular size="20" indeterminate aria-label="Loading account balances" />
      </div>
      <div v-if="accountsLoaded && !accountError && accounts.length === 0" class="text-medium-emphasis">No accounts found.</div>
      <div v-else-if="accountsLoaded && accounts.length > 0" class="d-flex flex-wrap" style="gap: 8px;">
        <v-chip v-for="account in accounts" :key="account.id" size="small" variant="outlined">
          {{ account.name ?? 'Unnamed account' }}: {{ formatCents(account.currentAmount) }}
        </v-chip>
      </div>
      </details>
    </v-card>

    <div v-if="loading && !hasLoaded" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate aria-label="Loading budget" />
    </div>
    <div v-else>
      <v-alert v-if="hasLoaded && !loadError && budget?.categories.length === 0" type="info" variant="tonal" class="mb-4">
        No budget categories yet. <v-btn to="/settings" variant="text" min-height="44">Manage categories</v-btn>
      </v-alert>
      <v-expansion-panels v-for="group in grouped" :key="group.groupName" :model-value="0" class="mb-2">
        <v-expansion-panel>
          <v-expansion-panel-title>
            <span class="font-weight-bold">{{ group.groupName }}</span>
          </v-expansion-panel-title>
          <v-expansion-panel-text class="pa-0 budget-category-panel">
            <v-list density="compact">
              <v-list-item
                v-for="cat in group.items"
                :key="cat.id"
                class="border-b category-clickable-row"
                @click="openCategoryHistory(cat)"
              >
                <div class="budget-category-content">
                  <div class="budget-category-heading mb-1">
                    <span class="budget-category-name">{{ cat.name ?? '(unnamed)' }}</span>
                    <v-chip size="small" :color="cat.currentBalance >= 0 ? 'success' : 'error'">
                      Balance: {{ formatCents(cat.currentBalance) }}
                    </v-chip>
                  </div>
                  <div v-if="cat.description" class="budget-category-description mb-1">{{ cat.description }}</div>
                  <div class="budget-chip-row d-flex flex-wrap mt-1" style="gap: 4px;">
                    <v-chip size="small">Budget: {{ cat.usePercentage ? `${cat.budgetedPercentage}%` : formatCents(cat.budgetedAmount) }}</v-chip>
                    <v-chip size="small" variant="outlined" :title="`Allocated in ${monthLabel}`">Allocated: {{ formatCents(cat.monthlyAllocations) }}</v-chip>
                    <v-chip size="small" color="error" variant="outlined">Spent: {{ formatCents(cat.monthlyExpenses) }}</v-chip>
                    <v-btn
                      :icon="isAveragesExpanded(cat.id) ? 'mdi-chevron-left' : 'mdi-chart-timeline-variant'"
                      :variant="isAveragesExpanded(cat.id) ? 'tonal' : 'text'"
                      :aria-label="isAveragesExpanded(cat.id) ? 'Hide spending averages' : 'Show spending averages'"
                      :title="isAveragesExpanded(cat.id) ? 'Hide spending averages' : 'Show spending averages'"
                      :aria-expanded="isAveragesExpanded(cat.id) ? 'true' : 'false'"
                      :aria-controls="`budget-averages-${cat.id}`"
                      width="24"
                      height="24"
                      size="x-small"
                      class="budget-averages-toggle"
                      @click.stop="toggleAverages(cat.id)"
                      @keydown.stop
                    />
                    <span
                      v-show="isAveragesExpanded(cat.id)"
                      :id="`budget-averages-${cat.id}`"
                      class="budget-averages"
                    >
                      <v-chip
                        size="small"
                        variant="outlined"
                        :color="getAverageChipColor(cat.usePercentage, cat.budgetedAmount, cat.threeMonthAverage)"
                      >
                        3mo avg: {{ formatCents(cat.threeMonthAverage) }}
                      </v-chip>
                      <v-chip
                        size="small"
                        variant="outlined"
                        :color="getAverageChipColor(cat.usePercentage, cat.budgetedAmount, cat.sixMonthAverage)"
                      >
                        6mo avg: {{ formatCents(cat.sixMonthAverage) }}
                      </v-chip>
                      <v-chip
                        size="small"
                        variant="outlined"
                        :color="getAverageChipColor(cat.usePercentage, cat.budgetedAmount, cat.twelveMonthAverage)"
                      >
                        12mo avg: {{ formatCents(cat.twelveMonthAverage) }}
                      </v-chip>
                    </span>
                  </div>
                </div>
                <template #append>
                  <v-btn
                    icon="mdi-chart-bar"
                    variant="text"
                    size="small"
                    min-width="44"
                    min-height="44"
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
              <v-chip
                size="small"
                variant="outlined"
                :color="getAverageChipColor(categoryChart.usePercentage, categoryChart.budgetedAmount, chartThreeMonthAverage)"
              >
                3mo avg: {{ formatCents(chartThreeMonthAverage) }}
              </v-chip>
              <v-chip
                size="small"
                variant="outlined"
                :color="getAverageChipColor(categoryChart.usePercentage, categoryChart.budgetedAmount, chartSixMonthAverage)"
              >
                6mo avg: {{ formatCents(chartSixMonthAverage) }}
              </v-chip>
              <v-chip
                size="small"
                variant="outlined"
                :color="getAverageChipColor(categoryChart.usePercentage, categoryChart.budgetedAmount, chartTwelveMonthAverage)"
              >
                12mo avg: {{ formatCents(chartTwelveMonthAverage) }}
              </v-chip>
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
.budget-details > summary {
  min-height: 44px;
  display: list-item;
  align-content: center;
  cursor: pointer;
  white-space: normal;
}
.budget-details :deep(.v-chip) {
  max-width: 100%;
  height: auto;
  min-height: 24px;
}
.budget-details :deep(.v-chip__content) {
  white-space: normal;
  overflow-wrap: anywhere;
}
.category-clickable-row {
  cursor: pointer;
}

.budget-category-panel :deep(.v-expansion-panel-text__wrapper) {
  padding-inline: 8px;
}

.budget-category-panel :deep(.v-list-item__content) {
  overflow: visible;
}

.budget-category-content {
  white-space: normal;
  overflow: visible;
  overflow-wrap: anywhere;
}

.budget-category-heading {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 4px 8px;
}

.budget-category-content :deep(.v-chip) {
  max-width: 100%;
  height: auto;
  min-height: 24px;
}

.budget-category-content :deep(.v-chip__content) {
  white-space: normal;
  overflow-wrap: anywhere;
}

.budget-category-name {
  font-size: 0.8125rem;
  font-weight: 600;
  line-height: 1.25rem;
  color: rgba(var(--v-theme-on-surface), 0.87);
}

.budget-category-description {
  font-size: 0.875rem;
  line-height: 1.25rem;
  color: rgba(var(--v-theme-on-surface), 0.6);
}

.budget-chip-row {
  align-items: center;
}

.budget-averages {
  display: contents;
}

/* Visually matches chip height; the pseudo-element keeps a 44px touch target without growing the row. */
.budget-averages-toggle {
  position: relative;
  z-index: 1;
  overflow: visible;
  margin-inline: 6px;
}

.budget-averages-toggle::before {
  content: '';
  position: absolute;
  inset: -10px;
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
