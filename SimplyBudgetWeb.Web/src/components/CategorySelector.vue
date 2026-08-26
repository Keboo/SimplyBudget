<script setup lang="ts">
import { computed } from 'vue'
import type { ExpenseCategoryDto } from '@/types'

type CategorySelectorValue = number | string | null

const props = withDefaults(defineProps<{
  modelValue: CategorySelectorValue
  categories: ExpenseCategoryDto[]
  label?: string
  nullOptionLabel?: string | null
  clearable?: boolean
  hideDetails?: boolean | 'auto'
  density?: 'default' | 'comfortable' | 'compact'
  error?: boolean
  allowCustom?: boolean
  disabled?: boolean
}>(), {
  label: 'Category',
  nullOptionLabel: null,
  clearable: true,
  hideDetails: true,
  density: 'compact',
  error: false,
  allowCustom: false,
  disabled: false,
})

const emit = defineEmits<{
  'update:modelValue': [value: CategorySelectorValue]
}>()

interface CategoryOption {
  id: number | null
  name: string
}

const items = computed<CategoryOption[]>(() => {
  const sortedCategories = props.categories
    .map(category => ({
      id: category.id,
      name: category.name ?? `Category ${category.id}`,
    }))
    .sort((a, b) => a.name.localeCompare(b.name, undefined, { sensitivity: 'base' }))

  if (props.nullOptionLabel === null) {
    return sortedCategories
  }

  return [{ id: null, name: props.nullOptionLabel }, ...sortedCategories]
})

const validCategoryIds = computed(() => new Set(props.categories.map(category => category.id)))

function updateValue(value: CategorySelectorValue) {
  if (value === null) {
    emit('update:modelValue', null)
    return
  }

  if (typeof value === 'number') {
    emit('update:modelValue', validCategoryIds.value.has(value) ? value : null)
    return
  }

  const parsedValue = Number(value)
  if (Number.isInteger(parsedValue) && validCategoryIds.value.has(parsedValue)) {
    emit('update:modelValue', parsedValue)
    return
  }

  emit('update:modelValue', props.allowCustom ? value : null)
}
</script>

<template>
  <v-combobox
    :model-value="props.modelValue"
    :label="props.label"
    :items="items"
    item-title="name"
    item-value="id"
    :return-object="false"
    auto-select-first="exact"
    :clearable="props.clearable"
    :hide-details="props.hideDetails"
    :density="props.density"
    :error="props.error"
    :disabled="props.disabled"
    @update:model-value="updateValue"
  />
</template>
