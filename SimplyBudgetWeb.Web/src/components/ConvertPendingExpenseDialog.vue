<script setup lang="ts">
import { ref, computed, watch, onBeforeUnmount } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type {
  CalculatorTaxOptionDto,
  CalculatorTaxOptionsDto,
  ExpenseCategoryDto,
  PendingExpenseDto,
  ConvertPendingExpenseRequest,
  ReceiptDto,
} from '@/types'
import { formatCents, dollarsToCents, centsToDollars, parseLocalDate } from '@/utils/currency'
import IncomeAllocationList from '@/components/IncomeAllocationList.vue'
import CategorySelector from '@/components/CategorySelector.vue'
import AmountField from '@/components/AmountField.vue'
import ReceiptPreview from '@/components/ReceiptPreview.vue'

const props = defineProps<{
  modelValue: boolean
  pendingExpense: PendingExpenseDto | null
  categories: ExpenseCategoryDto[]
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  success: []
  noteSaved: [pendingExpense: PendingExpenseDto]
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
const notes = ref('')
const savedNotes = ref('')
const incomeAllocations = ref<Record<number, string>>({})
const ignoreBudget = ref(false)
const submitting = ref(false)
const savingNote = ref(false)
const receiptMatches = ref<ReceiptDto[]>([])
const receiptPanel = ref<number>()
const receiptPanelTouched = ref(false)
const receiptId = ref<number | null>(null)
const receiptChosen = ref(false)
const previewReceiptId = ref<number | null>(null)
const loadingReceiptMatches = ref(false)
const receiptMatchError = ref('')
const pendingReceipts = ref<ReceiptDto[]>([])
const browsingReceipts = ref(false)
const loadingPendingReceipts = ref(false)
const pendingReceiptError = ref('')
let receiptRequest = 0
let matchRequest = 0
let browseRequest = 0
const uniqueUnlinked = (receipts: ReceiptDto[]) =>
  [...new Map(receipts.filter(receipt => !receipt.isLinked).map(receipt => [receipt.id, receipt])).values()]
const otherPendingReceipts = computed(() =>
  pendingReceipts.value.filter(receipt => !receiptMatches.value.some(match => match.id === receipt.id)),
)
const availableReceipts = computed(() => uniqueUnlinked([...receiptMatches.value, ...pendingReceipts.value]))
const receiptSummary = computed(() => {
  const selected = availableReceipts.value.find(receipt => receipt.id === receiptId.value)
  if (selected) return selected.merchantName || selected.fileName
  if (receiptMatchError.value) return 'Could not check matches'
  if (loadingReceiptMatches.value) return 'Checking matches...'
  const count = receiptMatches.value.length
  return count ? `${count} suggested ${count === 1 ? 'match' : 'matches'}` : 'No matches'
})
const calculatorOpen = ref(false)
const calculatorLineIndex = ref<number | null>(null)
const calculatorInput = ref('')
const calculatorItems = ref<number[]>([])
const calculatorTaxOptions = ref<CalculatorTaxOptionDto[]>([])
const calculatorSelectedTaxKey = ref('none')

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
  [() => props.modelValue, () => props.pendingExpense?.id],
  () => {
    receiptRequest++
    receiptId.value = null
    receiptChosen.value = false
    receiptMatches.value = []
    receiptPanel.value = undefined
    receiptPanelTouched.value = false
    pendingReceipts.value = []
    browsingReceipts.value = false
    loadingPendingReceipts.value = false
    pendingReceiptError.value = ''
    previewReceiptId.value = null
    receiptMatchError.value = ''
    loadingReceiptMatches.value = false
    const pe = props.pendingExpense
    if (!props.modelValue || !pe) return
    description.value = pe.description ?? ''
    date.value = pe.date.split('T')[0]
    notes.value = pe.notes ?? ''
    savedNotes.value = notes.value
    ignoreBudget.value = false
    lines.value = [{
      expenseCategoryId: pe.suggestedCategoryId ?? null,
      amount: centsToDollars(pe.amount),
    }]
    incomeAllocations.value = {}
    void loadReceiptMatches()
  },
  { immediate: true },
)

