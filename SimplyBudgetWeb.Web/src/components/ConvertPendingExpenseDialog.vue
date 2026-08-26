<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { ExpenseCategoryDto, PendingExpenseDto, ConvertPendingExpenseRequest } from '@/types'
import { formatCents, dollarsToCents, centsToDollars, parseLocalDate } from '@/utils/currency'
import IncomeAllocationList from '@/components/IncomeAllocationList.vue'

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
  expenseCategoryId: number | string | null
  amount: string
}

const emptyLine = (): LineItem => ({ expenseCategoryId: null, amount: '' })

const description = ref('')
const date = ref('')
const lines = ref<LineItem[]>([emptyLine()])
const incomeAllocations = ref<Record<number, string>>({})
const ignoreBudget = ref(false)
const submitting = ref(false)
const calculatorOpen = ref(false)
const calculatorLineIndex = ref<number | null>(null)
const calculatorInput = ref('')
const calculatorItems = ref<number[]>([])
const calculatorAddTax = ref(false)

function isValidCategoryId(value: LineItem['expenseCategoryId']): value is number {
  return typeof value === 'number' && props.categories.some(category => category.id === value)
}

function isEmptyLine(line: LineItem) {
  return line.expenseCategoryId === null && dollarsToCents(line.amount) === 0
}

function isCompleteLine(line: LineItem): line is LineItem & { expenseCategoryId: number } {
  return isValidCategoryId(line.expenseCategoryId) && dollarsToCents(line.amount) > 0
}

// Pre-fill the form whenever a new pending expense is opened for conversion:
// a single line item defaulting to the suggested category (if any) and the
// full amount, ready for the user to edit or split across categories. Income
// (credit) items instead use the allocation list, seeded empty.
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
    incomeAllocations.value = {}
  },
  { immediate: true },
)

const sortedCategories = computed(() =>
  [...props.categories].sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '')),
)

const dateAsMonth = computed(() => parseLocalDate(date.value))

const incomeRemainingCents = computed(() => {
  if (!props.pendingExpense) return 0
  const allocated = Object.values(incomeAllocations.value).reduce((sum, amount) => sum + dollarsToCents(amount || '0'), 0)
  return props.pendingExpense.amount - allocated
})

const remainingCents = computed(() => {
  if (!props.pendingExpense) return 0
  if (!props.pendingExpense.isDebit) return incomeRemainingCents.value
  const allocated = lines.value.reduce((sum, l) => sum + dollarsToCents(l.amount || '0'), 0)
  return props.pendingExpense.amount - allocated
})

const hasPartialLines = computed(() =>
  lines.value.some(line => !isEmptyLine(line) && !isCompleteLine(line)),
)

const canSubmit = computed(() => {
  if (!props.pendingExpense) return false
  if (!props.pendingExpense.isDebit) return incomeRemainingCents.value === 0
  return remainingCents.value === 0
    && !hasPartialLines.value
    && lines.value.some(isCompleteLine)
})

const calculatorTotalCents = computed(() => {
  const subtotal = calculatorItems.value.reduce((sum, amount) => sum + amount, 0)
  return calculatorAddTax.value ? Math.round(subtotal * 1.091) : subtotal
})

watch(
  lines,
  currentLines => {
    if (!props.pendingExpense?.isDebit) return

    const nonEmptyLines = currentLines.filter(line => !isEmptyLine(line))
    const shouldAddEmptyLine = (
      remainingCents.value > 0
      && nonEmptyLines.length > 0
      && nonEmptyLines.every(isCompleteLine)
    )

    const nextLines = shouldAddEmptyLine
      ? [...nonEmptyLines, emptyLine()]
      : nonEmptyLines.length > 0
        ? nonEmptyLines
        : [emptyLine()]

    const didChange = (
      currentLines.length !== nextLines.length
      || currentLines.some((line, index) => {
        const nextLine = nextLines[index]
        return !nextLine
          || line.expenseCategoryId !== nextLine.expenseCategoryId
          || line.amount !== nextLine.amount
      })
    )

    if (didChange) {
      lines.value = nextLines
    }
  },
  { deep: true },
)

function removeLine(index: number) {
  lines.value = lines.value.filter((_, i) => i !== index)
}

function openCalculator(index: number) {
  calculatorLineIndex.value = index
  calculatorInput.value = ''
  calculatorItems.value = []
  calculatorAddTax.value = false
  calculatorOpen.value = true
}

function closeCalculator() {
  calculatorOpen.value = false
  calculatorLineIndex.value = null
}

function addCalculatorItem() {
  const amount = dollarsToCents(calculatorInput.value)
  if (amount <= 0) return
  calculatorItems.value.push(amount)
  calculatorInput.value = ''
}

function removeCalculatorItem(index: number) {
  calculatorItems.value.splice(index, 1)
}

function applyCalculator() {
  if (calculatorLineIndex.value === null) return
  const line = lines.value[calculatorLineIndex.value]
  if (!line) return
  line.amount = centsToDollars(calculatorTotalCents.value)
  closeCalculator()
}

function close() {
  closeCalculator()
  emit('update:modelValue', false)
}

