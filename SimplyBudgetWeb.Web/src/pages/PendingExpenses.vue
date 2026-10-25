<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useDisplay } from 'vuetify'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { PendingExpenseDto, AssigneeDto, ExpenseCategoryDto } from '@/types'
import { formatCents, formatMonth } from '@/utils/currency'
import { useMonthQueryParam } from '@/composables/useMonthQueryParam'
import { useExternalLinkRules } from '@/utils/externalLinks'
import { includesSearchText, tryParseSearchAmountInCents } from '@/utils/search'
import ConvertPendingExpenseDialog from '@/components/ConvertPendingExpenseDialog.vue'
import MonthPickerNav from '@/components/MonthPickerNav.vue'
import AddRuleDialog from '@/components/AddRuleDialog.vue'
import { useContextualLoad } from '@/composables/useContextualLoad'
import LoadFeedback from '@/components/LoadFeedback.vue'

const snackbar = useSnackbarStore()
const { xs: phone } = useDisplay()
const reapplyConfirmOpen = ref(false)
const { loadExternalLinkRules, externalLinksFor } = useExternalLinkRules()

const { currentMonth } = useMonthQueryParam()
const search = ref('')
const assigneeId = ref<number | null>(null)
const { data: items, loading, error: loadError, hasLoaded, load: loadPending } = useContextualLoad<PendingExpenseDto[]>([], 'Failed to load pending expenses')
const { data: assignees, loading: assigneesLoading, error: assigneesError, hasLoaded: assigneesLoaded, load: loadAssignees } = useContextualLoad<AssigneeDto[]>([], 'Failed to load assignees')
const { data: categories, loading: categoriesLoading, error: categoriesError, hasLoaded: categoriesLoaded, load: loadCategories } = useContextualLoad<ExpenseCategoryDto[]>([], 'Failed to load categories')
const discardItem = ref<PendingExpenseDto | null>(null)
const convertItem = ref<PendingExpenseDto | null>(null)
const convertDialogOpen = ref(false)
const deleteAllOpen = ref(false)
const deletingAll = ref(false)
const reapplyingRules = ref(false)
const editingNoteItemId = ref<number | null>(null)
const noteDrafts = ref<Record<number, string>>({})
const addRuleOpen = ref(false)
const ruleSourceItem = ref<PendingExpenseDto | null>(null)

// "All" plus the real filter options; used for both the toolbar filter and the
// page-level assignee filter.
const assigneeFilterOptions = computed(() => [{ id: null, name: 'All' }, ...assignees.value])
const filteredItems = computed(() => {
  const searchText = search.value.trim()
  if (!searchText) return items.value

  const normalizedSearchText = searchText.toLocaleLowerCase()
  const searchAmountInCents = tryParseSearchAmountInCents(searchText)
  const searchAmountAbs = searchAmountInCents === null ? null : Math.abs(searchAmountInCents)

  return items.value.filter((item) => {
    if (includesSearchText(item.description, normalizedSearchText)) return true
    if (includesSearchText(item.suggestedCategoryDescription, normalizedSearchText)) return true
    if (searchAmountAbs === null) return false
    return Math.abs(item.amount) === searchAmountAbs
  })
})
// Items are returned sorted oldest-first, so consecutive entries belong to the same
// month unless this changes; used to show a divider between months in the list.
function monthGroupLabel(dateString: string) {
  return new Date(dateString).toLocaleDateString('default', { month: 'long', year: 'numeric' })
}

function isNewMonth(index: number) {
  if (index === 0) return true
  return monthGroupLabel(filteredItems.value[index].date) !== monthGroupLabel(filteredItems.value[index - 1].date)
}

function buildFilters() {
  const month = `${formatMonth(currentMonth.value)}-01`
  const params = new URLSearchParams({ month })
  if (assigneeId.value !== null) params.set('assigneeId', String(assigneeId.value))
  return params
}

async function fetchAssignees() {
  await loadAssignees('assignees', async () => await apiClient.get<AssigneeDto[]>('/api/assignees') ?? [])
}

