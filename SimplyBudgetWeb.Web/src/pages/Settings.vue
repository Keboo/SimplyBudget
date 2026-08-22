<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { RuleDto, ExpenseCategoryDto } from '@/types'

const snackbar = useSnackbarStore()

const rules = ref<RuleDto[]>([])
const categories = ref<ExpenseCategoryDto[]>([])
const loading = ref(false)
const addOpen = ref(false)
const addTargetCategoryLabel = ref('')
const deleteRule = ref<RuleDto | null>(null)
const editRule = ref<RuleDto | null>(null)

const form = ref({ name: '', ruleRegex: '', expenseCategoryId: null as number | null })

const categoryOptions = computed(() => [{ id: null, name: 'None' }, ...categories.value])

interface RuleGroup {
  key: string
  expenseCategoryId: number | null
  label: string
  rules: RuleDto[]
}

const ruleGroups = computed<RuleGroup[]>(() => {
  const groups: RuleGroup[] = []
  const knownCategoryIds = new Set<number>()
  const rulesByCategory = new Map<number | null, RuleDto[]>()

  for (const rule of rules.value) {
    const key = rule.expenseCategoryId
    const groupedRules = rulesByCategory.get(key)
    if (groupedRules) {
      groupedRules.push(rule)
    } else {
      rulesByCategory.set(key, [rule])
    }
  }

  for (const category of categories.value) {
    knownCategoryIds.add(category.id)
    groups.push({
      key: `category-${category.id}`,
      expenseCategoryId: category.id,
      label: category.name ?? `Category ${category.id}`,
      rules: rulesByCategory.get(category.id) ?? [],
    })
  }

  groups.push({
    key: 'category-none',
    expenseCategoryId: null,
    label: 'No category',
    rules: rulesByCategory.get(null) ?? [],
  })

  for (const [categoryId, groupedRules] of rulesByCategory.entries()) {
    if (categoryId === null || knownCategoryIds.has(categoryId)) continue
    groups.push({
      key: `category-missing-${categoryId}`,
      expenseCategoryId: categoryId,
      label: groupedRules[0]?.categoryName ?? `Unknown category (${categoryId})`,
      rules: groupedRules,
    })
  }

  return groups
})

// Expense category management (rename / hide / delete)
const manageCategories = ref<ExpenseCategoryDto[]>([])
const categoriesLoading = ref(false)
const showHiddenCategories = ref(false)
const editCategoryId = ref<number | null>(null)
const editCategoryForm = ref({ name: '', categoryName: '' })
const deleteCategory = ref<ExpenseCategoryDto | null>(null)

async function fetchManageCategories() {
  categoriesLoading.value = true
  try {
    manageCategories.value = await apiClient.get<ExpenseCategoryDto[]>(
      `/api/expense-categories?includeHidden=${showHiddenCategories.value}`,
    ) ?? []
  } catch {
    snackbar.enqueueSnackbar('Failed to load expense categories', { variant: 'error' })
  } finally {
    categoriesLoading.value = false
  }
}

function startEditCategory(category: ExpenseCategoryDto) {
  editCategoryId.value = category.id
  editCategoryForm.value = { name: category.name ?? '', categoryName: category.categoryName ?? '' }
}

async function handleSaveCategoryEdit(category: ExpenseCategoryDto) {
  try {
    await apiClient.put(`/api/expense-categories/${category.id}`, {
      name: editCategoryForm.value.name,
      categoryName: editCategoryForm.value.categoryName,
      budgetedAmount: category.budgetedAmount,
      budgetedPercentage: category.budgetedPercentage,
      cap: category.cap,
      accountId: category.accountId,
    })
    snackbar.enqueueSnackbar('Category updated', { variant: 'success' })
    editCategoryId.value = null
    void fetchManageCategories()
  } catch {
    snackbar.enqueueSnackbar('Failed to update category', { variant: 'error' })
  }
}

async function handleToggleHideCategory(category: ExpenseCategoryDto) {
  try {
    if (category.isHidden) {
      await apiClient.post(`/api/expense-categories/${category.id}/restore`)
      snackbar.enqueueSnackbar('Category restored', { variant: 'success' })
    } else {
      await apiClient.post(`/api/expense-categories/${category.id}/hide`)
      snackbar.enqueueSnackbar('Category hidden', { variant: 'success' })
    }
    void fetchManageCategories()
  } catch {
    snackbar.enqueueSnackbar('Failed to update category', { variant: 'error' })
  }
}

async function handleDeleteCategory() {
  if (!deleteCategory.value) return
  try {
    await apiClient.delete(`/api/expense-categories/${deleteCategory.value.id}`)
    snackbar.enqueueSnackbar('Category deleted', { variant: 'success' })
    deleteCategory.value = null
    void fetchManageCategories()
  } catch (err) {
    snackbar.enqueueSnackbar(err instanceof Error ? err.message : 'Failed to delete category', { variant: 'error' })
    deleteCategory.value = null
  }
}

watch(showHiddenCategories, () => void fetchManageCategories())

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

function openAddForCategory(expenseCategoryId: number | null, categoryLabel: string) {
  resetForm()
  form.value.expenseCategoryId = expenseCategoryId
  addTargetCategoryLabel.value = categoryLabel
  addOpen.value = true
}

function closeAdd() {
  addOpen.value = false
}

watch(addOpen, (isOpen) => {
  if (!isOpen) {
    resetForm()
    addTargetCategoryLabel.value = ''
  }
})

onMounted(() => {
  void fetchRules()
  void fetchCategories()
  void fetchManageCategories()
})
</script>