async function loadReceiptMatches() {
  if (!props.modelValue || !props.pendingExpense?.isDebit || !date.value) return
  const session = receiptRequest
  const request = ++matchRequest
  const pending = props.pendingExpense
  receiptMatchError.value = ''
  loadingReceiptMatches.value = true
  try {
    const params = new URLSearchParams({
      amount: String(pending.amount),
      date: pending.date.split('T')[0]!,
    })
    const matches = await apiClient.get<ReceiptDto[]>(`/api/receipts/matches?${params}`)
    if (session !== receiptRequest || request !== matchRequest) return
    receiptMatches.value = uniqueUnlinked(matches ?? [])
    if (!receiptChosen.value && receiptId.value === null) receiptId.value = bestMatchId(receiptMatches.value, pending.date)
    if (!receiptPanelTouched.value) receiptPanel.value = receiptMatches.value.length ? 0 : undefined
  } catch (e: unknown) {
    if (session !== receiptRequest || request !== matchRequest) return
    receiptMatchError.value = e instanceof Error ? e.message : 'Failed to find matching receipts'
    if (!receiptPanelTouched.value) receiptPanel.value = 0
    snackbar.enqueueSnackbar(e instanceof Error ? e.message : 'Failed to find matching receipts', { variant: 'error' })
  } finally {
    if (session === receiptRequest && request === matchRequest) loadingReceiptMatches.value = false
  }
}

// Matches already share the expense total; pre-select the one closest in date, unless another
// match is just as close and the choice would be a guess.
function bestMatchId(matches: ReceiptDto[], expenseDate: string): number | null {
  const day = (value: string) => Date.parse(value.split('T')[0]!) / 86_400_000
  const ranked = matches
    .filter(receipt => receipt.transactionDate)
    .map(receipt => ({ id: receipt.id, distance: Math.abs(day(receipt.transactionDate!) - day(expenseDate)) }))
    .sort((a, b) => a.distance - b.distance)
  const [best, next] = ranked
  return best && (!next || next.distance > best.distance) ? best.id : null
}

async function browsePendingReceipts() {
  if (!props.modelValue || !props.pendingExpense?.isDebit) return
  const session = receiptRequest
  const request = ++browseRequest
  browsingReceipts.value = true
  loadingPendingReceipts.value = true
  pendingReceiptError.value = ''
  try {
    const receipts = await apiClient.get<ReceiptDto[]>('/api/receipts')
    if (session !== receiptRequest || request !== browseRequest) return
    pendingReceipts.value = uniqueUnlinked(receipts ?? [])
  } catch (error: unknown) {
    if (session !== receiptRequest || request !== browseRequest) return
    pendingReceiptError.value = error instanceof Error ? error.message : 'Failed to load pending receipts'
  } finally {
    if (session === receiptRequest && request === browseRequest) loadingPendingReceipts.value = false
  }
}

watch(receiptId, () => { previewReceiptId.value = null })
onBeforeUnmount(() => { receiptRequest++ })

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

const hasNoteChanges = computed(() => (notes.value ?? '').trim() !== (savedNotes.value ?? '').trim())

const calculatorTotalCents = computed(() => {
  const subtotal = calculatorItems.value.reduce((sum, amount) => sum + amount, 0)
  const taxPercentage = selectedTaxPercentage.value
  return taxPercentage === null
    ? subtotal
    : Math.round(subtotal * (1 + (taxPercentage / 100)))
})

