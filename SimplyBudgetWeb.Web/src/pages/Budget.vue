<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { BudgetResponse, BudgetCategoryDto, ExpenseCategoryDto } from '@/types'
import { formatCents, formatMonth, parseMonth } from '@/utils/currency'
import AddTransactionDialog from '@/components/AddTransactionDialog.vue'

const snackbar = useSnackbarStore()
const route = useRoute()
const router = useRouter()

function initialMonth(): Date {
  const q = route.query.month
  if (typeof q === 'string' && /^\d{4}-\d{2}$/.test(q)) return parseMonth(q)
  const now = new Date()
  return new Date(now.getFullYear(), now.getMonth(), 1)
}

const currentMonth = ref(initialMonth())
const budget = ref<BudgetResponse | null>(null)
const loading = ref(false)
const dialogOpen = ref(false)
const categories = ref<ExpenseCategoryDto[]>([])

const monthLabel = computed(() =>
  currentMonth.value.toLocaleString('default', { month: 'long', year: 'numeric' }),
)

interface Group {
  groupName: string
  items: BudgetCategoryDto[]
}

const grouped = computed<Group[]>(() => {
  const map = new Map<string, BudgetCategoryDto[]>()
  for (const cat of budget.value?.categories ?? []) {
    const key = cat.categoryName?.trim() || 'Uncategorized'
    if (!map.has(key)) map.set(key, [])
    map.get(key)!.push(cat)
  }
  return Array.from(map.entries()).map(([groupName, items]) => ({ groupName, items }))
})

async function fetchBudget() {
  loading.value = true
  try {
    const month = formatMonth(currentMonth.value)
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

function prevMonth() {
  const d = currentMonth.value
  currentMonth.value = new Date(d.getFullYear(), d.getMonth() - 1, 1)
}

function nextMonth() {
  const d = currentMonth.value
  currentMonth.value = new Date(d.getFullYear(), d.getMonth() + 1, 1)
}

watch(currentMonth, (month) => {
  void router.replace({ query: { ...route.query, month: formatMonth(month) } })
  void fetchBudget()
})

onMounted(() => {
  void fetchBudget()
  void fetchCategories()
})

function onDialogSuccess() {
  void fetchBudget()
  dialogOpen.value = false
}
</script>

<template>
  <div>
    <div class="d-flex align-center mb-4" style="gap: 8px;">
      <v-btn variant="outlined" size="small" prepend-icon="mdi-chevron-left" @click="prevMonth">Prev</v-btn>
      <span class="text-h6" style="min-width: 160px; text-align: center;">{{ monthLabel }}</span>
      <v-btn variant="outlined" size="small" append-icon="mdi-chevron-right" @click="nextMonth">Next</v-btn>
    </div>

    <v-card v-if="budget" class="pa-4 mb-4">
      <span class="text-h5">Total Budget: <strong>{{ formatCents(budget.totalBudget) }}</strong></span>
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
              <v-list-item v-for="cat in group.items" :key="cat.id" class="border-b">
                <v-list-item-title>{{ cat.name ?? '(unnamed)' }}</v-list-item-title>
                <v-list-item-subtitle>
                  <div class="d-flex flex-wrap mt-1" style="gap: 4px;">
                    <v-chip size="small">Budget: {{ cat.usePercentage ? `${cat.budgetedPercentage}%` : formatCents(cat.budgetedAmount) }}</v-chip>
                    <v-chip size="small" color="error" variant="outlined">Spent: {{ formatCents(cat.monthlyExpenses) }}</v-chip>
                    <v-chip size="small" :color="cat.currentBalance >= 0 ? 'success' : 'error'">Balance: {{ formatCents(cat.currentBalance) }}</v-chip>
                    <v-chip size="small" variant="outlined">3mo avg: {{ formatCents(cat.threeMonthAverage) }}</v-chip>
                    <v-chip size="small" variant="outlined">6mo avg: {{ formatCents(cat.sixMonthAverage) }}</v-chip>
                    <v-chip size="small" variant="outlined">12mo avg: {{ formatCents(cat.twelveMonthAverage) }}</v-chip>
                  </div>
                </v-list-item-subtitle>
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
  </div>
</template>
