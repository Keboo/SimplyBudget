<script setup lang="ts">
import { ref, watch } from 'vue'
import { useDisplay } from 'vuetify'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { ExpenseCategoryDto } from '@/types'
import { centsToDollars, dollarsToCents } from '@/utils/currency'

const props = defineProps<{
  modelValue: boolean
  category: ExpenseCategoryDto | null
}>()
const emit = defineEmits<{
  'update:modelValue': [value: boolean]
  success: []
  'after-leave': []
}>()

const { xs: phone } = useDisplay()
const snackbar = useSnackbarStore()
const saving = ref(false)
const form = ref({
  name: '',
  description: '',
  categoryName: '',
  budgetType: 'fixed' as 'fixed' | 'percentage',
  budgetedAmount: '',
  budgetedPercentage: '',
  cap: '',
})

watch(() => [props.modelValue, props.category], () => {
  const category = props.category
  if (!props.modelValue || !category) return
  form.value = {
    name: category.name ?? '',
    description: category.description ?? '',
    categoryName: category.categoryName ?? '',
    budgetType: category.usePercentage ? 'percentage' : 'fixed',
    budgetedAmount: centsToDollars(category.budgetedAmount),
    budgetedPercentage: category.budgetedPercentage.toString(),
    cap: category.cap === null ? '' : centsToDollars(category.cap),
  }
}, { immediate: true })

function close() {
  if (!saving.value) emit('update:modelValue', false)
}

async function save() {
  const category = props.category
  if (!category || saving.value) return
  const name = form.value.name.trim()
  const budgetedAmount = dollarsToCents(form.value.budgetedAmount)
  const budgetedPercentage = Number(form.value.budgetedPercentage)
  const capText = form.value.cap.trim()
  const cap = capText === '' ? null : Number(capText)

  if (!name) {
    snackbar.enqueueSnackbar('Category name is required', { variant: 'error' })
    return
  }
  if (form.value.budgetType === 'fixed' && (!Number.isFinite(Number(form.value.budgetedAmount)) || budgetedAmount < 0)) {
    snackbar.enqueueSnackbar('Budgeted amount must be 0 or greater', { variant: 'error' })
    return
  }
  if (form.value.budgetType === 'percentage' && (!Number.isInteger(budgetedPercentage) || budgetedPercentage <= 0 || budgetedPercentage > 100)) {
    snackbar.enqueueSnackbar('Budgeted percentage must be a whole number between 1 and 100', { variant: 'error' })
    return
  }
  if (cap !== null && (!Number.isFinite(cap) || cap < 0)) {
    snackbar.enqueueSnackbar('Cap must be a valid amount that is 0 or greater', { variant: 'error' })
    return
  }

  saving.value = true
  try {
    await apiClient.put(`/api/expense-categories/${category.id}`, {
      name,
      description: form.value.description,
      categoryName: form.value.categoryName,
      budgetedAmount: form.value.budgetType === 'fixed' ? budgetedAmount : 0,
      budgetedPercentage: form.value.budgetType === 'percentage' ? budgetedPercentage : 0,
      cap: cap === null ? null : Math.round(cap * 100),
      accountId: category.accountId,
    })
    snackbar.enqueueSnackbar('Category updated', { variant: 'success' })
    emit('update:modelValue', false)
    emit('success')
  } catch {
    snackbar.enqueueSnackbar('Failed to update category', { variant: 'error' })
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
    @update:model-value="(value: boolean) => !value && close()"
    @after-leave="emit('after-leave')"
  >
    <v-card :class="{ 'category-editor-phone': phone }" title="Edit category">
      <v-form :disabled="saving" @submit.prevent="save">
        <v-card-text class="d-flex flex-column ga-4">
          <v-text-field v-model="form.name" label="Name" hide-details autofocus />
          <v-text-field v-model="form.categoryName" label="Group" hide-details />
          <v-textarea v-model="form.description" label="Description" rows="2" auto-grow hide-details />
          <v-select v-model="form.budgetType" label="Budget type" :items="[{ title: 'Fixed', value: 'fixed' }, { title: 'Percentage', value: 'percentage' }]" hide-details />
          <v-text-field v-if="form.budgetType === 'fixed'" v-model="form.budgetedAmount" label="Budget" type="number" step="0.01" min="0" prefix="$" hide-details />
          <v-text-field v-else v-model="form.budgetedPercentage" label="Budget" type="number" step="1" min="1" max="100" suffix="%" hide-details />
          <v-text-field v-model="form.cap" label="Cap" type="number" step="0.01" min="0" prefix="$" :disabled="form.budgetType !== 'fixed'" hint="Optional maximum running balance for fixed-budget categories." persistent-hint />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn :disabled="saving" @click="close">Cancel</v-btn>
          <v-btn color="primary" type="submit" :loading="saving">Save</v-btn>
        </v-card-actions>
      </v-form>
    </v-card>
  </v-dialog>
</template>

<style scoped>
.category-editor-phone {
  height: 100dvh;
}
.category-editor-phone :deep(.v-form) {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-height: 0;
}
.category-editor-phone :deep(.v-card-text) {
  min-height: 0;
  overflow-y: auto;
}
.category-editor-phone :deep(.v-btn) {
  min-height: 44px;
  min-width: 44px;
}
.category-editor-phone :deep(.v-card-actions) {
  flex-shrink: 0;
  padding-bottom: max(12px, env(safe-area-inset-bottom));
}
</style>
