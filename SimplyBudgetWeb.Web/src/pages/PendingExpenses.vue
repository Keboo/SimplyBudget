<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { PendingExpenseDto, AssigneeDto, ExpenseCategoryDto } from '@/types'
import { formatCents, formatMonth } from '@/utils/currency'
import { useMonthQueryParam } from '@/composables/useMonthQueryParam'
import { AMAZON_TRANSACTIONS_URL, hasAmazonInDescription } from '@/utils/merchantLinks'
import ConvertPendingExpenseDialog from '@/components/ConvertPendingExpenseDialog.vue'
import MonthPickerNav from '@/components/MonthPickerNav.vue'

const snackbar = useSnackbarStore()

const { currentMonth } = useMonthQueryParam({ storageKey: 'pending-expenses' })
const search = ref('')
const assigneeId = ref<number | null>(null)
const items = ref<PendingExpenseDto[]>([])
const assignees = ref<AssigneeDto[]>([])
const categories = ref<ExpenseCategoryDto[]>([])
const loading = ref(false)
const discardItem = ref<PendingExpenseDto | null>(null)
const convertItem = ref<PendingExpenseDto | null>(null)
const convertDialogOpen = ref(false)
const deleteAllOpen = ref(false)
const deletingAll = ref(false)
const reapplyingRules = ref(false)
const editingNoteItemId = ref<number | null>(null)
const noteDrafts = ref<Record<number, string>>({})
const addRuleOpen = ref(false)
const ruleForm = ref({ name: '', ruleRegex: '', expenseCategoryId: null as number | null })

// "All" plus the real filter options; used for both the toolbar filter and the
// page-level assignee filter.
const assigneeFilterOptions = computed(() => [{ id: null, name: 'All' }, ...assignees.value])
const ruleCategoryOptions = computed(() => [{ id: null, name: 'None' }, ...categories.value])

// Items are returned sorted oldest-first, so consecutive entries belong to the same
// month unless this changes; used to show a divider between months in the list.
function monthGroupLabel(dateString: string) {
  return new Date(dateString).toLocaleDateString('default', { month: 'long', year: 'numeric' })
}

function isNewMonth(index: number) {
  if (index === 0) return true
  return monthGroupLabel(items.value[index].date) !== monthGroupLabel(items.value[index - 1].date)
}

function buildFilters() {
  const month = `${formatMonth(currentMonth.value)}-01`
  const params = new URLSearchParams({ month })
  if (search.value) params.set('search', search.value)
  if (assigneeId.value !== null) params.set('assigneeId', String(assigneeId.value))
  return params
}

async function fetchAssignees() {
  try {
    assignees.value = await apiClient.get<AssigneeDto[]>('/api/assignees') ?? []
  } catch { /* ignore */ }
}

async function fetchCategories() {
  try {
    categories.value = await apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories') ?? []
  } catch { /* ignore */ }
}

async function fetchPendingExpenses() {
  loading.value = true
  try {
    const query = buildFilters().toString()
    items.value = await apiClient.get<PendingExpenseDto[]>(`/api/pending-expenses${query ? `?${query}` : ''}`) ?? []
  } catch {
    snackbar.enqueueSnackbar('Failed to load pending expenses', { variant: 'error' })
  } finally {
    loading.value = false
  }
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
    const query = buildFilters().toString()
    await apiClient.delete(`/api/pending-expenses${query ? `?${query}` : ''}`)
    snackbar.enqueueSnackbar('Pending expenses discarded', { variant: 'success' })
    deleteAllOpen.value = false
    void fetchPendingExpenses()
  } catch {
    snackbar.enqueueSnackbar('Failed to discard pending expenses', { variant: 'error' })
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

function resetRuleForm() {
  ruleForm.value = { name: '', ruleRegex: '', expenseCategoryId: null }
}

function openAddRule(item: PendingExpenseDto) {
  resetRuleForm()
  ruleForm.value.ruleRegex = item.description ?? ''
  ruleForm.value.expenseCategoryId = item.suggestedCategoryId ?? null
  addRuleOpen.value = true
}

function closeAddRule() {
  addRuleOpen.value = false
}

async function handleAddRule() {
  try {
    await apiClient.post('/api/rules', {
      name: ruleForm.value.name,
      ruleRegex: ruleForm.value.ruleRegex,
      expenseCategoryId: ruleForm.value.expenseCategoryId,
    })
    snackbar.enqueueSnackbar('Rule added', { variant: 'success' })
    addRuleOpen.value = false
  } catch {
    snackbar.enqueueSnackbar('Failed to add rule', { variant: 'error' })
  }
}

watch(addRuleOpen, (isOpen) => {
  if (!isOpen) {
    resetRuleForm()
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

watch([currentMonth, search, assigneeId], fetchPendingExpenses)

onMounted(() => {
  void fetchAssignees()
  void fetchCategories()
  void fetchPendingExpenses()
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

      <v-btn
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
        variant="outlined"
        size="small"
        color="error"
        prepend-icon="mdi-delete-sweep"
        :disabled="items.length === 0"
        @click="deleteAllOpen = true"
      >
        Delete All
      </v-btn>
    </v-card>

    <div v-if="loading" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate aria-label="Loading pending expenses" />
    </div>
    <v-list v-else>
      <v-list-item v-if="items.length === 0">
        <v-list-item-title>No pending expenses found.</v-list-item-title>
      </v-list-item>
      <template v-for="(item, index) in items" :key="item.id">
        <v-list-subheader v-if="isNewMonth(index)" class="pending-month-group-header">
          {{ monthGroupLabel(item.date) }}
        </v-list-subheader>
        <v-card class="mb-2">
          <v-list-item density="compact" style="cursor: pointer;" @click="openConvert(item)">
            <v-list-item-title>
              <div class="pending-row">
                <span class="pending-main">
                  <span class="pending-date">{{ new Date(item.date).toLocaleDateString() }}</span>
                  <span class="pending-description">{{ item.description }}</span>
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
                    v-if="hasAmazonInDescription(item.description)"
                    prepend-icon="mdi-open-in-new"
                    title="Open Amazon transactions page"
                    :href="AMAZON_TRANSACTIONS_URL"
                    target="_blank"
                    rel="noopener noreferrer"
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
            <div v-if="isEditingNote(item.id) || hasNote(item)" class="px-4 pb-3 pt-1" @click.stop>
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
    />
    <v-dialog :model-value="addRuleOpen" max-width="500" @update:model-value="(val: boolean) => !val && closeAddRule()">
      <v-card>
        <v-card-title>Add Rule</v-card-title>
        <v-card-text class="d-flex flex-column" style="gap: 16px;">
          <v-text-field label="Name" v-model="ruleForm.name" />
          <v-text-field label="Regex Pattern" v-model="ruleForm.ruleRegex" />
          <v-select label="Target Category" :items="ruleCategoryOptions" item-title="name" item-value="id" v-model="ruleForm.expenseCategoryId" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="closeAddRule">Cancel</v-btn>
          <v-btn color="primary" @click="handleAddRule">Add</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

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
          Discard all {{ items.length }} pending expense{{ items.length === 1 ? '' : 's' }}
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
</style>