async function fetchCategories() {
  await loadCategories('categories', async () => await apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories') ?? [])
}

async function fetchPendingExpenses() {
  const query = buildFilters().toString()
  await loadPending(query, async () => await apiClient.get<PendingExpenseDto[]>(`/api/pending-expenses${query ? `?${query}` : ''}`) ?? [])
}

async function updateAssignee(item: PendingExpenseDto, newAssigneeId: number | null) {
  const previousAssigneeId = item.assigneeId
  const previousAssigneeName = item.assigneeName
  const matchingAssignee = assignees.value.find((assignee) => assignee.id === newAssigneeId)
  item.assigneeId = newAssigneeId
  item.assigneeName = matchingAssignee?.name ?? null

  try {
    await apiClient.put(`/api/pending-expenses/${item.id}`, {
      assigneeId: newAssigneeId,
      notes: item.notes,
      version: item.version,
    })
    const refreshed = await apiClient.get<PendingExpenseDto>(`/api/pending-expenses/${item.id}`)
    item.version = refreshed.version
  } catch {
    item.assigneeId = previousAssigneeId
    item.assigneeName = previousAssigneeName
    snackbar.enqueueSnackbar('Failed to update assignee', { variant: 'error' })
  }
}

function startEditingNote(item: PendingExpenseDto) {
  editingNoteItemId.value = item.id
  noteDrafts.value[item.id] = item.notes ?? ''
}

function discardEditingNote(itemId: number) {
  if (editingNoteItemId.value === itemId) {
    editingNoteItemId.value = null
  }
  delete noteDrafts.value[itemId]
}

function hasNote(item: PendingExpenseDto) {
  return !!item.notes && item.notes.trim().length > 0
}

function isEditingNote(itemId: number) {
  return editingNoteItemId.value === itemId
}

async function saveNote(item: PendingExpenseDto) {
  const previousNote = item.notes
  const nextNoteRaw = noteDrafts.value[item.id] ?? ''
  const nextNote = nextNoteRaw.trim().length > 0 ? nextNoteRaw.trim() : null
  item.notes = nextNote

  try {
    await apiClient.put(`/api/pending-expenses/${item.id}`, {
      assigneeId: item.assigneeId,
      notes: nextNote,
      version: item.version,
    })
    const refreshed = await apiClient.get<PendingExpenseDto>(`/api/pending-expenses/${item.id}`)
    item.version = refreshed.version
    discardEditingNote(item.id)
  } catch {
    item.notes = previousNote
    snackbar.enqueueSnackbar('Failed to update notes', { variant: 'error' })
  }
}

async function handleDiscard() {
  if (!discardItem.value) return
  const idToRemove = discardItem.value.id
  try {
    await apiClient.delete(`/api/pending-expenses/${idToRemove}`, {
      ifMatch: discardItem.value.version,
    })
    snackbar.enqueueSnackbar('Pending expense discarded', { variant: 'success' })
    items.value = items.value.filter((item) => item.id !== idToRemove)
    discardItem.value = null
  } catch {
    snackbar.enqueueSnackbar('Failed to discard pending expense', { variant: 'error' })
  }
}

async function handleDeleteAll() {
  deletingAll.value = true
  try {
    const visibleItems = filteredItems.value
    const hasSearch = search.value.trim().length > 0

    if (hasSearch) {
      await Promise.all(visibleItems.map((item) => apiClient.delete(`/api/pending-expenses/${item.id}`, { ifMatch: item.version })))
      const visibleIds = new Set(visibleItems.map((item) => item.id))
      items.value = items.value.filter((item) => !visibleIds.has(item.id))
    } else {
      const query = buildFilters().toString()
      await apiClient.delete(`/api/pending-expenses${query ? `?${query}` : ''}`)
      items.value = []
    }

    snackbar.enqueueSnackbar('Pending expenses discarded', { variant: 'success' })
    deleteAllOpen.value = false
    void fetchPendingExpenses()
  } catch {
    snackbar.enqueueSnackbar('Failed to discard pending expenses', { variant: 'error' })
    void fetchPendingExpenses()
  } finally {
    deletingAll.value = false
  }
}

async function handleReapplyRules() {
  reapplyingRules.value = true
  try {
    await apiClient.post('/api/pending-expenses/reapply-rules')
    snackbar.enqueueSnackbar('Pending expense rules re-run', { variant: 'success' })
    await fetchPendingExpenses()
  } catch {
    snackbar.enqueueSnackbar('Failed to re-run pending expense rules', { variant: 'error' })
  } finally {
    reapplyingRules.value = false
  }
}

function openConvert(item: PendingExpenseDto) {
  convertItem.value = item
  convertDialogOpen.value = true
}

function openAddRule(item: PendingExpenseDto) {
  ruleSourceItem.value = item
  addRuleOpen.value = true
}

watch(addRuleOpen, (isOpen) => {
  if (!isOpen) {
    ruleSourceItem.value = null
  }
})

function onConvertSuccess() {
  convertDialogOpen.value = false
  if (convertItem.value) {
    const idToRemove = convertItem.value.id
    items.value = items.value.filter((item) => item.id !== idToRemove)
  }
  convertItem.value = null
}

function onConvertNoteSaved(updated: PendingExpenseDto) {
  const existing = items.value.find((item) => item.id === updated.id)
  if (existing) {
    Object.assign(existing, updated)
  }
}

watch([currentMonth, assigneeId], fetchPendingExpenses)

onMounted(() => {
  void fetchAssignees()
  void fetchCategories()
  void fetchPendingExpenses()
  void loadExternalLinkRules()
})
</script>

<template>
  <div>
    <h5 class="text-h5 mb-4">Pending Expenses</h5>

    <v-card class="pa-4 mb-4 d-flex flex-wrap align-center" style="gap: 16px;">
      <MonthPickerNav v-model="currentMonth" />

      <v-text-field label="Search" v-model="search" density="compact" style="max-width: 200px;" hide-details />

      <v-select
        label="Assignee"
        :items="assigneeFilterOptions"
        item-title="name"
        item-value="id"
        v-model="assigneeId"
        density="compact"
        style="max-width: 200px;"
        hide-details
      />

      <v-spacer />

      <v-menu v-if="phone" location="bottom end">
        <template #activator="{ props }">
          <v-btn v-bind="props" variant="outlined" min-height="44" append-icon="mdi-menu-down">Manage</v-btn>
        </template>
        <v-list>
          <v-list-item title="Re-run Rules" :disabled="reapplyingRules" @click="reapplyConfirmOpen = true" />
          <v-list-item title="Delete All" :disabled="filteredItems.length === 0" @click="deleteAllOpen = true" />
        </v-list>
      </v-menu>
      <v-btn
        v-if="!phone"
        variant="outlined"
        size="small"
        prepend-icon="mdi-refresh"
        :loading="reapplyingRules"
        :disabled="reapplyingRules"
        @click="handleReapplyRules"
      >
        Re-run Rules
      </v-btn>

      <v-btn
        v-if="!phone"
        variant="outlined"
        size="small"
        color="error"
        prepend-icon="mdi-delete-sweep"
        :disabled="filteredItems.length === 0"
        @click="deleteAllOpen = true"
      >
        Delete All
      </v-btn>
    </v-card>

    <LoadFeedback label="pending expenses" :error="loadError" :loading="loading" :stale="hasLoaded" @retry="fetchPendingExpenses" />
    <LoadFeedback label="assignees" :error="assigneesError" :loading="assigneesLoading" :stale="assigneesLoaded" @retry="fetchAssignees" />
    <LoadFeedback label="categories" :error="categoriesError" :loading="categoriesLoading" :stale="categoriesLoaded" @retry="fetchCategories" />
    <div v-if="loading && !hasLoaded" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate aria-label="Loading pending expenses" />
    </div>
    <v-list v-else>
      <v-list-item v-if="hasLoaded && !loadError && filteredItems.length === 0">
        <div v-if="search || assigneeId !== null">
          <p>No pending expenses match your filters.</p>
          <v-btn min-height="44" variant="text" @click="search = ''; assigneeId = null">Clear filters</v-btn>
        </div>
        <div v-else>
          <p>No pending expenses for this month.</p>
          <v-btn to="/import" min-height="44" variant="text">Import transactions</v-btn>
        </div>
      </v-list-item>
      <template v-for="(item, index) in filteredItems" :key="item.id">
        <v-list-subheader v-if="isNewMonth(index)" class="pending-month-group-header">
          {{ monthGroupLabel(item.date) }}
        </v-list-subheader>
        <v-card class="mb-2">
          <v-card-text v-if="phone" class="pending-phone">
            <div class="pending-merchant-amount">
              <div class="pending-description font-weight-medium">{{ item.description }}</div>
              <strong class="pending-amount" :class="item.isDebit ? 'text-debit' : 'text-credit'">
                {{ item.isDebit ? '' : '+' }}{{ formatCents(item.amount) }}
              </strong>
            </div>
            <div class="pending-context text-body-2">
              <span>{{ new Date(item.date).toLocaleDateString() }}</span>
              <span v-if="item.suggestedCategoryName">Suggested: {{ item.suggestedCategoryName }}</span>
            </div>
            <v-menu location="bottom">
              <template #activator="{ props }">
                <v-btn v-bind="props" variant="tonal" class="pending-assignment text-none" min-height="44" prepend-icon="mdi-account-outline">
                  <span>Assigned to: {{ item.assigneeName ?? 'Unassigned' }}</span>
                </v-btn>
              </template>
              <v-list aria-label="Assign pending expense">
                <v-list-item title="Unassign" :disabled="item.assigneeId === null" @click="updateAssignee(item, null)" />
                <v-list-item v-for="assignee in assignees" :key="assignee.id" :title="assignee.name ?? 'Unnamed'" @click="updateAssignee(item, assignee.id)" />
              </v-list>
            </v-menu>
            <div class="d-flex flex-wrap ga-2">
              <v-btn color="primary" variant="flat" min-height="44" @click="openConvert(item)">Review / Apply</v-btn>
              <v-menu location="bottom end">
                <template #activator="{ props }">
                  <v-btn v-bind="props" variant="text" min-height="44" aria-label="Pending expense actions" append-icon="mdi-menu-down">More</v-btn>
                </template>
                <v-list>
                  <v-list-item title="Edit note" @click="startEditingNote(item)" />
                  <v-list-item title="Create rule" @click="openAddRule(item)" />
                  <v-list-item title="Discard pending expense" base-color="error" @click="discardItem = item" />
                </v-list>
              </v-menu>
              <v-btn v-for="link in externalLinksFor(item.description)" :key="link.url" icon="mdi-open-in-new" variant="text" min-height="44" min-width="44" :href="link.url" target="_blank" rel="noopener noreferrer" :aria-label="`Open ${link.name} link`" />
            </div>
          </v-card-text>
          <v-list-item v-else density="compact" style="cursor: pointer;" @click="openConvert(item)">
            <v-list-item-title>
              <div class="pending-row">
                <span class="pending-main">
                  <span class="pending-date">{{ new Date(item.date).toLocaleDateString() }}</span>
                  <span class="pending-description">{{ item.description }}</span>
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
                  <v-chip v-if="item.suggestedCategoryName" size="small" variant="outlined" class="ml-1">
                    Suggested: {{ item.suggestedCategoryName }}
                  </v-chip>
                </span>
                <div class="d-flex align-center pending-right" style="gap: 6px;" @click.stop>
                  <v-menu location="bottom end">
                    <template #activator="{ props }">
                      <v-chip
                        v-bind="props"
                        size="small"
                        variant="outlined"
                        :prepend-icon="item.assigneeId === null ? 'mdi-account-plus-outline' : undefined"
                        @click.stop
                      >
                        {{ item.assigneeName ?? 'Assign' }}
                        <template v-if="item.assigneeId !== null" #append>
                          <v-icon
                            icon="mdi-close-circle"
                            size="x-small"
                            class="ms-1"
                            @click.stop.prevent="updateAssignee(item, null)"
                          />
                        </template>
                      </v-chip>
                    </template>

                    <v-list density="compact">
                      <v-list-item v-if="assignees.length === 0" title="No assignees available" disabled />
                      <v-list-item
                        v-for="assignee in assignees"
                        :key="assignee.id"
                        :title="assignee.name ?? 'Unnamed'"
                        @click="updateAssignee(item, assignee.id)"
                      />
                    </v-list>
                  </v-menu>
                  <span
                    class="font-weight-bold"
                    :style="{ color: `rgb(var(--v-theme-${item.isDebit ? 'debit' : 'credit'}))` }"
                  >{{ item.isDebit ? '' : '+' }}{{ formatCents(item.amount) }}</span>
                </div>
              </div>
            </v-list-item-title>
            <template #append>
              <v-menu location="bottom end">
                <template #activator="{ props }">
                  <v-btn
                    v-bind="props"
                    icon="mdi-dots-vertical"
                    variant="text"
                    size="small"
                    aria-label="Pending expense actions"
                    @click.stop
                  />
                </template>
                <v-list density="compact">
                  <v-list-item
                    prepend-icon="mdi-check-circle-outline"
                    title="Apply Expense"
                    @click="openConvert(item)"
                  />
                  <v-list-item
                    prepend-icon="mdi-note-edit-outline"
                    title="Edit note"
                    :disabled="isEditingNote(item.id)"
                    @click="startEditingNote(item)"
                  />
                  <v-list-item
                    prepend-icon="mdi-filter-plus-outline"
                    title="Create rule"
                    @click="openAddRule(item)"
                  />
                  <v-list-item
                    prepend-icon="mdi-delete"
                    title="Discard pending expense"
                    base-color="error"
                    @click="discardItem = item"
                  />
                </v-list>
              </v-menu>
            </template>
          </v-list-item>
          <v-expand-transition>
            <div v-if="isEditingNote(item.id) || hasNote(item)" class="pending-note px-4 pb-3 pt-1" @click.stop>
              <div v-if="isEditingNote(item.id)" class="d-flex flex-wrap align-center" style="gap: 8px;">
                <v-text-field
                  :model-value="noteDrafts[item.id]"
                  label="Notes"
                  density="compact"
                  style="flex: 1; min-width: 220px;"
                  hide-details
                  @update:model-value="(val: string) => (noteDrafts[item.id] = val)"
                />
                <v-btn size="small" color="primary" @click="saveNote(item)">Save</v-btn>
                <v-btn size="small" variant="text" @click="discardEditingNote(item.id)">Discard</v-btn>
              </div>
              <div v-else class="text-body-2 text-medium-emphasis">{{ item.notes }}</div>
            </div>
          </v-expand-transition>
        </v-card>
      </template>
    </v-list>

    <ConvertPendingExpenseDialog
      v-model="convertDialogOpen"
      :pending-expense="convertItem"
      :categories="categories"
      @success="onConvertSuccess"
      @note-saved="onConvertNoteSaved"
    />
    <AddRuleDialog
      v-model="addRuleOpen"
      :categories="categories"
      :description="ruleSourceItem?.description"
      :notes="ruleSourceItem?.notes"
      :expense-category-id="ruleSourceItem?.suggestedCategoryId ?? null"
    />

    <v-dialog :model-value="!!discardItem" max-width="480" @update:model-value="(val: boolean) => !val && (discardItem = null)">
      <v-card>
        <v-card-title>Confirm Discard</v-card-title>
        <v-card-text>Discard pending expense "{{ discardItem?.description }}"? This cannot be undone.</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="discardItem = null">Cancel</v-btn>
          <v-btn color="error" @click="handleDiscard">Discard</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
    <v-dialog v-model="deleteAllOpen" max-width="480">
      <v-card>
        <v-card-title>Confirm Delete All</v-card-title>
        <v-card-text>
          Discard all {{ filteredItems.length }} pending expense{{ filteredItems.length === 1 ? '' : 's' }}
          <template v-if="search || assigneeId !== null">matching the current filters</template>?
          This cannot be undone.
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="deleteAllOpen = false" :disabled="deletingAll">Cancel</v-btn>
          <v-btn color="error" @click="handleDeleteAll" :loading="deletingAll">Delete All</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
    <v-dialog v-model="reapplyConfirmOpen" max-width="480">
      <v-card title="Re-run pending expense rules?">
        <v-card-text>Reapply your current rules to pending expenses. Suggested categories and notes may change.</v-card-text>
        <v-card-actions>
          <v-btn min-height="44" @click="reapplyConfirmOpen = false">Cancel</v-btn>
          <v-btn min-height="44" color="primary" @click="reapplyConfirmOpen = false; handleReapplyRules()">Re-run Rules</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>

<style scoped>
.pending-month-group-header {
  min-height: 22px;
  font-size: 0.75rem;
  font-weight: 600;
  color: rgba(var(--v-theme-on-surface), 0.65);
  padding-inline: 8px;
}

.pending-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 10px;
}

