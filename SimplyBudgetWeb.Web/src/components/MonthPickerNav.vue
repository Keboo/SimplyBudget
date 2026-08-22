<script setup lang="ts">
import { computed, ref } from 'vue'

const props = defineProps<{
  modelValue: Date
}>()

const emit = defineEmits<{
  (event: 'update:modelValue', value: Date): void
}>()

interface MonthOption {
  key: string
  date: Date
  label: string
}

const menuOpen = ref(false)

const monthLabel = computed(() =>
  props.modelValue.toLocaleString('default', { month: 'long', year: 'numeric' }),
)

const monthOptions = computed<MonthOption[]>(() => {
  const today = new Date()
  const firstMonth = new Date(today.getFullYear(), today.getMonth(), 1)

  return Array.from({ length: 37 }, (_, monthOffset) => {
    const date = new Date(firstMonth.getFullYear(), firstMonth.getMonth() - monthOffset, 1)
    return {
      key: `${date.getFullYear()}-${date.getMonth()}`,
      date,
      label: date.toLocaleString('default', { month: 'long', year: 'numeric' }),
    }
  })
})

function prevMonth() {
  const date = props.modelValue
  emit('update:modelValue', new Date(date.getFullYear(), date.getMonth() - 1, 1))
}

function nextMonth() {
  const date = props.modelValue
  emit('update:modelValue', new Date(date.getFullYear(), date.getMonth() + 1, 1))
}

function isSelectedMonth(optionDate: Date): boolean {
  return (
    props.modelValue.getFullYear() === optionDate.getFullYear()
    && props.modelValue.getMonth() === optionDate.getMonth()
  )
}

function selectMonth(optionDate: Date) {
  emit('update:modelValue', new Date(optionDate.getFullYear(), optionDate.getMonth(), 1))
  menuOpen.value = false
}
</script>

<template>
  <div class="month-picker-nav d-flex align-center">
    <v-btn
      variant="outlined"
      size="small"
      icon="mdi-chevron-left"
      aria-label="Previous month"
      @click="prevMonth"
    />

    <v-menu v-model="menuOpen" location="bottom center" offset="6">
      <template #activator="{ props: activatorProps }">
        <v-btn
          v-bind="activatorProps"
          variant="text"
          size="small"
          append-icon="mdi-menu-down"
          class="month-picker-nav__label text-none"
        >
          {{ monthLabel }}
        </v-btn>
      </template>

      <v-list density="compact" class="month-picker-nav__menu">
        <v-list-item
          v-for="option in monthOptions"
          :key="option.key"
          :active="isSelectedMonth(option.date)"
          @click="selectMonth(option.date)"
        >
          <v-list-item-title>{{ option.label }}</v-list-item-title>
        </v-list-item>
      </v-list>
    </v-menu>

    <v-btn
      variant="outlined"
      size="small"
      icon="mdi-chevron-right"
      aria-label="Next month"
      @click="nextMonth"
    />
  </div>
</template>

<style scoped>
.month-picker-nav {
  gap: 4px;
}

.month-picker-nav__label {
  min-width: 0;
  padding-inline: 4px;
  white-space: nowrap;
}

.month-picker-nav__menu {
  max-height: 280px;
  overflow-y: auto;
}
</style>
