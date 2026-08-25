<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { ExpenseCategoryDto, TransactionRequest, TransferRequest } from '@/types'
import { formatCents, dollarsToCents, centsToDollars, parseLocalDate } from '@/utils/currency'
import IncomeAllocationList from '@/components/IncomeAllocationList.vue'

const props = defineProps<{
  modelValue: boolean
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

const tab = ref<'transaction' | 'income' | 'transfer'>('transaction')
const description = ref('')
const date = ref(new Date().toISOString().split('T')[0])
const lines = ref<LineItem[]>([emptyLine()])
const incomeTotal = ref('')
const incomeAllocations = ref<Record<number, string>>({})
const transferAmount = ref('')
const fromCategoryId = ref<number | null>(null)
const toCategoryId = ref<number | null>(null)
const submitting = ref(false)
const calculatorOpen = ref(false)
const calculatorLineIndex = ref<number | null>(null)
const calculatorInput = ref('')
const calculatorItems = ref<number[]>([])
const calculatorAddTax = ref(false)

function removeLine(index: number) {
  lines.value = lines.value.filter((_, i) => i !== index)
}

function resetForm() {
  description.value = ''
  date.value = new Date().toISOString().split('T')[0]
  lines.value = [emptyLine()]
  incomeTotal.value = ''
  incomeAllocations.value = {}
  transferAmount.value = ''
  fromCategoryId.value = null
  toCategoryId.value = null
  tab.value = 'transaction'
}

function close() {
  closeCalculator()
  resetForm()
  emit('update:modelValue', false)
}

function isValidCategoryId(value: LineItem['expenseCategoryId']): value is number {
  return typeof value === 'number' && props.categories.some(category => category.id === value)
}

function isEmptyLine(line: LineItem) {
  return line.expenseCategoryId === null && dollarsToCents(line.amount) === 0
}

function isCompleteLine(line: LineItem): line is LineItem & { expenseCategoryId: number } {
  return isValidCategoryId(line.expenseCategoryId) && dollarsToCents(line.amount) > 0
}

const hasPartialLines = computed(() =>
  lines.value.some(line => !isEmptyLine(line) && !isCompleteLine(line)),
)

const canSubmitLines = computed(() =>
  !hasPartialLines.value && lines.value.some(isCompleteLine),
)

const incomeTotalCents = computed(() => dollarsToCents(incomeTotal.value))

const incomeAllocatedCents = computed(() =>
  Object.values(incomeAllocations.value).reduce((sum, amount) => sum + dollarsToCents(amount || '0'), 0),
)

const incomeRemainingCents = computed(() => incomeTotalCents.value - incomeAllocatedCents.value)

const canSubmitIncome = computed(() =>
  incomeTotalCents.value > 0 && incomeRemainingCents.value === 0,
)

const dateAsMonth = computed(() => parseLocalDate(date.value))

const calculatorTotalCents = computed(() => {
  const subtotal = calculatorItems.value.reduce((sum, amount) => sum + amount, 0)
  return calculatorAddTax.value ? Math.round(subtotal * 1.091) : subtotal
})

watch(
  lines,
  currentLines => {
    if (
      tab.value === 'transaction'
      && currentLines.length > 0
      && currentLines.every(line => isCompleteLine(line))
    ) {
      lines.value.push(emptyLine())
    }
  },
  { deep: true },
)

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

async function submit() {
  submitting.value = true
  try {
    if (tab.value === 'transaction') {
      if (!canSubmitLines.value) return
      const payload: TransactionRequest = {
        description: description.value,
        date: date.value,
        items: lines.value
          .filter(isCompleteLine)
          .map(l => ({
            expenseCategoryId: l.expenseCategoryId,
            amount: dollarsToCents(l.amount),
          })),
      }
      await apiClient.post('/api/transactions/transaction', payload)
      snackbar.enqueueSnackbar('Transaction added', { variant: 'success' })
    } else if (tab.value === 'income') {
      if (!canSubmitIncome.value) return
      const payload: TransactionRequest = {
        description: description.value,
        date: date.value,
        items: Object.entries(incomeAllocations.value)
          .map(([categoryId, amount]) => ({
            expenseCategoryId: Number(categoryId),
            amount: dollarsToCents(amount),
          }))
          .filter(item => item.amount > 0),
      }
      await apiClient.post('/api/transactions/income', payload)
      snackbar.enqueueSnackbar('Income added', { variant: 'success' })
    } else {
      const payload: TransferRequest = {
        description: description.value,
        date: date.value,
        amount: dollarsToCents(transferAmount.value),
        fromCategoryId: fromCategoryId.value as number,
        toCategoryId: toCategoryId.value as number,
      }
      await apiClient.post('/api/transactions/transfer', payload)
      snackbar.enqueueSnackbar('Transfer added', { variant: 'success' })
    }
    resetForm()
    emit('success')
  } catch (e: unknown) {
    snackbar.enqueueSnackbar(e instanceof Error ? e.message : 'Failed to save', { variant: 'error' })
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <v-dialog :model-value="props.modelValue" max-width="600" @update:model-value="(val: boolean) => !val && close()">
    <v-card>
      <v-card-title>Add Transaction</v-card-title>
      <v-card-text class="dialog-scroll-area">
        <v-tabs v-model="tab" class="mb-4">
          <v-tab value="transaction">Transaction</v-tab>
          <v-tab value="income">Income</v-tab>
          <v-tab value="transfer">Transfer</v-tab>
        </v-tabs>

        <div class="d-flex flex-column ga-3">
          <v-text-field label="Description" v-model="description" hide-details />
          <v-text-field label="Date" type="date" v-model="date" hide-details />

          <template v-if="tab === 'transaction'">
            <span class="text-subtitle-2">Items</span>
            <div v-for="(item, index) in lines" :key="index" class="allocation-line d-flex align-center ga-1">
              <v-combobox
                label="Category"
                :items="props.categories"
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

          <template v-else-if="tab === 'income'">
            <v-text-field
              label="Total Income Amount ($)"
              type="number"
              step="0.01"
              min="0"
              v-model="incomeTotal"
              hide-details
            />
            <span class="text-subtitle-2">Allocate to Categories</span>
            <IncomeAllocationList
              :total-cents="incomeTotalCents"
              :categories="props.categories"
              :month="dateAsMonth"
              v-model="incomeAllocations"
            />
          </template>

          <template v-else>
            <v-text-field label="Amount ($)" type="number" step="0.01" min="0" v-model="transferAmount" />
            <v-select label="From Category" :items="props.categories" item-title="name" item-value="id" v-model="fromCategoryId" />
            <v-select label="To Category" :items="props.categories" item-title="name" item-value="id" v-model="toCategoryId" />
          </template>
        </div>
      </v-card-text>
      <div v-if="tab === 'income'" class="px-4 pt-2">
        <span
          class="text-body-2"
          :class="incomeRemainingCents === 0 ? 'text-success' : 'text-warning'"
        >
          Remaining to allocate: {{ formatCents(incomeRemainingCents) }}
        </span>
      </div>
      <v-card-actions>
        <v-spacer />
        <v-btn @click="close">Cancel</v-btn>
        <v-btn
          color="primary"
          :loading="submitting"
          :disabled="(tab === 'transaction' && !canSubmitLines) || (tab === 'income' && !canSubmitIncome)"
          @click="submit"
        >
          Save
        </v-btn>
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