.pending-main {
  min-width: 0;
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
}

.pending-date {
  flex: 0 0 auto;
  font-size: 0.75rem;
  color: rgba(var(--v-theme-on-surface), 0.7);
}

.pending-description {
  min-width: 0;
  white-space: normal;
  overflow-wrap: anywhere;
}

.pending-right {
  flex: 0 0 auto;
}

.pending-phone {
  display: flex;
  flex-direction: column;
  gap: 12px;
}
.pending-merchant-amount {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto;
  gap: 12px;
  align-items: start;
}
.pending-amount {
  white-space: nowrap;
}
.pending-context {
  display: flex;
  flex-direction: column;
  gap: 4px;
  overflow-wrap: anywhere;
}
.pending-assignment {
  width: 100%;
  height: auto !important;
  padding-block: 10px;
}
.pending-assignment :deep(.v-btn__content) {
  white-space: normal;
  overflow-wrap: anywhere;
  min-width: 0;
}
.pending-phone :deep(.v-btn:focus-visible) {
  outline: 2px solid currentColor;
  outline-offset: 2px;
}

@media (max-width: 720px) {
  .pending-row {
    align-items: center;
  }

  .pending-main {
    display: grid;
    grid-template-columns: 1fr;
    align-items: start;
    gap: 4px;
  }

  .pending-description {
    grid-column: 1;
  }
}

@media (max-width: 599px) {
  .pending-note :deep(.v-input) {
    min-width: 0 !important;
    flex: 1 1 100% !important;
  }
  .pending-note :deep(.v-btn) {
    min-width: 44px;
    min-height: 44px;
  }
}
</style>
