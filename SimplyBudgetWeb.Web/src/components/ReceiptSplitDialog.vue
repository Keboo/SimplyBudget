<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useDisplay } from 'vuetify'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type {
  CalculatorTaxOptionDto,
  CalculatorTaxOptionsDto,
  ExpenseCategoryDto,
  ReceiptDto,
} from '@/types'
import { centsToDollars, dollarsToCents, formatCents } from '@/utils/currency'
import { calculateReceiptSplits } from '@/utils/receiptSplit'
import CategorySelector from '@/components/CategorySelector.vue'
import ReceiptPreview from '@/components/ReceiptPreview.vue'

const props = defineProps<{
  modelValue: boolean
  receipt: ReceiptDto
  categories: ExpenseCategoryDto[]
  pendingAmountCents: number
  suggestedCategoryId: number | null
}>()

const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  apply: [items: Array<{ expenseCategoryId: number; amount: number }>]
}>()

interface EditableReceiptLine {
  id: string
  description: string
  amount: string
  categoryId: number | string | null
  taxable: boolean
  excluded: boolean
}

const snackbar = useSnackbarStore()
const { xs: phone } = useDisplay()
const lines = ref<EditableReceiptLine[]>([])
const taxOptions = ref<CalculatorTaxOptionDto[]>([])
const selectedTaxKey = ref('none')
const bulkCategoryId = ref<number | string | null>(null)
const taxOptionsError = ref('')
const loadingTaxOptions = ref(false)
let nextManualLineId = 0

const sortedCategories = computed(() =>
  [...props.categories].sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '')),
)

const selectedTaxPercentage = computed<number | null>(() => {
  if (!selectedTaxKey.value.startsWith('tax-')) return null
  const index = Number.parseInt(selectedTaxKey.value.slice(4), 10)
  return Number.isInteger(index) ? taxOptions.value[index]?.percentage ?? null : null
})

const totals = computed(() => calculateReceiptSplits(
  lines.value.map(line => ({
    categoryId: typeof line.categoryId === 'number'
      && props.categories.some(category => category.id === line.categoryId)
      ? line.categoryId
      : null,
    amountCents: dollarsToCents(line.amount),
    taxable: line.taxable,
    excluded: line.excluded,
  })),
  selectedTaxPercentage.value,
))

const differenceCents = computed(() => props.pendingAmountCents - totals.value.totalCents)
const canApply = computed(() =>
  totals.value.items.length > 0
  && totals.value.unassignedCount === 0
  && lines.value.every(line => line.excluded || dollarsToCents(line.amount) > 0)
  && differenceCents.value === 0,
)

const categoryTotals = computed(() => totals.value.items.map(item => ({
  ...item,
  name: props.categories.find(category => category.id === item.expenseCategoryId)?.name
    ?? `Category ${item.expenseCategoryId}`,
})))

watch(
  [() => props.modelValue, () => props.receipt.id],
  ([isOpen]) => {
    if (!isOpen) return
    lines.value = props.receipt.lineItems.map(line => ({
      id: `receipt-${line.id}`,
      description: line.description,
      amount: centsToDollars(line.amountCents),
      categoryId: props.suggestedCategoryId,
      taxable: true,
      excluded: false,
    }))
    selectedTaxKey.value = 'none'
    bulkCategoryId.value = props.suggestedCategoryId
    void loadTaxOptions()
  },
  { immediate: true },
)

async function loadTaxOptions() {
  loadingTaxOptions.value = true
  taxOptionsError.value = ''
  try {
    const response = await apiClient.get<CalculatorTaxOptionsDto>('/api/calculator-tax-options')
    taxOptions.value = response.options ?? []
  } catch (error: unknown) {
    taxOptions.value = []
    taxOptionsError.value = error instanceof Error ? error.message : 'Failed to load tax options'
    snackbar.enqueueSnackbar(taxOptionsError.value, { variant: 'error' })
  } finally {
    loadingTaxOptions.value = false
  }
}

