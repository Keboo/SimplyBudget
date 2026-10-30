<script setup lang="ts">
import { ref, watch } from 'vue'
import { useDisplay } from 'vuetify'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { BudgetCategoryDto, ExpenseCategoryDto } from '@/types'
import { centsToDollars } from '@/utils/currency'

const props = defineProps<{
  modelValue: boolean
  category: ExpenseCategoryDto | BudgetCategoryDto | null
}>()
const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  success: []
  closed: []
}>()
const { xs: phone } = useDisplay()
const snackbar = useSnackbarStore()
const saving = ref(false)
const error = ref('')
const form = ref({
  name: '', description: '', categoryName: '',
  budgetType: 'fixed' as 'fixed' | 'percentage',
  budgetedAmount: '', budgetedPercentage: '', cap: '',
})

watch(() => [props.modelValue, props.category] as const, ([open, category]) => {
  if (!open || !category) return
  error.value = ''
  form.value = {
    name: category.name ?? '',
    description: category.description ?? '',
    categoryName: category.categoryName ?? '',
    budgetType: category.usePercentage ? 'percentage' : 'fixed',
    budgetedAmount: centsToDollars(category.budgetedAmount),
    budgetedPercentage: String(category.budgetedPercentage),
    cap: category.cap === null ? '' : centsToDollars(category.cap),
  }
}, { immediate: true })

function close() {
  if (!saving.value) emit('update:modelValue', false)
}

async function save() {
  const category = props.category
  if (!category || saving.value) return
  error.value = ''
  const name = form.value.name.trim()
  const fixed = form.value.budgetType === 'fixed'
  const amount = Number(form.value.budgetedAmount)
  const percentage = Number(form.value.budgetedPercentage)
  const cap = form.value.cap.trim() === '' ? null : Number(form.value.cap)
  if (!name) {
    error.value = 'Category name is required'
  } else if (fixed && (!Number.isFinite(amount) || amount < 0)) {
    error.value = 'Budgeted amount must be 0 or greater'
  } else if (!fixed && (!Number.isInteger(percentage) || percentage <= 0 || percentage > 100)) {
    error.value = 'Budgeted percentage must be a whole number between 1 and 100'
  } else if (cap !== null && (!Number.isFinite(cap) || cap < 0)) {
    error.value = 'Cap must be a valid amount that is 0 or greater'
  }
  if (error.value) return

  saving.value = true
  try {
    await apiClient.put(`/api/expense-categories/${category.id}`, {
      name,
      description: form.value.description,
      categoryName: form.value.categoryName,
      budgetedAmount: fixed ? Math.round(amount * 100) : 0,
      budgetedPercentage: fixed ? 0 : percentage,
      cap: cap === null ? null : Math.round(cap * 100),
      accountId: category.accountId,
    })
    snackbar.enqueueSnackbar('Category updated', { variant: 'success' })
    emit('update:modelValue', false)
    emit('success')
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to update category'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <v-dialog
    :model-value="modelValue"
    :fullscreen="phone"
    :persistent="saving"
    max-width="600"
    aria-label="Edit expense category"
    @update:model-value="(value: boolean) => !value && close()"
    @after-leave="emit('closed')"
  >
    <v-card title="Edit category">
      <form @submit.prevent="save">
        <v-card-text class="d-flex flex-column ga-4">
          <v-alert v-if="error" type="error" variant="tonal" role="alert">{{ error }}</v-alert>
          <v-text-field v-model="form.name" label="Name" hide-details autofocus :disabled="saving" />
          <v-text-field v-model="form.categoryName" label="Group" hide-details :disabled="saving" />
          <v-textarea v-model="form.description" label="Description" rows="2" auto-grow hide-details :disabled="saving" />
          <v-select v-model="form.budgetType" label="Budget type" :items="[{ title: 'Fixed', value: 'fixed' }, { title: 'Percentage', value: 'percentage' }]" hide-details :disabled="saving" />
          <v-text-field v-if="form.budgetType === 'fixed'" v-model="form.budgetedAmount" label="Budget" type="number" step="0.01" min="0" prefix="$" hide-details :disabled="saving" />
          <v-text-field v-else v-model="form.budgetedPercentage" label="Budget" type="number" step="1" min="1" max="100" suffix="%" hide-details :disabled="saving" />
          <v-text-field v-model="form.cap" label="Cap" type="number" step="0.01" min="0" prefix="$" :disabled="saving || form.budgetType !== 'fixed'" hint="Optional maximum running balance for fixed-budget categories." persistent-hint />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn :disabled="saving" @click="close">Cancel</v-btn>
          <v-btn color="primary" type="submit" :loading="saving" :disabled="saving">Save</v-btn>
        </v-card-actions>
      </form>
    </v-card>
  </v-dialog>
</template>
