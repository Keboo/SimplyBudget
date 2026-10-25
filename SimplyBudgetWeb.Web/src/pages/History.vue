<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import { useDisplay } from 'vuetify'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { HistoryItemDto, HistoryItemUpdateRequest, ExpenseCategoryDto, AccountDto, AccountBalanceSnapshotDto, ReceiptDto } from '@/types'
import { useAuthStore } from '@/stores/auth'
import { formatCents, formatMonth } from '@/utils/currency'
import { useMonthQueryParam } from '@/composables/useMonthQueryParam'
import { useExternalLinkRules } from '@/utils/externalLinks'
import { includesSearchText, tryParseSearchAmountInCents } from '@/utils/search'
import AddTransactionDialog from '@/components/AddTransactionDialog.vue'
import MonthPickerNav from '@/components/MonthPickerNav.vue'
import { useRoute } from 'vue-router'
import CategorySelector from '@/components/CategorySelector.vue'
import AddRuleDialog from '@/components/AddRuleDialog.vue'
import { createMonthUpdatesHubClient } from '@/services/monthUpdatesHub'
import ReceiptPreview from '@/components/ReceiptPreview.vue'
import { useContextualLoad } from '@/composables/useContextualLoad'
import LoadFeedback from '@/components/LoadFeedback.vue'

const snackbar = useSnackbarStore()
const route = useRoute()
const authStore = useAuthStore()
const { xs: phone } = useDisplay()
const filtersOpen = ref(false)
const { loadExternalLinkRules, externalLinksFor } = useExternalLinkRules()

function parseCategoryIdQuery(value: unknown): number | null {
  const raw = Array.isArray(value) ? value[0] : value
  const parsedCategoryId = Number(raw)
  return Number.isInteger(parsedCategoryId) ? parsedCategoryId : null
}

const { currentMonth } = useMonthQueryParam()
const search = ref('')
const categoryId = ref<number | null>(parseCategoryIdQuery(route.query.categoryId))
const { data: items, loading, error: loadError, hasLoaded, load: loadHistory } = useContextualLoad<HistoryItemDto[]>([], 'Failed to load history')
const { data: categories, loading: categoriesLoading, error: categoriesError, hasLoaded: categoriesLoaded, load: loadCategories } = useContextualLoad<ExpenseCategoryDto[]>([], 'Failed to load categories')
const { data: accounts, loading: accountLoading, error: accountError, hasLoaded: accountsLoaded, load: loadAccounts } = useContextualLoad<AccountDto[]>([], 'Failed to load account balances')
const editNotesItem = ref<HistoryItemDto | null>(null)
const editNotesDraft = ref('')
const savingNotes = ref(false)
const deleteItem = ref<HistoryItemDto | null>(null)
const accountBalanceItem = ref<HistoryItemDto | null>(null)
const itemAccountBalances = ref<AccountBalanceSnapshotDto[]>([])
const itemAccountBalancesLoading = ref(false)
const itemAccountBalancesError = ref(false)
const dialogOpen = ref(false)
const addRuleOpen = ref(false)
const ruleSourceItem = ref<HistoryItemDto | null>(null)
const receiptDialogOpen = ref(false)
const receiptToView = ref<ReceiptDto | null>(null)
let realtimeRefreshInFlight = false
let realtimeRefreshQueued = false
let accountBalanceRequestId = 0

const monthLabel = computed(() =>
  currentMonth.value.toLocaleString('default', { month: 'long', year: 'numeric' }),
)

