<script setup lang="ts">
import { ref } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { ExpenseCategoryDto, TransactionRequest, TransferRequest } from '@/types'

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
  expenseCategoryId: number | null
  amount: string
}

const emptyLine = (): LineItem => ({ expenseCategoryId: null, amount: '' })

const tab = ref<'transaction' | 'income' | 'transfer'>('transaction')
const description = ref('')
const date = ref(new Date().toISOString().split('T')[0])
const lines = ref<LineItem[]>([emptyLine()])
const transferAmount = ref('')
const fromCategoryId = ref<number | null>(null)
const toCategoryId = ref<number | null>(null)
const submitting = ref(false)

function addLine() {
  lines.value.push(emptyLine())
}

function removeLine(index: number) {
  lines.value = lines.value.filter((_, i) => i !== index)
}

function resetForm() {
  description.value = ''
  date.value = new Date().toISOString().split('T')[0]
  lines.value = [emptyLine()]
  transferAmount.value = ''
  fromCategoryId.value = null
  toCategoryId.value = null
  tab.value = 'transaction'
}

function close() {
  resetForm()
  emit('update:modelValue', false)
}

function dollarsToCents(s: string) {
  return Math.round(parseFloat(s) * 100)
}

async function submit() {
  submitting.value = true
  try {
    if (tab.value === 'transaction' || tab.value === 'income') {
      const endpoint = tab.value === 'transaction' ? '/api/transactions/transaction' : '/api/transactions/income'
      const payload: TransactionRequest = {
        description: description.value,
        date: date.value,
        items: lines.value
          .filter(l => l.expenseCategoryId !== null && l.amount !== '')
          .map(l => ({
            expenseCategoryId: l.expenseCategoryId as number,
            amount: dollarsToCents(l.amount),
          })),
      }
      await apiClient.post(endpoint, payload)
      snackbar.enqueueSnackbar(tab.value === 'transaction' ? 'Transaction added' : 'Income added', { variant: 'success' })
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
      <v-card-text>
        <v-tabs v-model="tab" class="mb-4">
          <v-tab value="transaction">Transaction</v-tab>
          <v-tab value="income">Income</v-tab>
          <v-tab value="transfer">Transfer</v-tab>
        </v-tabs>

        <div class="d-flex flex-column" style="gap: 16px;">
          <v-text-field label="Description" v-model="description" />
          <v-text-field label="Date" type="date" v-model="date" />

          <template v-if="tab === 'transaction' || tab === 'income'">
            <span class="text-subtitle-2">Items</span>
            <div v-for="(item, index) in lines" :key="index" class="d-flex align-center mb-2" style="gap: 8px;">
              <v-select
                label="Category"
                :items="props.categories"
                item-title="name"
                item-value="id"
                v-model="item.expenseCategoryId"
                style="flex: 2;"
                density="compact"
              />
              <v-text-field
                label="Amount ($)"
                type="number"
                step="0.01"
                min="0"
                v-model="item.amount"
                style="flex: 1;"
                density="compact"
              />
              <v-btn v-if="lines.length > 1" icon="mdi-minus" size="small" variant="text" aria-label="Remove line" @click="removeLine(index)" />
            </div>
            <v-btn size="small" prepend-icon="mdi-plus" variant="text" @click="addLine">Add Line</v-btn>
          </template>

          <template v-else>
            <v-text-field label="Amount ($)" type="number" step="0.01" min="0" v-model="transferAmount" />
            <v-select label="From Category" :items="props.categories" item-title="name" item-value="id" v-model="fromCategoryId" />
            <v-select label="To Category" :items="props.categories" item-title="name" item-value="id" v-model="toCategoryId" />
          </template>
        </div>
      </v-card-text>
      <v-card-actions>
        <v-spacer />
        <v-btn @click="close">Cancel</v-btn>
        <v-btn color="primary" :loading="submitting" @click="submit">Save</v-btn>
      </v-card-actions>
    </v-card>
  </v-dialog>
</template>
