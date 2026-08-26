<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { HistoryItemDto, ExpenseCategoryDto, AccountDto } from '@/types'
import { formatCents, formatMonth } from '@/utils/currency'
import { useMonthQueryParam } from '@/composables/useMonthQueryParam'
import { AMAZON_TRANSACTIONS_URL, hasAmazonInDescription } from '@/utils/merchantLinks'
import { includesSearchText, tryParseSearchAmountInCents } from '@/utils/search'
import AddTransactionDialog from '@/components/AddTransactionDialog.vue'
import MonthPickerNav from '@/components/MonthPickerNav.vue'
import { useRoute } from 'vue-router'

const snackbar = useSnackbarStore()
const route = useRoute()

const { currentMonth } = useMonthQueryParam({ storageKey: 'history' })
const search = ref('')
const categoryId = ref<number | null>(null)
const items = ref<HistoryItemDto[]>([])
const categories = ref<ExpenseCategoryDto[]>([])
const accounts = ref<AccountDto[]>([])
const loading = ref(false)
const accountLoading = ref(false)
const deleteItem = ref<HistoryItemDto | null>(null)
const dialogOpen = ref(false)

const monthLabel = computed(() =>
  currentMonth.value.toLocaleString('default', { month: 'long', year: 'numeric' }),
)

const categoryOptions = computed(() => [{ id: null, name: 'All' }, ...categories.value])
const filteredItems = computed(() => {
  const searchText = search.value.trim()
  if (!searchText) return items.value

  const normalizedSearchText = searchText.toLocaleLowerCase()
  const searchAmountInCents = tryParseSearchAmountInCents(searchText)
  const searchAmountAbs = searchAmountInCents === null ? null : Math.abs(searchAmountInCents)

  return items.value.filter((item) => {
    if (includesSearchText(item.description, normalizedSearchText)) return true
    if (searchAmountAbs === null) return false

    return item.details.some((detail) => Math.abs(detail.amount) === searchAmountAbs)
      || Math.abs(totalForItem(item)) === searchAmountAbs
  })
})

function monthGroupLabel(dateString: string) {
  return new Date(dateString).toLocaleDateString('default', { month: 'long', year: 'numeric' })
}

function isNewMonth(index: number) {
  if (index === 0) return true
  return monthGroupLabel(filteredItems.value[index].date) !== monthGroupLabel(filteredItems.value[index - 1].date)
}

async function fetchCategories() {
  try {
    categories.value = await apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories')
  } catch { /* ignore */ }
}

async function fetchHistory() {
  loading.value = true
  try {
    const month = `${formatMonth(currentMonth.value)}-01`
    const params = new URLSearchParams({ month })
    if (categoryId.value !== null) params.set('categoryId', String(categoryId.value))
    items.value = await apiClient.get<HistoryItemDto[]>(`/api/history?${params}`) ?? []
  } catch {
    snackbar.enqueueSnackbar('Failed to load history', { variant: 'error' })
  } finally {
    loading.value = false
  }
}

async function fetchAccountBalances() {
  accountLoading.value = true
  try {
    const month = `${formatMonth(currentMonth.value)}-01`
    const params = new URLSearchParams({ month })
    accounts.value = await apiClient.get<AccountDto[]>(`/api/accounts?${params}`) ?? []
  } catch {
    snackbar.enqueueSnackbar('Failed to load account balances', { variant: 'error' })
  } finally {
    accountLoading.value = false
  }
}

async function handleDelete() {
  if (!deleteItem.value) return
  try {
    await apiClient.delete(`/api/history/${deleteItem.value.id}`)
    snackbar.enqueueSnackbar('Transaction deleted', { variant: 'success' })
    deleteItem.value = null
    void Promise.all([fetchHistory(), fetchAccountBalances()])
  } catch {
    snackbar.enqueueSnackbar('Failed to delete transaction', { variant: 'error' })
  }
}

function totalForItem(item: HistoryItemDto) {
  return item.details.reduce((sum, d) => sum + d.amount, 0)
}

watch([currentMonth, categoryId], fetchHistory)
watch(currentMonth, fetchAccountBalances)

onMounted(() => {
  void fetchCategories()
  void fetchHistory()
  void fetchAccountBalances()

  const rawCategoryId = route.query.categoryId
  const categoryIdQuery = Array.isArray(rawCategoryId) ? rawCategoryId[0] : rawCategoryId
  const parsedCategoryId = Number(categoryIdQuery)
  if (Number.isFinite(parsedCategoryId)) {
    categoryId.value = parsedCategoryId
  }
})

function onDialogSuccess() {
  void Promise.all([fetchHistory(), fetchAccountBalances()])
  dialogOpen.value = false
}
</script>

