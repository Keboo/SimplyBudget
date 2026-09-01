<script setup lang="ts">
import { ref, watch } from 'vue'

const props = withDefaults(defineProps<{
  modelValue: string
  label?: string
  density?: 'default' | 'comfortable' | 'compact'
}>(), {
  label: 'Amount ($)',
  density: 'compact',
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
}>()

// The displayed value is kept local while editing so that dependent totals
// (such as remaining amounts) only update on blur or when Enter is pressed.
const draft = ref(props.modelValue)

watch(() => props.modelValue, value => {
  draft.value = value
})

function commit() {
  if (draft.value === props.modelValue) return
  emit('update:modelValue', draft.value)
}
</script>

<template>
  <v-text-field
    :label="props.label"
    type="number"
    step="0.01"
    min="0"
    v-model="draft"
    hide-details
    :density="props.density"
    @blur="commit"
    @keyup.enter="commit"
  />
</template>
