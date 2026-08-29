<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import type { ExpenseCategoryDto } from '@/types'
import { formatCents, dollarsToCents, centsToDollars } from '@/utils/currency'

const props = defineProps<{
  /** Total income amount (in cents) being allocated. */
  totalCents: number
  categories: ExpenseCategoryDto[]
  /** Month whose budget remaining amounts should be shown/used (any date within the month). */
  month: Date
  /** Dollar-string amount per expense category id. */
  modelValue: Record<number, string>
}>()

const emit = defineEmits<{
  'update:modelValue': [value: Record<number, string>]
}>()

interface RemainingBudgetByCategoryDto {
  currentAmount: number
  remainingAmount: number
}

const remainingBudgetByCategory = ref<Record<number, RemainingBudgetByCategoryDto>>({})

async function loadRemainingBudget() {
  const year = props.month.getFullYear()
  const month = String(props.month.getMonth() + 1).padStart(2, '0')
  try {
    remainingBudgetByCategory.value = await apiClient.get<Record<number, RemainingBudgetByCategoryDto>>(
      `/api/expense-categories/remaining-budget?month=${year}-${month}-01`,
    ) ?? {}
  } catch {
    remainingBudgetByCategory.value = {}
  }
}

watch(() => props.month, loadRemainingBudget, { immediate: true })

function amountFor(categoryId: number): string {
  return props.modelValue[categoryId] ?? ''
}

function setAmount(categoryId: number, amount: string) {
  emit('update:modelValue', { ...props.modelValue, [categoryId]: amount })
}

function amountCentsFor(categoryId: number): number {
  return dollarsToCents(amountFor(categoryId))
}

function currentAmountFor(category: ExpenseCategoryDto): number {
  if (category.usePercentage) return amountCentsFor(category.id)
  return remainingBudgetByCategory.value[category.id]?.currentAmount ?? 0
}

function remainingBudgetFor(category: ExpenseCategoryDto): number {
  return remainingBudgetByCategory.value[category.id]?.remainingAmount ?? 0
}

function maxSuggestedAmountFor(category: ExpenseCategoryDto): number {
  const budgetRemaining = remainingBudgetFor(category)
  const availableToAllocate = remainingCents.value + amountCentsFor(category.id)
  return Math.max(0, Math.min(budgetRemaining, availableToAllocate))
}

function percentageAmountFor(category: ExpenseCategoryDto): number {
  return Math.round((category.budgetedPercentage / 100) * props.totalCents)
}

// Categories first by percentage-based (percentage before fixed), then by whether there's a
// pending/remaining budget amount for the month, then alphabetically.
const sortedCategories = computed(() =>
  [...props.categories].sort((a, b) => {
    if (a.usePercentage !== b.usePercentage) return a.usePercentage ? -1 : 1

    const aHasRemaining = !a.usePercentage && remainingBudgetFor(a) > 0
    const bHasRemaining = !b.usePercentage && remainingBudgetFor(b) > 0
    if (aHasRemaining !== bHasRemaining) return aHasRemaining ? -1 : 1

    return (a.name ?? '').localeCompare(b.name ?? '')
  }),
)

const allocatedCents = computed(() =>
  props.categories.reduce((sum, c) => sum + amountCentsFor(c.id), 0),
)

const remainingCents = computed(() => props.totalCents - allocatedCents.value)

defineExpose({ remainingCents })

function applyRemainingBudget(category: ExpenseCategoryDto) {
  const amount = maxSuggestedAmountFor(category)
  if (amount <= 0) return
  setAmount(category.id, centsToDollars(amount))
}

function applyPercentage(category: ExpenseCategoryDto) {
  setAmount(category.id, centsToDollars(percentageAmountFor(category)))
}
</script>

<template>
  <div class="d-flex flex-column ga-1">
    <div
      v-for="category in sortedCategories"
      :key="category.id"
      class="income-allocation-row d-flex align-center ga-2"
    >
      <div class="category-info">
        <div>
          {{ category.name }}
          <span class="text-caption text-medium-emphasis">
            &middot; Budget:
            <template v-if="category.usePercentage">
              {{ category.budgetedPercentage }}%
            </template>
            <template v-else>
              {{ formatCents(category.budgetedAmount) }}
            </template>
          </span>
        </div>
        <div class="text-caption text-medium-emphasis">
          Current: {{ formatCents(currentAmountFor(category)) }} &middot; Remaining:
          <template v-if="category.usePercentage">
            <a href="#" class="allocation-link" @click.prevent="applyPercentage(category)">
              {{ category.budgetedPercentage }}%
            </a>
          </template>
          <template v-else>
            <a
              v-if="remainingBudgetFor(category) > 0"
              href="#"
              class="allocation-link"
              @click.prevent="applyRemainingBudget(category)"
            >
              {{ formatCents(remainingBudgetFor(category)) }}
            </a>
            <span v-else class="allocation-link-disabled">
              {{ formatCents(remainingBudgetFor(category)) }}
            </span>
          </template>
        </div>
      </div>
      <v-text-field
        label="Amount ($)"
        type="number"
        step="0.01"
        min="0"
        :model-value="amountFor(category.id)"
        @update:model-value="(val: string) => setAmount(category.id, val)"
        hide-details
        density="compact"
        class="amount-field"
      />
    </div>
  </div>
</template>

<style scoped>
.income-allocation-row {
  min-height: 40px;
}

.category-info {
  flex: 2 1 0;
}

.amount-field {
  flex: 1 1 160px;
}

.allocation-link {
  text-decoration: underline;
}

.allocation-link-disabled {
  text-decoration: underline;
  opacity: 0.5;
}
</style>