<template>
  <div>
    <h5 class="text-h5 mb-4">Import Rules</h5>

    <div v-if="loading" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate aria-label="Loading import rules" />
    </div>
    <div v-else>
      <v-card
        v-for="group in ruleGroups"
        :key="group.key"
        class="mb-4"
      >
        <v-card-title class="d-flex justify-space-between align-center">
          <span>{{ group.label }}</span>
          <v-btn
            size="small"
            color="primary"
            @click="openAddForCategory(group.expenseCategoryId, group.label)"
          >
            Add Rule
          </v-btn>
        </v-card-title>
        <v-divider />
        <v-list>
          <v-list-item v-if="group.rules.length === 0">
            <v-list-item-title>No rules in this category.</v-list-item-title>
          </v-list-item>
          <v-list-item v-for="rule in group.rules" :key="rule.id">
            <v-list-item-title>{{ rule.name }}</v-list-item-title>
            <v-list-item-subtitle>Pattern: {{ rule.ruleRegex ?? '—' }}</v-list-item-subtitle>
            <template #append>
              <div class="d-flex" style="gap: 4px;">
                <v-btn icon="mdi-pencil" variant="text" aria-label="Edit rule" @click="openEdit(rule)" />
                <v-btn icon="mdi-delete" variant="text" color="error" aria-label="Delete rule" @click="deleteRule = rule" />
              </div>
            </template>
          </v-list-item>
        </v-list>
      </v-card>
      <v-alert v-if="rules.length === 0" type="info" variant="tonal">No rules defined yet.</v-alert>
    </div>

    <v-dialog :model-value="addOpen" max-width="500" @update:model-value="(val: boolean) => !val && closeAdd()">
      <v-card>
        <v-card-title>Add Rule</v-card-title>
        <v-card-text class="d-flex flex-column" style="gap: 16px;">
          <v-text-field label="Name" v-model="form.name" />
          <v-text-field label="Regex Pattern" v-model="form.ruleRegex" />
          <v-text-field label="Target Category" :model-value="addTargetCategoryLabel" readonly />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="closeAdd">Cancel</v-btn>
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

    <v-divider class="my-6" />

    <div class="d-flex justify-space-between align-center mb-4">
      <h5 class="text-h5">Expense Categories</h5>
      <v-switch
        v-model="showHiddenCategories"
        label="Show hidden"
        density="compact"
        hide-details
        color="primary"
      />
    </div>

    <div v-if="categoriesLoading" class="d-flex justify-center pa-8">
      <v-progress-circular indeterminate aria-label="Loading expense categories" />
    </div>
    <v-list v-else>
      <v-list-item v-if="manageCategories.length === 0">
        <v-list-item-title>No expense categories defined.</v-list-item-title>
      </v-list-item>
      <v-card v-for="category in manageCategories" :key="category.id" class="mb-2">
        <v-list-item>
          <v-list-item-title>
            <div v-if="editCategoryId === category.id" class="d-flex align-center" style="gap: 8px;">
              <v-text-field
                v-model="editCategoryForm.name"
                label="Name"
                density="compact"
                hide-details
                autofocus
                style="max-width: 220px;"
                @keydown.enter="handleSaveCategoryEdit(category)"
              />
              <v-text-field
                v-model="editCategoryForm.categoryName"
                label="Group"
                density="compact"
                hide-details
                style="max-width: 220px;"
                @keydown.enter="handleSaveCategoryEdit(category)"
              />
            </div>
            <div v-else class="d-flex align-center" style="gap: 8px;">
              <span>{{ category.name }}</span>
              <v-chip v-if="category.isHidden" size="small">Hidden</v-chip>
            </div>
          </v-list-item-title>
          <v-list-item-subtitle v-if="editCategoryId !== category.id">
            {{ category.categoryName ?? 'No group' }}
          </v-list-item-subtitle>
          <template #append>
            <div v-if="editCategoryId === category.id" class="d-flex" style="gap: 4px;">
              <v-btn icon="mdi-content-save" color="primary" variant="text" aria-label="Save" @click="handleSaveCategoryEdit(category)" />
              <v-btn icon="mdi-close" variant="text" aria-label="Cancel" @click="editCategoryId = null" />
            </div>
            <div v-else class="d-flex align-center" style="gap: 4px;">
              <v-btn icon="mdi-pencil" variant="text" aria-label="Rename category" @click="startEditCategory(category)" />
              <v-btn
                :icon="category.isHidden ? 'mdi-eye' : 'mdi-eye-off'"
                variant="text"
                :aria-label="category.isHidden ? 'Restore category' : 'Hide category'"
                @click="handleToggleHideCategory(category)"
              />
              <v-tooltip :text="category.hasItems ? 'Categories with items cannot be deleted' : 'Delete category'">
                <template #activator="{ props }">
                  <span v-bind="props">
                    <v-btn
                      icon="mdi-delete"
                      variant="text"
                      color="error"
                      :disabled="category.hasItems"
                      aria-label="Delete category"
                      @click="deleteCategory = category"
                    />
                  </span>
                </template>
              </v-tooltip>
            </div>
          </template>
        </v-list-item>
      </v-card>
    </v-list>

    <v-dialog :model-value="!!deleteCategory" max-width="480" @update:model-value="(val: boolean) => !val && (deleteCategory = null)">
      <v-card>
        <v-card-title>Confirm Delete</v-card-title>
        <v-card-text>Permanently delete category "{{ deleteCategory?.name }}"? This cannot be undone.</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="deleteCategory = null">Cancel</v-btn>
          <v-btn color="error" @click="handleDeleteCategory">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>
  </div>
</template>