function assignUnassigned() {
  if (typeof bulkCategoryId.value !== 'number') return
  for (const line of lines.value) {
    if (!line.excluded && typeof line.categoryId !== 'number')
      line.categoryId = bulkCategoryId.value
  }
}

function addLine() {
  lines.value.push({
    id: `manual-${nextManualLineId++}`,
    description: '',
    amount: '',
    categoryId: props.suggestedCategoryId,
    taxable: true,
    excluded: false,
  })
}

function apply() {
  if (!canApply.value) return
  emit('apply', totals.value.items.map(({ expenseCategoryId, amount }) => ({ expenseCategoryId, amount })))
  emit('update:modelValue', false)
}

function close() {
  emit('update:modelValue', false)
}
</script>

<template>
  <v-dialog
    :model-value="props.modelValue"
    :fullscreen="phone"
    max-width="1000"
    scrollable
    @update:model-value="(value: boolean) => !value && close()"
  >
    <v-card v-if="props.modelValue">
      <v-card-title class="d-flex flex-wrap justify-space-between align-center ga-2">
        <span>Split by receipt</span>
        <span class="text-body-2 text-medium-emphasis">
          {{ props.receipt.merchantName || props.receipt.fileName }}
        </span>
      </v-card-title>
      <v-card-text class="receipt-split-content">
        <v-expansion-panels class="mb-3">
          <v-expansion-panel title="Preview receipt image">
            <v-expansion-panel-text>
              <ReceiptPreview
                :receipt-id="props.receipt.id"
                :image-geometry="props.receipt.imageGeometry"
                :title="`Receipt from ${props.receipt.merchantName || props.receipt.fileName}`"
              />
            </v-expansion-panel-text>
          </v-expansion-panel>
        </v-expansion-panels>

        <div class="d-flex flex-wrap align-center ga-2 mb-3">
          <CategorySelector
            v-model="bulkCategoryId"
            label="Assign unassigned to"
            :categories="sortedCategories"
            class="bulk-category"
          />
          <v-btn
            variant="outlined"
            :disabled="typeof bulkCategoryId !== 'number' || totals.unassignedCount === 0"
            @click="assignUnassigned"
          >
            Assign unassigned
          </v-btn>
          <v-btn variant="text" prepend-icon="mdi-plus" @click="addLine">Add missing item</v-btn>
        </div>

        <div class="receipt-lines">
          <div
            v-for="(line, index) in lines"
            :key="line.id"
            class="receipt-line"
            :class="{ 'receipt-line-excluded': line.excluded }"
          >
            <v-text-field
              v-model="line.description"
              :label="`Item ${index + 1}`"
              maxlength="500"
              density="compact"
              hide-details
            />
            <v-text-field
              v-model="line.amount"
              label="Amount"
              prefix="$"
              type="number"
              min="0.01"
              step="0.01"
              density="compact"
              hide-details
            />
            <CategorySelector
              v-model="line.categoryId"
              label="Category"
              :categories="sortedCategories"
              class="line-category"
            />
            <v-checkbox
              v-model="line.taxable"
              label="Taxable"
              density="compact"
              hide-details
              class="taxable-checkbox"
              :disabled="line.excluded"
            />
            <v-btn
              :icon="line.excluded ? 'mdi-undo' : 'mdi-eye-off-outline'"
              size="small"
              variant="text"
              :aria-label="line.excluded ? `Include item ${index + 1}` : `Exclude item ${index + 1}`"
              @click="line.excluded = !line.excluded"
            />
          </div>
        </div>

        <v-alert v-if="lines.length === 0" type="info" variant="tonal" class="mt-3">
          No line items were scanned. Add a missing item to build category splits.
        </v-alert>

        <div class="tax-options mt-4">
          <div class="text-subtitle-2 mb-1">Tax</div>
          <div class="text-caption text-medium-emphasis">
            Receipt prices may already include tax. Tax starts off; choose a preset only when the scanned amounts are pre-tax.
          </div>
          <v-radio-group v-model="selectedTaxKey" inline hide-details :disabled="loadingTaxOptions">
            <v-radio label="None" value="none" />
            <v-radio
              v-for="(option, index) in taxOptions"
              :key="`${option.name}-${index}`"
              :label="`${option.name} (+${option.percentage}%)`"
              :value="`tax-${index}`"
            />
          </v-radio-group>
          <v-alert v-if="taxOptionsError" type="warning" variant="tonal" density="compact" class="mt-2">
            Tax options could not be loaded. You can continue without tax.
          </v-alert>
        </div>

        <v-card variant="outlined" class="pa-3 mt-4">
          <div class="text-subtitle-2 mb-2">Category totals</div>
          <div v-if="categoryTotals.length === 0" class="text-body-2 text-medium-emphasis">
            Assign at least one item to a category to see totals.
          </div>
          <div v-for="category in categoryTotals" :key="category.expenseCategoryId" class="category-total">
            <span>{{ category.name }}</span>
            <span>{{ formatCents(category.subtotalCents) }}</span>
            <span v-if="selectedTaxPercentage !== null" class="text-medium-emphasis">
              + {{ formatCents(category.taxCents) }} tax
            </span>
            <strong>{{ formatCents(category.amount) }}</strong>
          </div>
          <v-divider class="my-2" />
          <div class="d-flex justify-space-between">
            <strong>Split total</strong>
            <strong>{{ formatCents(totals.totalCents) }}</strong>
          </div>
          <div
            class="d-flex flex-wrap justify-space-between mt-1"
            :class="differenceCents === 0 ? 'text-success' : 'text-warning'"
            role="status"
          >
            <span>
              {{ differenceCents === 0 ? 'Matches pending expense' : differenceCents > 0 ? 'Remaining to allocate' : 'Over pending expense by' }}
            </span>
            <strong>{{ formatCents(Math.abs(differenceCents)) }}</strong>
          </div>
          <div v-if="totals.unassignedCount > 0" class="text-warning text-body-2 mt-1" role="status">
            Assign or exclude {{ totals.unassignedCount }} item{{ totals.unassignedCount === 1 ? '' : 's' }} before applying.
          </div>
        </v-card>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn min-height="44" @click="close">Cancel</v-btn>
        <v-btn
          color="primary"
          min-height="44"
          :disabled="!canApply"
          @click="apply"
        >
          Apply {{ totals.items.length }} {{ totals.items.length === 1 ? 'split' : 'splits' }}
        </v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.receipt-split-content {
  max-height: min(72vh, 760px);
}
.bulk-category {
  flex: 1 1 220px;
  min-width: 180px;
}
.receipt-line {
  display: grid;
  grid-template-columns: minmax(140px, 1fr) minmax(100px, 130px) minmax(150px, 1fr) auto 44px;
  gap: 8px;
  align-items: center;
  padding-block: 4px;
}
.receipt-line-excluded {
  opacity: 0.55;
}
.line-category {
  min-width: 0;
}
.taxable-checkbox {
  min-width: 90px;
}
.category-total {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto auto;
  gap: 12px;
  padding-block: 2px;
}
@media (max-width: 700px) {
  .receipt-split-content {
    max-height: none;
    flex: 1;
    overflow-y: auto;
  }
  .receipt-line {
    grid-template-columns: minmax(0, 1fr) 44px;
    border-bottom: 1px solid rgba(var(--v-border-color), var(--v-border-opacity));
    padding-block: 8px;
  }
  .receipt-line > :first-child,
  .receipt-line > :nth-child(2),
  .receipt-line > :nth-child(3),
  .receipt-line > :nth-child(4) {
    grid-column: 1 / -1;
  }
  .receipt-line > :last-child {
    grid-column: 2;
    grid-row: 1;
  }
  .category-total {
    grid-template-columns: minmax(0, 1fr) auto;
  }
  .category-total > :nth-child(3) {
    grid-column: 1;
  }
  .category-total > :last-child {
    grid-column: 2;
    grid-row: 2;
  }
}
</style>