<template>
  <div>
    <h5 class="text-h5 mb-4">Expenses</h5>

    <v-card class="pa-4 mb-4 d-flex flex-wrap align-center" style="gap: 16px;">
      <MonthPickerNav v-model="currentMonth" />

      <v-text-field label="Search" v-model="search" density="compact" style="max-width: 200px;" hide-details />

      <v-select
        label="Category"
        :items="categoryOptions"
        item-title="name"
        item-value="id"
        v-model="categoryId"
        density="compact"
        style="max-width: 200px;"
        hide-details
      />
    </v-card>

    <v-card class="pa-4 mb-4">
      <div class="text-subtitle-2 mb-2">Account balances through {{ monthLabel }}</div>
      <div v-if="accountLoading" class="d-flex justify-center py-2">
        <v-progress-circular size="20" indeterminate aria-label="Loading account balances" />
      </div>
      <div v-else-if="accounts.length === 0" class="text-medium-emphasis">No accounts found.</div>
      <div v-else class="d-flex flex-wrap" style="gap: 8px;">
        <v-chip v-for="account in accounts" :key="account.id" size="small" variant="outlined">
          {{ account.name ?? 'Unnamed account' }}: {{ formatCents(account.currentAmount) }}
        </v-chip>
      </div>
    </v-card>

    <div v-if="loading" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate aria-label="Loading history" />
    </div>
    <v-list v-else>
      <v-list-item v-if="filteredItems.length === 0">
        <v-list-item-title>No transactions found.</v-list-item-title>
      </v-list-item>
      <template v-for="(item, index) in filteredItems" :key="item.id">
        <v-list-subheader v-if="isNewMonth(index)" class="expense-month-group-header">
          {{ monthGroupLabel(item.date) }}
        </v-list-subheader>
        <v-card class="mb-2">
        <v-list-item>
          <v-list-item-title>
            <div class="expense-row">
              <span class="expense-main">
                <span class="expense-date">{{ new Date(item.date).toLocaleDateString() }}</span>
                <span class="expense-description">{{ item.description }}</span>
                <v-chip v-if="item.isTransfer" size="small">Transfer</v-chip>
                <v-btn
                  v-if="hasAmazonInDescription(item.description)"
                  icon="mdi-open-in-new"
                  variant="text"
                  size="x-small"
                  color="primary"
                  :href="AMAZON_TRANSACTIONS_URL"
                  target="_blank"
                  rel="noopener noreferrer"
                  aria-label="Open Amazon transactions page"
                />
              </span>
              <span class="font-weight-bold expense-amount">{{ formatCents(totalForItem(item)) }}</span>
            </div>
          </v-list-item-title>
          <v-list-item-subtitle>
            <div class="d-flex flex-wrap mt-1" style="gap: 4px;">
              <v-chip v-for="d in item.details" :key="d.id" size="small" variant="outlined">
                {{ d.categoryName }}: {{ formatCents(d.amount) }}
              </v-chip>
            </div>
          </v-list-item-subtitle>
          <template #append>
            <v-menu location="bottom end">
              <template #activator="{ props }">
                <v-btn
                  v-bind="props"
                  icon="mdi-dots-vertical"
                  variant="text"
                  size="small"
                  aria-label="Transaction actions"
                />
              </template>
              <v-list density="compact">
                <v-list-item
                  prepend-icon="mdi-delete"
                  title="Delete transaction"
                  base-color="error"
                  @click="deleteItem = item"
                />
              </v-list>
            </v-menu>
          </template>
        </v-list-item>
      </v-card>
      </template>
    </v-list>

    <v-btn
      icon="mdi-plus"
      color="primary"
      aria-label="Add Transaction"
      style="position: fixed; bottom: 32px; right: 32px; z-index: 100;"
      @click="dialogOpen = true"
    />

    <AddTransactionDialog v-model="dialogOpen" :categories="categories" @success="onDialogSuccess" />

    <v-dialog :model-value="!!deleteItem" max-width="480" @update:model-value="(val: boolean) => !val && (deleteItem = null)">
      <v-card>
        <v-card-title>Confirm Delete</v-card-title>
        <v-card-text>Delete transaction "{{ deleteItem?.description }}"?</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="deleteItem = null">Cancel</v-btn>
          <v-btn color="error" @click="handleDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<style scoped>
.expense-month-group-header {
  min-height: 22px;
  font-size: 0.75rem;
  font-weight: 600;
  color: rgba(var(--v-theme-on-surface), 0.65);
  padding-inline: 8px;
}

.expense-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;
}

.expense-main {
  min-width: 0;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
}

.expense-date {
  flex: 0 0 auto;
  font-size: 0.75rem;
  color: rgba(var(--v-theme-on-surface), 0.7);
}

.expense-description {
  min-width: 0;
  white-space: normal;
  overflow-wrap: anywhere;
}

.expense-amount {
  flex: 0 0 auto;
  text-align: right;
}

@media (max-width: 720px) {
  .expense-row {
    align-items: center;
  }

  .expense-main {
    display: grid;
    grid-template-columns: 1fr;
    align-items: start;
    gap: 4px;
  }

  .expense-description {
    grid-column: 1;
  }
}
</style>
