<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { HistoryItemDto, ExpenseCategoryDto, AccountDto } from '@/types'
import { formatCents, formatMonth } from '@/utils/currency'
import { useMonthQueryParam } from '@/composables/useMonthQueryParam'
import AddTransactionDialog from '@/components/AddTransactionDialog.vue'

const snackbar = useSnackbarStore()

const { currentMonth } = useMonthQueryParam()
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
    if (search.value) params.set('search', search.value)
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

function prevMonth() {
  const d = currentMonth.value
  currentMonth.value = new Date(d.getFullYear(), d.getMonth() - 1, 1)
}

function nextMonth() {
  const d = currentMonth.value
  currentMonth.value = new Date(d.getFullYear(), d.getMonth() + 1, 1)
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

watch([currentMonth, search, categoryId], fetchHistory)
watch(currentMonth, fetchAccountBalances)

onMounted(() => {
  void fetchCategories()
  void fetchHistory()
  void fetchAccountBalances()
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
      <v-btn variant="outlined" size="small" prepend-icon="mdi-chevron-left" @click="prevMonth">Prev</v-btn>
      <span style="min-width: 140px; text-align: center;">{{ monthLabel }}</span>
      <v-btn variant="outlined" size="small" append-icon="mdi-chevron-right" @click="nextMonth">Next</v-btn>

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
      <v-list-item v-if="items.length === 0">
        <v-list-item-title>No transactions found.</v-list-item-title>
      </v-list-item>
      <v-card v-for="item in items" :key="item.id" class="mb-2">
        <v-list-item>
          <v-list-item-title>
            <div class="d-flex justify-space-between align-center">
              <span>
                {{ new Date(item.date).toLocaleDateString() }} — {{ item.description }}
                <v-chip v-if="item.isTransfer" size="small" class="ml-2">Transfer</v-chip>
              </span>
              <span class="font-weight-bold">{{ formatCents(totalForItem(item)) }}</span>
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
            <v-btn icon="mdi-delete" variant="text" color="error" aria-label="Delete transaction" @click="deleteItem = item" />
          </template>
        </v-list-item>
      </v-card>
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
