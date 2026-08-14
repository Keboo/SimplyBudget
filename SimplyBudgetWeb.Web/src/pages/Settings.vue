<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { RuleDto, ExpenseCategoryDto } from '@/types'

const snackbar = useSnackbarStore()

const rules = ref<RuleDto[]>([])
const categories = ref<ExpenseCategoryDto[]>([])
const loading = ref(false)
const addOpen = ref(false)
const deleteRule = ref<RuleDto | null>(null)
const editRule = ref<RuleDto | null>(null)

const form = ref({ name: '', ruleRegex: '', expenseCategoryId: null as number | null })

const categoryOptions = computed(() => [{ id: null, name: 'None' }, ...categories.value])

async function fetchRules() {
  loading.value = true
  try {
    rules.value = await apiClient.get<RuleDto[]>('/api/rules') ?? []
  } catch {
    snackbar.enqueueSnackbar('Failed to load rules', { variant: 'error' })
  } finally {
    loading.value = false
  }
}

async function fetchCategories() {
  try {
    categories.value = await apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories') ?? []
  } catch { /* ignore */ }
}

function resetForm() {
  form.value = { name: '', ruleRegex: '', expenseCategoryId: null }
}

async function handleAdd() {
  try {
    await apiClient.post('/api/rules', {
      name: form.value.name,
      ruleRegex: form.value.ruleRegex,
      expenseCategoryId: form.value.expenseCategoryId,
    })
    snackbar.enqueueSnackbar('Rule added', { variant: 'success' })
    addOpen.value = false
    resetForm()
    void fetchRules()
  } catch {
    snackbar.enqueueSnackbar('Failed to add rule', { variant: 'error' })
  }
}

async function handleEdit() {
  if (!editRule.value) return
  try {
    await apiClient.put(`/api/rules/${editRule.value.id}`, {
      name: form.value.name,
      ruleRegex: form.value.ruleRegex,
      expenseCategoryId: form.value.expenseCategoryId,
    })
    snackbar.enqueueSnackbar('Rule updated', { variant: 'success' })
    editRule.value = null
    resetForm()
    void fetchRules()
  } catch {
    snackbar.enqueueSnackbar('Failed to update rule', { variant: 'error' })
  }
}

async function handleDelete() {
  if (!deleteRule.value) return
  try {
    await apiClient.delete(`/api/rules/${deleteRule.value.id}`)
    snackbar.enqueueSnackbar('Rule deleted', { variant: 'success' })
    deleteRule.value = null
    void fetchRules()
  } catch {
    snackbar.enqueueSnackbar('Failed to delete rule', { variant: 'error' })
  }
}

function openEdit(rule: RuleDto) {
  editRule.value = rule
  form.value = {
    name: rule.name ?? '',
    ruleRegex: rule.ruleRegex ?? '',
    expenseCategoryId: rule.expenseCategoryId ?? null,
  }
}

function openAdd() {
  resetForm()
  addOpen.value = true
}

onMounted(() => {
  void fetchRules()
  void fetchCategories()
})
</script>

<template>
  <div>
    <div class="d-flex justify-space-between align-center mb-4">
      <h5 class="text-h5">Import Rules</h5>
      <v-btn color="primary" @click="openAdd">Add Rule</v-btn>
    </div>

    <div v-if="loading" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate aria-label="Loading import rules" />
    </div>
    <v-list v-else>
      <v-list-item v-if="rules.length === 0">
        <v-list-item-title>No rules defined.</v-list-item-title>
      </v-list-item>
      <v-card v-for="rule in rules" :key="rule.id" class="mb-2">
        <v-list-item>
          <v-list-item-title>{{ rule.name }}</v-list-item-title>
          <v-list-item-subtitle>Pattern: {{ rule.ruleRegex ?? '—' }} · Category: {{ rule.categoryName ?? 'None' }}</v-list-item-subtitle>
          <template #append>
            <div class="d-flex" style="gap: 4px;">
              <v-btn icon="mdi-pencil" variant="text" aria-label="Edit rule" @click="openEdit(rule)" />
              <v-btn icon="mdi-delete" variant="text" color="error" aria-label="Delete rule" @click="deleteRule = rule" />
            </div>
          </template>
        </v-list-item>
      </v-card>
    </v-list>

    <v-dialog v-model="addOpen" max-width="500">
      <v-card>
        <v-card-title>Add Rule</v-card-title>
        <v-card-text class="d-flex flex-column" style="gap: 16px;">
          <v-text-field label="Name" v-model="form.name" />
          <v-text-field label="Regex Pattern" v-model="form.ruleRegex" />
          <v-select label="Category" :items="categoryOptions" item-title="name" item-value="id" v-model="form.expenseCategoryId" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="addOpen = false">Cancel</v-btn>
          <v-btn color="primary" @click="handleAdd">Add</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog :model-value="!!editRule" max-width="500" @update:model-value="(val: boolean) => !val && (editRule = null)">
      <v-card>
        <v-card-title>Edit Rule</v-card-title>
        <v-card-text class="d-flex flex-column" style="gap: 16px;">
          <v-text-field label="Name" v-model="form.name" />
          <v-text-field label="Regex Pattern" v-model="form.ruleRegex" />
          <v-select label="Category" :items="categoryOptions" item-title="name" item-value="id" v-model="form.expenseCategoryId" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="editRule = null">Cancel</v-btn>
          <v-btn color="primary" @click="handleEdit">Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog :model-value="!!deleteRule" max-width="480" @update:model-value="(val: boolean) => !val && (deleteRule = null)">
      <v-card>
        <v-card-title>Confirm Delete</v-card-title>
        <v-card-text>Delete rule "{{ deleteRule?.name }}"?</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="deleteRule = null">Cancel</v-btn>
          <v-btn color="error" @click="handleDelete">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>
