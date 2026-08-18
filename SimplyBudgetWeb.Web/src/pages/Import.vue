<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type {
  ImportItemDto,
  ExpenseCategoryDto,
  BudgetDataExportPackageDto,
} from '@/types'
import { formatCents } from '@/utils/currency'

const snackbar = useSnackbarStore()

const csvText = ref('')
const items = ref<ImportItemDto[]>([])
const categories = ref<ExpenseCategoryDto[]>([])
const loading = ref(false)
const submitting = ref(false)
const exportingData = ref(false)
const importingData = ref(false)
const importFile = ref<File | null>(null)
const importFileInput = ref<HTMLInputElement | null>(null)

async function fetchCategories() {
  try {
    categories.value = await apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories') ?? []
  } catch { /* ignore */ }
}

async function handleParse() {
  if (!csvText.value.trim()) return
  loading.value = true
  try {
    const data = await apiClient.post<ImportItemDto[]>('/api/import/parse', { csv: csvText.value })
    items.value = data ?? []
    snackbar.enqueueSnackbar(`Parsed ${data?.length ?? 0} items`, { variant: 'success' })
  } catch {
    snackbar.enqueueSnackbar('Failed to parse CSV', { variant: 'error' })
  } finally {
    loading.value = false
  }
}

function updateCategory(index: number, categoryId: number | null) {
  const item = items.value[index]
  if (!item) return
  const cat = categoryId !== null ? categories.value.find(c => c.id === categoryId) : undefined
  item.suggestedCategoryId = categoryId
  item.suggestedCategoryName = cat?.name ?? null
}

async function handleImport() {
  submitting.value = true
  try {
    await apiClient.post('/api/import/save', items.value)
    snackbar.enqueueSnackbar('Import saved', { variant: 'success' })
    items.value = []
    csvText.value = ''
  } catch {
    snackbar.enqueueSnackbar('Failed to save import', { variant: 'error' })
  } finally {
    submitting.value = false
  }
}

function onImportFileSelected(event: Event) {
  const input = event.target as HTMLInputElement
  importFile.value = input.files?.item(0) ?? null
}

async function handleExportAllData() {
  exportingData.value = true
  try {
    const result = await apiClient.download('/api/data-portability/export')
    const fileName = result.fileName ?? `simplybudget-export-${new Date().toISOString().replace(/[:.]/g, '-')}.json`
    const url = URL.createObjectURL(result.blob)
    const link = document.createElement('a')
    link.href = url
    link.download = fileName
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
    snackbar.enqueueSnackbar('Data export downloaded', { variant: 'success' })
  } catch {
    snackbar.enqueueSnackbar('Failed to export data', { variant: 'error' })
  } finally {
    exportingData.value = false
  }
}

async function handleImportAllData() {
  if (!importFile.value) return

  importingData.value = true
  try {
    const fileContent = await importFile.value.text()
    const payload = JSON.parse(fileContent) as BudgetDataExportPackageDto
    await apiClient.post('/api/data-portability/import', payload)
    importFile.value = null
    if (importFileInput.value) {
      importFileInput.value.value = ''
    }
    snackbar.enqueueSnackbar('Data import completed', { variant: 'success' })
  } catch {
    snackbar.enqueueSnackbar('Failed to import data export', { variant: 'error' })
  } finally {
    importingData.value = false
  }
}

onMounted(() => { void fetchCategories() })
</script>

<template>
  <div>
    <h5 class="text-h5 mb-4">Import Transactions</h5>

    <v-card class="pa-4 mb-4">
      <h6 class="text-h6 mb-2">Full Data Transfer</h6>
      <p class="text-body-2 mb-4">
        Export all accounts, categories, transactions, rules, and metadata into a JSON file, or import a previous export.
      </p>
      <div class="d-flex flex-wrap align-center" style="gap: 12px;">
        <v-btn color="primary" :loading="exportingData" :disabled="exportingData || importingData" @click="handleExportAllData">
          Export All Data
        </v-btn>
        <input
          ref="importFileInput"
          type="file"
          accept=".json,application/json"
          :disabled="importingData || exportingData"
          @change="onImportFileSelected"
        />
        <v-btn
          color="secondary"
          :loading="importingData"
          :disabled="importingData || exportingData || !importFile"
          @click="handleImportAllData"
        >
          Import Export File
        </v-btn>
      </div>
      <p v-if="importFile" class="text-body-2 mt-3 mb-0">Selected file: {{ importFile.name }}</p>
    </v-card>

    <v-card class="pa-4 mb-4">
      <v-textarea
        label="Paste CSV data here"
        v-model="csvText"
        rows="8"
        placeholder="date,description,amount..."
        class="mb-4"
      />
      <v-btn color="primary" :loading="loading" :disabled="loading || !csvText.trim()" @click="handleParse">
        Parse
      </v-btn>
    </v-card>

    <template v-if="items.length > 0">
      <v-card class="mb-4" style="overflow: auto;">
        <v-table density="compact">
          <thead>
            <tr>
              <th>Done</th>
              <th>Date</th>
              <th>Description</th>
              <th style="text-align: right;">Amount</th>
              <th>Type</th>
              <th>Category</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(item, index) in items" :key="index" :style="{ opacity: item.isDone ? 0.5 : 1 }">
              <td><v-checkbox v-model="item.isDone" hide-details density="compact" /></td>
              <td>{{ new Date(item.date).toLocaleDateString() }}</td>
              <td>{{ item.description }}</td>
              <td style="text-align: right;">{{ formatCents(item.amount) }}</td>
              <td>{{ item.isDebit ? 'Debit' : 'Credit' }}</td>
              <td>
                <v-select
                  :items="[{ id: null, name: 'None' }, ...categories]"
                  item-title="name"
                  item-value="id"
                  :model-value="item.suggestedCategoryId"
                  density="compact"
                  hide-details
                  style="min-width: 150px;"
                  @update:model-value="(val: number | null) => updateCategory(index, val)"
                />
              </td>
            </tr>
          </tbody>
        </v-table>
      </v-card>
      <v-btn color="primary" :loading="submitting" :disabled="submitting" @click="handleImport">Save Import</v-btn>
    </template>
  </div>
</template>