const selectedTaxPercentage = computed<number | null>(() => {
  if (calculatorSelectedTaxKey.value === 'none') return null
  if (!calculatorSelectedTaxKey.value.startsWith('tax-')) return null

  const selectedIndex = Number.parseInt(calculatorSelectedTaxKey.value.slice(4), 10)
  const selectedTaxOption = Number.isInteger(selectedIndex)
    ? calculatorTaxOptions.value[selectedIndex]
    : undefined

  return selectedTaxOption?.percentage ?? null
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

async function openCalculator(index: number) {
  calculatorLineIndex.value = index
  calculatorInput.value = ''
  calculatorItems.value = []
  calculatorOpen.value = true
  await loadCalculatorTaxOptions()
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

async function loadCalculatorTaxOptions() {
  try {
    const response = await apiClient.get<CalculatorTaxOptionsDto>('/api/calculator-tax-options')
    calculatorTaxOptions.value = response.options ?? []

    const defaultTaxIndex = calculatorTaxOptions.value.findIndex(option => option.isDefault)
    calculatorSelectedTaxKey.value = defaultTaxIndex >= 0 ? `tax-${defaultTaxIndex}` : 'none'
  } catch {
    calculatorTaxOptions.value = []
    calculatorSelectedTaxKey.value = 'none'
  }
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
  receiptRequest++
  receiptId.value = null
  previewReceiptId.value = null
  emit('update:modelValue', false)
}

async function submit() {
  if (!props.pendingExpense || !canSubmit.value) return
  if (receiptId.value !== null && !availableReceipts.value.some(receipt => receipt.id === receiptId.value)) {
    receiptId.value = null
    snackbar.enqueueSnackbar('That receipt is no longer available. Select another receipt or apply without one.', { variant: 'error' })
    return
  }
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
      notes: notes.value,
      receiptId: receiptId.value,
      items,
    }
    await apiClient.post(`/api/pending-expenses/${props.pendingExpense.id}/convert`, payload)
    snackbar.enqueueSnackbar('Pending expense converted', { variant: 'success' })
    emit('success')
    close()
  } catch (e: unknown) {
    snackbar.enqueueSnackbar(e instanceof Error ? e.message : 'Failed to convert pending expense', { variant: 'error' })
    if (receiptId.value !== null) void loadReceiptMatches()
    if (browsingReceipts.value) void browsePendingReceipts()
  } finally {
    submitting.value = false
  }
}

async function saveNote() {
  if (!props.pendingExpense || !hasNoteChanges.value) return
  savingNote.value = true
  try {
    const nextNoteRaw = notes.value ?? ''
    const nextNote = nextNoteRaw.trim().length > 0 ? nextNoteRaw.trim() : null
    const refreshed = await apiClient.put<PendingExpenseDto>(`/api/pending-expenses/${props.pendingExpense.id}`, {
      assigneeId: props.pendingExpense.assigneeId,
      notes: nextNote,
      version: props.pendingExpense.version,
    })
    notes.value = refreshed.notes ?? ''
    savedNotes.value = notes.value
    emit('noteSaved', refreshed)
    snackbar.enqueueSnackbar('Note saved', { variant: 'success' })
  } catch (e: unknown) {
    snackbar.enqueueSnackbar(e instanceof Error ? e.message : 'Failed to save pending expense note', { variant: 'error' })
  } finally {
    savingNote.value = false
  }
}
</script>

<template>
  <v-dialog :model-value="props.modelValue" max-width="600" @update:model-value="(val: boolean) => !val && close()">
    <v-card v-if="pendingExpense">
      <v-card-title class="text-wrap">Process pending expense</v-card-title>
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

          <v-expansion-panels
            v-if="pendingExpense.isDebit"
            v-model="receiptPanel"
            variant="accordion"
            @update:model-value="receiptPanelTouched = true"
          >
            <v-expansion-panel elevation="0" border>
              <v-expansion-panel-title min-height="40" class="px-3 py-2">
                <div class="d-flex flex-wrap align-center ga-2">
                  <span class="text-body-2">Receipt (optional)</span>
                  <span class="text-caption text-medium-emphasis">{{ receiptSummary }}</span>
                  <v-progress-circular v-if="loadingReceiptMatches" size="16" width="2" indeterminate />
                </div>
              </v-expansion-panel-title>
              <v-expansion-panel-text>
                <p class="text-caption text-medium-emphasis">
                  Suggestions match the total and are within five days. Browse all receipts if AI missed the details.
                </p>
                <v-alert v-if="receiptMatchError" type="error" variant="tonal" class="mt-2">
                  {{ receiptMatchError }}
                  <v-btn variant="text" @click="loadReceiptMatches">Retry matches</v-btn>
                </v-alert>
                <v-radio-group v-model="receiptId" density="compact" hide-details @update:model-value="receiptChosen = true">
                  <v-radio :value="null" label="No receipt" density="compact" />
                  <div v-if="receiptMatches.length" class="text-caption">Suggested matches</div>
                  <v-radio
                    v-for="receipt in receiptMatches"
                    :key="receipt.id"
                    :value="receipt.id"
                    density="compact"
                  >
                    <template #label>
                      <span class="receipt-label">
                        {{ receipt.merchantName || receipt.fileName }}
                        · {{ receipt.transactionDate ? new Date(receipt.transactionDate).toLocaleDateString() : 'Date not set' }}
                        · {{ receipt.totalAmountCents == null ? 'Total not set' : formatCents(receipt.totalAmountCents) }}
                      </span>
                    </template>
                  </v-radio>
                  <div v-if="!receiptMatches.length && !loadingReceiptMatches && !receiptMatchError" class="text-body-2 text-medium-emphasis mt-2">
                    No matching pending receipts.
                  </div>
                  <template v-if="browsingReceipts">
                    <div class="text-subtitle-2 mt-3">Other pending receipts</div>
                    <v-radio v-for="receipt in otherPendingReceipts" :key="receipt.id" :value="receipt.id" density="compact">
                      <template #label>
                        <span class="receipt-label">
                          {{ receipt.merchantName || receipt.fileName }}
                          · {{ receipt.transactionDate ? new Date(receipt.transactionDate).toLocaleDateString() : 'Date not set' }}
                          · {{ receipt.totalAmountCents == null ? 'Total not set' : formatCents(receipt.totalAmountCents) }}
                        </span>
                      </template>
                    </v-radio>
                    <div v-if="!otherPendingReceipts.length && !loadingPendingReceipts && !pendingReceiptError" class="text-body-2 text-medium-emphasis mt-2">
                      No other pending receipts.
                    </div>
                  </template>
                </v-radio-group>
                <v-alert v-if="pendingReceiptError" type="error" variant="tonal" class="mt-2">{{ pendingReceiptError }}</v-alert>
                <div class="d-flex flex-wrap ga-1">
                  <v-btn variant="text" size="small" :disabled="loadingReceiptMatches" @click="loadReceiptMatches">Refresh matches</v-btn>
                  <v-btn variant="text" size="small" :loading="loadingPendingReceipts" :disabled="loadingPendingReceipts" @click="browsePendingReceipts">
                    {{ browsingReceipts ? 'Refresh all pending receipts' : 'Browse all pending receipts' }}
                  </v-btn>
                  <v-btn
                    v-if="receiptId !== null"
                    variant="text"
                    size="small"
                    @click="previewReceiptId = previewReceiptId === receiptId ? null : receiptId"
                  >
                    {{ previewReceiptId ? 'Hide receipt' : 'View receipt' }}
                  </v-btn>
                </div>
                <ReceiptPreview v-if="previewReceiptId !== null" :receipt-id="previewReceiptId" :image-geometry="availableReceipts.find(receipt => receipt.id === previewReceiptId)?.imageGeometry" />
              </v-expansion-panel-text>
            </v-expansion-panel>
          </v-expansion-panels>

          <template v-if="pendingExpense.isDebit">
            <span class="text-subtitle-2">Split expense across categories</span>
            <div v-for="(item, index) in lines" :key="index" class="allocation-line d-flex align-center ga-1">
              <CategorySelector
                label="Category"
                :categories="sortedCategories"
                v-model="item.expenseCategoryId"
                :error="item.expenseCategoryId !== null && !isValidCategoryId(item.expenseCategoryId)"
                :allow-custom="true"
                class="category-field"
              />
              <div class="amount-field d-flex align-center ga-1">
                <AmountField v-model="item.amount" />
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

        <v-textarea
          label="Notes"
          v-model="notes"
          rows="2"
          auto-grow
          density="compact"
          class="mt-2"
        />
      </div>
      <v-card-actions class="flex-wrap">
        <v-spacer />
        <v-btn min-height="44" @click="close">Cancel</v-btn>
        <v-btn min-height="44" :loading="savingNote" :disabled="submitting || !hasNoteChanges" @click="saveNote">Save Note</v-btn>
        <v-btn min-height="44" color="primary" :loading="submitting" :disabled="!canSubmit" @click="submit">Apply</v-btn>
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

        <v-radio-group v-model="calculatorSelectedTaxKey" density="compact" hide-details>
          <v-radio label="None" value="none" />
          <v-radio
            v-for="(option, index) in calculatorTaxOptions"
            :key="`${option.name}-${index}`"
            :label="`${option.name} (+${option.percentage}%)`"
            :value="`tax-${index}`"
          />
        </v-radio-group>
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
  min-width: 0;
}

.amount-field {
  flex: 1 1 180px;
  min-width: 0;
}
.receipt-label { overflow-wrap: anywhere; }
@media (max-width: 500px) {
  .allocation-line { flex-wrap: wrap; }
  .category-field { flex-basis: 100%; }
  .amount-field { flex: 1 1 160px; }
}
</style>
