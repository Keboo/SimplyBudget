<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { PendingExpenseDto, AssigneeDto, ExpenseCategoryDto } from '@/types'
import { formatCents } from '@/utils/currency'
import ConvertPendingExpenseDialog from '@/components/ConvertPendingExpenseDialog.vue'

const snackbar = useSnackbarStore()

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
const editingNoteItemId = ref<number | null>(null)
const noteDrafts = ref<Record<number, string>>({})

// "All" plus the real filter options; used for both the toolbar filter and the
// page-level assignee filter.
const assigneeFilterOptions = computed(() => [{ id: null, name: 'All' }, ...assignees.value])

// Items are returned sorted oldest-first, so consecutive entries belong to the same
// month unless this changes; used to show a divider between months in the list.
function monthLabel(dateString: string) {
  return new Date(dateString).toLocaleDateString('default', { month: 'long', year: 'numeric' })
}

function isNewMonth(index: number) {
  if (index === 0) return true
  return monthLabel(items.value[index].date) !== monthLabel(items.value[index - 1].date)
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
    const params = new URLSearchParams()
    if (search.value) params.set('search', search.value)
    if (assigneeId.value !== null) params.set('assigneeId', String(assigneeId.value))
    const query = params.toString()
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
    })
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
    })
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
    await apiClient.delete(`/api/pending-expenses/${idToRemove}`)
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
    const params = new URLSearchParams()
    if (search.value) params.set('search', search.value)
    if (assigneeId.value !== null) params.set('assigneeId', String(assigneeId.value))
    const query = params.toString()
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

function openConvert(item: PendingExpenseDto) {
  convertItem.value = item
  convertDialogOpen.value = true
}

function onConvertSuccess() {
  convertDialogOpen.value = false
  if (convertItem.value) {
    const idToRemove = convertItem.value.id
    items.value = items.value.filter((item) => item.id !== idToRemove)
  }
  convertItem.value = null
}

watch([search, assigneeId], fetchPendingExpenses)

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
        <v-list-subheader v-if="isNewMonth(index)" class="font-weight-bold">
          {{ monthLabel(item.date) }}
        </v-list-subheader>
        <v-card class="mb-2">
          <v-list-item density="compact" style="cursor: pointer;" @click="openConvert(item)">
            <v-list-item-title>
              <div class="d-flex justify-space-between align-center" style="gap: 12px;">
                <span class="d-flex align-center flex-wrap" style="gap: 6px;">
                  {{ new Date(item.date).toLocaleDateString() }} — {{ item.description }}
                  <v-chip v-if="item.suggestedCategoryName" size="small" variant="outlined" class="ml-1">
                    Suggested: {{ item.suggestedCategoryName }}
                  </v-chip>
                </span>
                <div class="d-flex align-center" style="gap: 6px;" @click.stop>
                  <v-menu location="bottom end">
                    <template #activator="{ props }">
                      <v-chip
                        v-bind="props"
                        size="small"
                        variant="outlined"
                        :closable="item.assigneeId !== null"
                        :prepend-icon="item.assigneeId === null ? 'mdi-account-plus-outline' : undefined"
                        @click.stop
                        @click:close.stop="updateAssignee(item, null)"
                      >
                        {{ item.assigneeName ?? 'Assign' }}
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
                  <v-btn
                    icon="mdi-note-edit-outline"
                    variant="text"
                    size="x-small"
                    aria-label="Edit pending expense note"
                    :disabled="isEditingNote(item.id)"
                    @click.stop="startEditingNote(item)"
                  />
                  <span
                    class="font-weight-bold"
                    :style="{ color: `rgb(var(--v-theme-${item.isDebit ? 'debit' : 'credit'}))` }"
                  >{{ item.isDebit ? '' : '+' }}{{ formatCents(item.amount) }}</span>
                </div>
              </div>
            </v-list-item-title>
            <template #append>
              <v-btn
                icon="mdi-delete"
                variant="text"
                color="error"
                aria-label="Discard pending expense"
                @click.stop="discardItem = item"
              />
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
