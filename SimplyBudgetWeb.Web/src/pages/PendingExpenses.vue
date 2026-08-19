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
const newAssigneeName = ref('')
const addAssigneeOpen = ref(false)
const deleteAllOpen = ref(false)
const deletingAll = ref(false)

// "All" plus the real filter options; used for both the toolbar filter and the
// per-item assignee picker (which additionally needs an "Unassigned" option).
const assigneeFilterOptions = computed(() => [{ id: null, name: 'All' }, ...assignees.value])
const assigneeAssignOptions = computed(() => [{ id: null, name: 'Unassigned' }, ...assignees.value])

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
  try {
    await apiClient.put(`/api/pending-expenses/${item.id}`, {
      assigneeId: newAssigneeId,
      notes: item.notes,
    })
    void fetchPendingExpenses()
  } catch {
    snackbar.enqueueSnackbar('Failed to update assignee', { variant: 'error' })
  }
}

async function updateNotes(item: PendingExpenseDto) {
  try {
    await apiClient.put(`/api/pending-expenses/${item.id}`, {
      assigneeId: item.assigneeId,
      notes: item.notes,
    })
  } catch {
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

async function handleAddAssignee() {
  if (!newAssigneeName.value.trim()) return
  try {
    await apiClient.post('/api/assignees', { name: newAssigneeName.value })
    snackbar.enqueueSnackbar('Assignee added', { variant: 'success' })
    newAssigneeName.value = ''
    addAssigneeOpen.value = false
    void fetchAssignees()
  } catch {
    snackbar.enqueueSnackbar('Failed to add assignee', { variant: 'error' })
  }
}

function openConvert(item: PendingExpenseDto) {
  convertItem.value = item
  convertDialogOpen.value = true
}

function onConvertSuccess() {
  convertDialogOpen.value = false
  void fetchPendingExpenses()
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

      <v-btn variant="outlined" size="small" prepend-icon="mdi-account-plus" @click="addAssigneeOpen = true">
        Add Assignee
      </v-btn>

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
      <v-card v-for="item in items" :key="item.id" class="mb-2">
        <v-list-item style="cursor: pointer;" @click="openConvert(item)">
          <v-list-item-title>
            <div class="d-flex justify-space-between align-center">
              <span>
                {{ new Date(item.date).toLocaleDateString() }} — {{ item.description }}
                <v-chip size="small" class="ml-2">{{ item.isDebit ? 'Debit' : 'Credit' }}</v-chip>
                <v-chip v-if="item.suggestedCategoryName" size="small" variant="outlined" class="ml-1">
                  Suggested: {{ item.suggestedCategoryName }}
                </v-chip>
              </span>
              <span class="font-weight-bold">{{ formatCents(item.amount) }}</span>
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
        <v-card-text class="d-flex flex-wrap align-center" style="gap: 16px;" @click.stop>
          <v-select
            label="Assignee"
            :items="assigneeAssignOptions"
            item-title="name"
            item-value="id"
            :model-value="item.assigneeId"
            density="compact"
            style="max-width: 220px;"
            hide-details
            @update:model-value="(val: number | null) => updateAssignee(item, val)"
          />
          <v-text-field
            label="Notes"
            v-model="item.notes"
            density="compact"
            style="flex: 1; min-width: 200px;"
            hide-details
            @blur="updateNotes(item)"
          />
        </v-card-text>
      </v-card>
    </v-list>

    <ConvertPendingExpenseDialog
      v-model="convertDialogOpen"
      :pending-expense="convertItem"
      :categories="categories"
      @success="onConvertSuccess"
    />

    <v-dialog v-model="addAssigneeOpen" max-width="480">
      <v-card>
        <v-card-title>Add Assignee</v-card-title>
        <v-card-text>
          <v-text-field label="Name" v-model="newAssigneeName" @keyup.enter="handleAddAssignee" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="addAssigneeOpen = false">Cancel</v-btn>
          <v-btn color="primary" @click="handleAddAssignee">Add</v-btn>
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