async function submit() {
  if (!props.pendingExpense || !canSubmit.value) return
  submitting.value = true
  try {
    const items = props.pendingExpense.isDebit
      ? lines.value
        .filter(isCompleteLine)
        .map(l => ({
          expenseCategoryId: l.expenseCategoryId,
          amount: dollarsToCents(l.amount),
        }))
      : Object.entries(incomeAllocations.value)
        .map(([categoryId, amount]) => ({
          expenseCategoryId: Number(categoryId),
          amount: dollarsToCents(amount),
        }))
        .filter(item => item.amount > 0)

    const payload: ConvertPendingExpenseRequest = {
      description: description.value,
      date: date.value,
      version: props.pendingExpense.version,
      ignoreBudget: ignoreBudget.value,
      items,
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
      <v-card-text class="dialog-scroll-area">
        <div class="d-flex flex-column ga-3">
          <v-text-field label="Description" v-model="description" hide-details />
          <v-text-field label="Date" type="date" v-model="date" hide-details />
          <div class="d-flex align-center justify-space-between flex-wrap ga-2">
            <span class="text-subtitle-1 font-weight-medium">
              Total: {{ formatCents(pendingExpense.amount) }}
            </span>
            <v-checkbox
              v-model="ignoreBudget"
              label="Do not contribute toward budget"
              density="compact"
              hide-details
            />
          </div>

          <template v-if="pendingExpense.isDebit">
            <span class="text-subtitle-2">Split expense across categories</span>
            <div v-for="(item, index) in lines" :key="index" class="allocation-line d-flex align-center ga-1">
              <v-combobox
                label="Category"
                :items="sortedCategories"
                item-title="name"
                item-value="id"
                v-model="item.expenseCategoryId"
                :return-object="false"
                auto-select-first="exact"
                clearable
                hide-details
                :error="item.expenseCategoryId !== null && !isValidCategoryId(item.expenseCategoryId)"
                density="compact"
                class="category-field"
              />
              <div class="amount-field d-flex align-center ga-1">
                <v-text-field
                  label="Amount ($)"
                  type="number"
                  step="0.01"
                  min="0"
                  v-model="item.amount"
                  hide-details
                  density="compact"
                />
                <v-btn
                  icon="mdi-calculator"
                  size="small"
                  variant="text"
                  aria-label="Open amount calculator"
                  @click="openCalculator(index)"
                />
              </div>
              <v-btn
                v-if="lines.length > 1"
                icon="mdi-delete"
                size="small"
                variant="text"
                color="error"
                aria-label="Remove line"
                class="align-self-center"
                @click="removeLine(index)"
              />
            </div>

            <span v-if="hasPartialLines" class="text-body-2 text-error">
              Each line item must have a category and an amount greater than zero.
            </span>
          </template>

          <template v-else>
            <span class="text-subtitle-2">Allocate income to categories</span>
            <IncomeAllocationList
              :total-cents="pendingExpense.amount"
              :categories="sortedCategories"
              :month="dateAsMonth"
              v-model="incomeAllocations"
            />
          </template>
        </div>
      </v-card-text>
      <div class="px-4 pt-2">
        <span
          class="text-body-2"
          :class="remainingCents === 0 ? 'text-success' : 'text-warning'"
        >
          Remaining to allocate: {{ formatCents(remainingCents) }}
        </span>
      </div>
      <v-card-actions>
        <v-spacer />
        <v-btn @click="close">Cancel</v-btn>
        <v-btn color="primary" :loading="submitting" :disabled="!canSubmit" @click="submit">Apply</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>

  <v-dialog v-model="calculatorOpen" max-width="400">
    <v-card>
      <v-card-title>Amount Calculator</v-card-title>
      <v-card-text>
        <v-text-field
          v-model="calculatorInput"
          label="Amount ($)"
          type="number"
          step="0.01"
          min="0"
          autofocus
          hint="Press Enter to add"
          persistent-hint
          @keydown.enter.prevent="addCalculatorItem"
        />

        <v-list v-if="calculatorItems.length" density="compact" class="py-0">
          <v-list-item v-for="(amount, index) in calculatorItems" :key="index">
            <v-list-item-title>{{ formatCents(amount) }}</v-list-item-title>
            <template #append>
              <v-btn
                icon="mdi-delete"
                size="small"
                variant="text"
                color="error"
                :aria-label="`Remove ${formatCents(amount)}`"
                @click="removeCalculatorItem(index)"
              />
            </template>
          </v-list-item>
        </v-list>

        <v-checkbox
          v-model="calculatorAddTax"
          label="Add Tax"
          density="compact"
          hide-details
        />
        <div class="text-h6">Total: {{ formatCents(calculatorTotalCents) }}</div>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn @click="closeCalculator">Cancel</v-btn>
        <v-btn color="primary" :disabled="calculatorItems.length === 0" @click="applyCalculator">Apply</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.dialog-scroll-area {
  max-height: 60vh;
  overflow-y: auto;
}

.allocation-line {
  min-height: 40px;
}

.category-field {
  flex: 2 1 0;
}

.amount-field {
  flex: 1 1 180px;
}
</style>