const filteredItems = computed(() => {
  const searchText = search.value.trim()
  if (!searchText) return items.value

  const normalizedSearchText = searchText.toLocaleLowerCase()
  const searchAmountInCents = tryParseSearchAmountInCents(searchText)
  const searchAmountAbs = searchAmountInCents === null ? null : Math.abs(searchAmountInCents)

  return items.value.filter((item) => {
    if (includesSearchText(item.description, normalizedSearchText)) return true
    if (includesSearchText(item.notes, normalizedSearchText)) return true
    if (item.details.some((detail) => includesSearchText(detail.categoryDescription, normalizedSearchText))) return true
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
  await loadCategories('categories', () => apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories'))
}

async function fetchHistory() {
  const month = `${formatMonth(currentMonth.value)}-01`
  const params = new URLSearchParams({ month })
  if (categoryId.value !== null) params.set('categoryId', String(categoryId.value))
  await loadHistory(params.toString(), async () => await apiClient.get<HistoryItemDto[]>(`/api/history?${params}`) ?? [])
}

async function fetchAccountBalances() {
  const month = `${formatMonth(currentMonth.value)}-01`
  const params = new URLSearchParams({ month })
  await loadAccounts(month, async () => await apiClient.get<AccountDto[]>(`/api/accounts?${params}`) ?? [])
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
      await Promise.all([fetchHistory(), fetchAccountBalances()])
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

async function openAccountBalances(item: HistoryItemDto) {
  const requestId = ++accountBalanceRequestId
  accountBalanceItem.value = item
  itemAccountBalances.value = []
  itemAccountBalancesError.value = false
  itemAccountBalancesLoading.value = true

  try {
    const balances = await apiClient.get<AccountBalanceSnapshotDto[]>(
      `/api/history/${item.id}/account-balances`,
    ) ?? []
    if (requestId === accountBalanceRequestId) {
      itemAccountBalances.value = balances
    }
  } catch {
    if (requestId === accountBalanceRequestId) {
      itemAccountBalancesError.value = true
      snackbar.enqueueSnackbar('Failed to load account balances for this transaction', { variant: 'error' })
    }
  } finally {
    if (requestId === accountBalanceRequestId) {
      itemAccountBalancesLoading.value = false
    }
  }
}

function openAddRule(item: HistoryItemDto) {
  ruleSourceItem.value = item
  addRuleOpen.value = true
}

function ruleCategoryIdFor(item: HistoryItemDto | null) {
  if (!item) return null
  // Only prefill when the transaction maps to a single category; multi-category
  // transactions have no obvious target for the rule.
  return item.details.length === 1 ? item.details[0].expenseCategoryId : null
}

function openNotesEditor(item: HistoryItemDto) {
  editNotesItem.value = item
  editNotesDraft.value = item.notes ?? ''
}

function openReceipt(receipt: ReceiptDto) {
  receiptToView.value = receipt
  receiptDialogOpen.value = true
}

function closeReceipt() {
  receiptDialogOpen.value = false
  receiptToView.value = null
}

function closeNotesEditor() {
  if (savingNotes.value) return
  editNotesItem.value = null
  editNotesDraft.value = ''
}

async function saveNotes() {
  if (!editNotesItem.value) return
  savingNotes.value = true
  try {
    const notesRaw = editNotesDraft.value ?? ''
    const payload: HistoryItemUpdateRequest = {
      notes: notesRaw.trim().length > 0 ? notesRaw.trim() : null,
    }
    const updated = await apiClient.put<HistoryItemDto>(`/api/history/${editNotesItem.value.id}`, payload)
    const index = items.value.findIndex(x => x.id === updated.id)
    if (index >= 0) {
      items.value[index] = updated
    }
    snackbar.enqueueSnackbar('Transaction notes updated', { variant: 'success' })
    closeNotesEditor()
  } catch {
    snackbar.enqueueSnackbar('Failed to update notes', { variant: 'error' })
  } finally {
    savingNotes.value = false
  }
}

watch([currentMonth, categoryId], () => {
  void fetchHistory()
})
watch(currentMonth, () => {
  void fetchAccountBalances()
})
watch(currentMonth, () => {
  void monthUpdatesHub.setMonth(formatMonth(currentMonth.value))
})

onBeforeUnmount(() => {
  void monthUpdatesHub.stop()
})

onMounted(() => {
  void fetchCategories()
  void fetchHistory()
  void fetchAccountBalances()
  void loadExternalLinkRules()
  void monthUpdatesHub.start(formatMonth(currentMonth.value))
})

function onDialogSuccess() {
  void Promise.all([fetchHistory(), fetchAccountBalances()])
  dialogOpen.value = false
}
</script>

<template>
  <div>
    <h5 class="text-h5 mb-4">Expenses</h5>

    <v-card class="history-toolbar pa-4 mb-4 d-flex flex-wrap align-center" style="gap: 16px;">
      <MonthPickerNav v-model="currentMonth" />

      <v-text-field label="Search" v-model="search" density="compact" style="max-width: 200px;" hide-details />

      <CategorySelector
        v-if="!phone"
        label="Category"
        :categories="categories"
        null-option-label="All"
        :clearable="false"
        v-model="categoryId"
        style="max-width: 200px;"
      />
      <v-btn v-if="phone" variant="outlined" min-height="44" prepend-icon="mdi-filter" @click="filtersOpen = true">Filters</v-btn>
    </v-card>
    <div v-if="search || categoryId !== null" class="active-filters d-flex flex-wrap align-center ga-2 mb-3" role="status">
      <span v-if="search">Search: {{ search }}</span>
      <span v-if="categoryId !== null">Category: {{ categories.find(category => category.id === categoryId)?.name ?? categoryId }}</span>
      <v-btn min-height="44" variant="text" @click="search = ''; categoryId = null">Clear filters</v-btn>
    </div>
    <v-dialog v-model="filtersOpen" max-width="440">
      <v-card title="Filter expenses">
        <v-card-text>
          <CategorySelector label="Category" :categories="categories" null-option-label="All" :clearable="false" v-model="categoryId" />
        </v-card-text>
        <v-card-actions>
          <v-btn min-height="44" @click="categoryId = null; search = ''">Clear filters</v-btn>
          <v-btn min-height="44" color="primary" @click="filtersOpen = false">Done</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <LoadFeedback label="account balances" :error="accountError" :loading="accountLoading" :stale="accountsLoaded" @retry="fetchAccountBalances" />
    <v-card class="pa-4 mb-4">
      <details :open="!phone" class="account-details">
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

    <LoadFeedback label="expenses" :error="loadError" :loading="loading" :stale="hasLoaded" @retry="fetchHistory" />
    <LoadFeedback label="categories" :error="categoriesError" :loading="categoriesLoading" :stale="categoriesLoaded" @retry="fetchCategories" />
    <div v-if="loading && !hasLoaded" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate aria-label="Loading history" />
    </div>
    <v-list v-else>
      <v-list-item v-if="hasLoaded && !loadError && filteredItems.length === 0">
        <div v-if="search || categoryId !== null">
          <p>No transactions match your filters.</p>
          <v-btn min-height="44" variant="text" @click="search = ''; categoryId = null">Clear filters</v-btn>
        </div>
        <div v-else>
          <p>No transactions for this month yet.</p>
          <v-btn min-height="44" variant="text" @click="dialogOpen = true">Add transaction</v-btn>
        </div>
      </v-list-item>
      <template v-for="(item, index) in filteredItems" :key="item.id">
        <v-list-subheader v-if="isNewMonth(index)" class="expense-month-group-header">
          {{ monthGroupLabel(item.date) }}
        </v-list-subheader>
        <v-card class="mb-2">
        <v-list-item class="history-item-clickable" @click="openAccountBalances(item)">
          <v-list-item-title>
            <div class="expense-row">
              <span class="expense-main">
                <span class="expense-date">{{ new Date(item.date).toLocaleDateString() }}</span>
                <span class="expense-description">{{ item.description }}</span>
                <v-chip v-if="item.isTransfer" size="small">Transfer</v-chip>
                <v-btn
                  v-for="link in externalLinksFor(item.description)"
                  :key="link.url"
                  icon="mdi-open-in-new"
                  variant="text"
                  size="x-small"
                  color="primary"
                  :href="link.url"
                  target="_blank"
                  rel="noopener noreferrer"
                  :aria-label="`Open ${link.name} link`"
                  :title="link.name"
                  @click.stop
                />
              </span>
              <span class="font-weight-bold expense-amount">{{ formatCents(totalForItem(item)) }}</span>
            </div>
          </v-list-item-title>
          <v-list-item-subtitle>
            <div v-if="item.notes" class="expense-notes mt-1">{{ item.notes }}</div>
            <div class="d-flex flex-wrap mt-1" style="gap: 4px;">
              <v-chip v-for="d in item.details" :key="d.id" size="small" variant="outlined">
                {{ d.categoryName }}: {{ formatCents(d.amount) }}
              </v-chip>
            </div>
            <v-btn
              v-if="item.receipt"
              variant="text"
              prepend-icon="mdi-receipt-text"
              class="mt-1"
              @click.stop="openReceipt(item.receipt)"
            >
              View receipt
            </v-btn>
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
                  @click.stop
                />
              </template>
              <v-list density="compact">
                <v-list-item
                  prepend-icon="mdi-note-edit-outline"
                  :title="item.notes ? 'Edit notes' : 'Add note'"
                  @click="openNotesEditor(item)"
                />
                <v-list-item
                  prepend-icon="mdi-filter-plus-outline"
                  title="Create rule"
                  @click="openAddRule(item)"
                />
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

    <v-dialog
      :model-value="!!accountBalanceItem"
      max-width="480"
      @update:model-value="(open: boolean) => !open && (accountBalanceItem = null)"
    >
      <v-card>
        <v-card-title>Account balances after transaction</v-card-title>
        <v-card-subtitle>
          {{ accountBalanceItem?.description ?? '(No description)' }}
          · {{ accountBalanceItem ? new Date(accountBalanceItem.date).toLocaleDateString() : '' }}
        </v-card-subtitle>
        <v-card-text>
          <div v-if="itemAccountBalancesLoading" class="d-flex justify-center py-2">
            <v-progress-circular size="20" indeterminate aria-label="Loading transaction account balances" />
          </div>
          <div v-else-if="itemAccountBalancesError" class="text-error">
            Could not load account balances.
          </div>
          <div v-else-if="itemAccountBalances.length === 0" class="text-medium-emphasis">
            No accounts found.
          </div>
          <v-list v-else density="compact">
            <v-list-item v-for="account in itemAccountBalances" :key="account.id">
              <v-list-item-title>{{ account.name ?? 'Unnamed account' }}</v-list-item-title>
              <template #append>{{ formatCents(account.currentAmount) }}</template>
            </v-list-item>
          </v-list>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="accountBalanceItem = null">Close</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <AddRuleDialog
      v-model="addRuleOpen"
      :categories="categories"
      :description="ruleSourceItem?.description"
      :notes="ruleSourceItem?.notes"
      :expense-category-id="ruleCategoryIdFor(ruleSourceItem)"
    />

    <v-dialog v-model="receiptDialogOpen" max-width="700" @update:model-value="(open: boolean) => !open && closeReceipt()">
      <v-card>
        <v-card-title class="text-wrap">{{ receiptToView?.merchantName ?? 'Receipt' }}</v-card-title>
        <v-card-text>
          <div v-if="receiptToView" class="text-body-2 mb-3">
            {{ receiptToView.transactionDate ? new Date(receiptToView.transactionDate).toLocaleDateString() : 'Date not set' }}
            · {{ receiptToView.totalAmountCents == null ? 'Total not set' : formatCents(receiptToView.totalAmountCents) }}
          </div>
          <ReceiptPreview v-if="receiptToView && receiptDialogOpen" :receipt-id="receiptToView.id" :image-geometry="receiptToView.imageGeometry" :title="receiptToView.merchantName || 'Linked receipt'" />
          <div v-if="receiptToView?.notes" class="mt-3 text-body-2">{{ receiptToView.notes }}</div>
          <div v-if="receiptToView?.lineItems.length" class="mt-2">
            <div v-for="line in receiptToView.lineItems" :key="line.id" class="d-flex justify-space-between ga-3 text-body-2">
              <span style="overflow-wrap: anywhere; min-width: 0;">{{ line.description }}</span>
              <span class="flex-shrink-0">{{ formatCents(line.amountCents) }}</span>
            </div>
          </div>
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="closeReceipt">Close</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog :model-value="!!editNotesItem" max-width="560" @update:model-value="(val: boolean) => !val && closeNotesEditor()">
      <v-card>
        <v-card-title>{{ editNotesItem?.notes ? 'Edit Notes' : 'Add Notes' }}</v-card-title>
        <v-card-text>
          <div class="text-body-2 mb-2 text-medium-emphasis">
            {{ editNotesItem?.description ?? '(No description)' }}
          </div>
          <v-textarea
            v-model="editNotesDraft"
            label="Notes"
            rows="4"
            auto-grow
            autofocus
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn :disabled="savingNotes" @click="closeNotesEditor">Cancel</v-btn>
          <v-btn color="primary" :loading="savingNotes" @click="saveNotes">Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

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
.account-details > summary {
  min-height: 44px;
  align-content: center;
  cursor: pointer;
}
.account-details :deep(.v-chip) {
  max-width: 100%;
  height: auto;
  min-height: 24px;
}
.account-details :deep(.v-chip__content) {
  white-space: normal;
  overflow-wrap: anywhere;
}
.active-filters {
  overflow-wrap: anywhere;
}
@media (max-width: 599px) {
  .history-toolbar {
    display: grid !important;
    grid-template-columns: minmax(0, 1fr) auto;
    gap: 12px !important;
  }
  .history-toolbar > .month-picker-nav {
    grid-column: 1 / -1;
  }
  .history-toolbar :deep(.v-input) {
    min-width: 0;
    max-width: none !important;
  }
}
.expense-month-group-header {
  min-height: 22px;
  font-size: 0.75rem;
  font-weight: 600;
  color: rgba(var(--v-theme-on-surface), 0.65);
  padding-inline: 8px;
}

.history-item-clickable {
  cursor: pointer;
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

.expense-notes {
  white-space: pre-wrap;
  overflow-wrap: anywhere;
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
