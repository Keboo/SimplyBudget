<script setup lang="ts">
import { ref, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { ExpenseCategoryDto, RuleDto } from '@/types'
import CategorySelector from '@/components/CategorySelector.vue'

const props = defineProps<{
  modelValue: boolean
  categories: ExpenseCategoryDto[]
  description?: string | null
  notes?: string | null
  expenseCategoryId?: number | null
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: boolean): void
  (e: 'created', rule: RuleDto): void
}>()

const snackbar = useSnackbarStore()
const saving = ref(false)
const form = ref({ name: '', ruleRegex: '', notes: '', expenseCategoryId: null as number | null })

function resetForm() {
  form.value = {
    name: '',
    ruleRegex: props.description ?? '',
    notes: props.notes ?? '',
    expenseCategoryId: props.expenseCategoryId ?? null,
  }
}

watch(
  () => props.modelValue,
  (isOpen) => {
    if (isOpen) resetForm()
  },
  { immediate: true },
)

function close() {
  if (saving.value) return
  emit('update:modelValue', false)
}

async function handleAdd() {
  saving.value = true
  try {
    const notes = form.value.notes.trim()
    const rule = await apiClient.post<RuleDto>('/api/rules', {
      name: form.value.name,
      ruleRegex: form.value.ruleRegex,
      notes: notes.length > 0 ? notes : null,
      expenseCategoryId: form.value.expenseCategoryId,
    })
    snackbar.enqueueSnackbar('Rule added', { variant: 'success' })
    emit('created', rule)
    emit('update:modelValue', false)
  } catch {
    snackbar.enqueueSnackbar('Failed to add rule', { variant: 'error' })
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <v-dialog :model-value="modelValue" max-width="500" @update:model-value="(val: boolean) => !val && close()">
    <v-card>
      <v-card-title>Add Rule</v-card-title>
      <v-card-text class="d-flex flex-column" style="gap: 16px;">
        <v-text-field label="Name" v-model="form.name" />
        <v-text-field label="Regex Pattern" v-model="form.ruleRegex" />
        <v-textarea
          label="Notes"
          rows="2"
          auto-grow
          hint="Added to matching pending expenses. A rule can add notes without setting a category."
          persistent-hint
          v-model="form.notes"
        />
        <CategorySelector
          label="Target Category"
          :categories="categories"
          null-option-label="None"
          :clearable="false"
          v-model="form.expenseCategoryId"
        />
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn :disabled="saving" @click="close">Cancel</v-btn>
        <v-btn color="primary" :loading="saving" @click="handleAdd">Add</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
