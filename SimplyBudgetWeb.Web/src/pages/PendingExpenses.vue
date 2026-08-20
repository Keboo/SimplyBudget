<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { PendingExpenseDto, AssigneeDto, ExpenseCategoryDto } from '@/types'
import { formatCents, formatMonth } from '@/utils/currency'
import ConvertPendingExpenseDialog from '@/components/ConvertPendingExpenseDialog.vue'

const snackbar = useSnackbarStore()

const now = new Date()
const currentMonth = ref(new Date(now.getFullYear(), now.getMonth(), 1))
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

// "All" plus the real filter options; used for both the toolbar filter and the
// per-item assignee picker (which additionally needs an "Unassigned" option).
const assigneeFilterOptions = computed(() => [{ id: null, name: 'All' }, ...assignees.value])
const assigneeAssignOptions = computed(() => [{ id: null, name: 'Unassigned' }, ...assignees.value])
const monthLabel = computed(() =>
  currentMonth.value.toLocaleString('default', { month: 'long', year: 'numeric' }),
)

// Items are returned sorted oldest-first, so consecutive entries belong to the same
// month unless this changes; used to show a divider between months in the list.
function monthGroupLabel(dateString: string) {
  return new Date(dateString).toLocaleDateString('default', { month: 'long', year: 'numeric' })
}

function isNewMonth(index: number) {
  if (index === 0) return true
  return monthGroupLabel(items.value[index].date) !== monthGroupLabel(items.value[index - 1].date)
}

function prevMonth() {
  const d = currentMonth.value
  currentMonth.value = new Date(d.getFullYear(), d.getMonth() - 1, 1)
}

function nextMonth() {
  const d = currentMonth.value
  currentMonth.value = new Date(d.getFullYear(), d.getMonth() + 1, 1)
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
      <v-btn variant="outlined" size="small" prepend-icon="mdi-chevron-left" @click="prevMonth">Prev</v-btn>
      <span style="min-width: 140px; text-align: center;">{{ monthLabel }}</span>
      <v-btn variant="outlined" size="small" append-icon="mdi-chevron-right" @click="nextMonth">Next</v-btn>

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
          {{ monthGroupLabel(item.date) }}
        </v-list-subheader>
        <v-card class="mb-2">
          <v-list-item style="cursor: pointer;" @click="openConvert(item)">
            <v-list-item-title>
              <div class="d-flex justify-space-between align-center">
                <span>
                  {{ new Date(item.date).toLocaleDateString() }} — {{ item.description }}
                  <v-chip v-if="item.suggestedCategoryName" size="small" variant="outlined" class="ml-1">
                    Suggested: {{ item.suggestedCategoryName }}
                  </v-chip>
                </span>
                <span
                  class="font-weight-bold"
                  :style="{ color: `rgb(var(--v-theme-${item.isDebit ? 'debit' : 'credit'}))` }"
                >{{ item.isDebit ? '' : '+' }}{{ formatCents(item.amount) }}</span>
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
