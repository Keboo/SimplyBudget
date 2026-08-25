<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { ExpenseCategoryDto, PendingExpenseDto, ConvertPendingExpenseRequest } from '@/types'
import { formatCents } from '@/utils/currency'

const props = defineProps<{
  modelValue: boolean
  pendingExpense: PendingExpenseDto | null
  categories: ExpenseCategoryDto[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  success: []
}>()

const snackbar = useSnackbarStore()

interface LineItem {
  expenseCategoryId: number | null
  amount: string
}

const emptyLine = (): LineItem => ({ expenseCategoryId: null, amount: '' })

const description = ref('')
const date = ref('')
const lines = ref<LineItem[]>([emptyLine()])
const ignoreBudget = ref(false)
const submitting = ref(false)

function centsToDollars(cents: number) {
  return (cents / 100).toFixed(2)
}

function dollarsToCents(s: string) {
  return Math.round((parseFloat(s) || 0) * 100)
}

// Pre-fill the form whenever a new pending expense is opened for conversion:
// a single line item defaulting to the suggested category (if any) and the
// full amount, ready for the user to edit or split across categories.
watch(
  () => props.pendingExpense,
  pe => {
    if (!pe) return
    description.value = pe.description ?? ''
    date.value = pe.date.split('T')[0]
    ignoreBudget.value = false
    lines.value = [{
      expenseCategoryId: pe.suggestedCategoryId ?? null,
      amount: centsToDollars(pe.amount),
    }]
  },
  { immediate: true },
)

const sortedCategories = computed(() =>
  [...props.categories].sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '')),
)

const remainingCents = computed(() => {
  if (!props.pendingExpense) return 0
  const allocated = lines.value.reduce((sum, l) => sum + dollarsToCents(l.amount || '0'), 0)
  return props.pendingExpense.amount - allocated
})

function addLine() {
  lines.value.push(emptyLine())
}

function removeLine(index: number) {
  lines.value = lines.value.filter((_, i) => i !== index)
}

function close() {
  emit('update:modelValue', false)
}

async function submit() {
  if (!props.pendingExpense) return
  submitting.value = true
  try {
    const payload: ConvertPendingExpenseRequest = {
      description: description.value,
      date: date.value,
      version: props.pendingExpense.version,
      ignoreBudget: ignoreBudget.value,
      items: lines.value
        .filter(l => l.expenseCategoryId !== null && l.amount !== '')
        .map(l => ({
          expenseCategoryId: l.expenseCategoryId as number,
          amount: dollarsToCents(l.amount),
        })),
    }
    await apiClient.post(`/api/pending-expenses/${props.pendingExpense.id}/convert`, payload)
    snackbar.enqueueSnackbar('Pending expense converted', { variant: 'success' })
    emit('success')
    close()
  } catch (e: unknown) {
    snackbar.enqueueSnackbar(e instanceof Error ? e.message : 'Failed to convert pending expense', { variant: 'error' })
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-dialog :model-value="props.modelValue" max-width="600" @update:model-value="(val: boolean) => !val && close()">
    <v-card v-if="pendingExpense">
      <v-card-title>Convert Pending Expense</v-card-title>
      <v-card-text>
        <div class="d-flex flex-column" style="gap: 16px;">
          <v-text-field label="Description" v-model="description" />
          <v-text-field label="Date" type="date" v-model="date" />
          <v-checkbox
            v-model="ignoreBudget"
            label="Do not contribute toward budget"
            density="compact"
            hide-details
          />

          <span class="text-subtitle-2">
            {{ pendingExpense.isDebit ? 'Split expense across categories' : 'Split income across categories' }}
            (total {{ formatCents(pendingExpense.amount) }})
          </span>
          <div v-for="(item, index) in lines" :key="index" class="d-flex align-center mb-2" style="gap: 8px;">
            <v-select
              label="Category"
              :items="sortedCategories"
              item-title="name"
              item-value="id"
              v-model="item.expenseCategoryId"
              style="flex: 2;"
              density="compact"
            />
            <v-text-field
              label="Amount ($)"
              type="number"
              step="0.01"
              min="0"
              v-model="item.amount"
              style="flex: 1;"
              density="compact"
            />
            <v-btn v-if="lines.length > 1" icon="mdi-minus" size="small" variant="text" aria-label="Remove line" @click="removeLine(index)" />
          </div>
          <v-btn size="small" prepend-icon="mdi-plus" variant="text" @click="addLine">Add Line</v-btn>

          <span
            class="text-body-2"
            :class="remainingCents === 0 ? 'text-success' : 'text-warning'"
          >
            Remaining to allocate: {{ formatCents(remainingCents) }}
          </span>
        </div>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn @click="close">Cancel</v-btn>
        <v-btn color="primary" :loading="submitting" @click="submit">Apply</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
