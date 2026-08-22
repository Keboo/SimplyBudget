<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import { useAuthStore } from '@/stores/auth'
import type { RuleDto, ExpenseCategoryDto, AccountDto, CurrentUserDto } from '@/types'
import { formatCents } from '@/utils/currency'

const snackbar = useSnackbarStore()
const authStore = useAuthStore()

const PANEL_PROFILE = 0
const PANEL_ACCOUNTS = 1
const PANEL_RULES = 2
const PANEL_CATEGORIES = 3

const openPanel = ref<number | undefined>(undefined)

const profileLoaded = ref(false)
const profileLoading = ref(false)
const profileSaving = ref(false)
const profileDisplayName = ref(authStore.displayName ?? authStore.account?.name ?? '')

const accounts = ref<AccountDto[]>([])
const accountsLoading = ref(false)
const accountsLoaded = ref(false)
const accountEditId = ref<number | null>(null)
const accountEditName = ref('')
const accountNewName = ref('')
const accountAddOpen = ref(false)

const rules = ref<RuleDto[]>([])
const categories = ref<ExpenseCategoryDto[]>([])
const rulesLoading = ref(false)
const rulesLoaded = ref(false)
const addRuleOpen = ref(false)
const addTargetCategoryLabel = ref('')
const deleteRule = ref<RuleDto | null>(null)
const editRule = ref<RuleDto | null>(null)
const ruleForm = ref({ name: '', ruleRegex: '', expenseCategoryId: null as number | null })

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
    const groupedRules = rulesByCategory.get(rule.expenseCategoryId)
    if (groupedRules) {
      groupedRules.push(rule)
    } else {
      rulesByCategory.set(rule.expenseCategoryId, [rule])
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

const categoriesLoaded = ref(false)
const categoriesLoading = ref(false)
const showHiddenCategories = ref(false)
const manageCategories = ref<ExpenseCategoryDto[]>([])
const editCategoryId = ref<number | null>(null)
const editCategoryForm = ref({ name: '', categoryName: '' })
const deleteCategory = ref<ExpenseCategoryDto | null>(null)

async function fetchCurrentUserProfile() {
  profileLoading.value = true
  try {
    const profile = await apiClient.get<CurrentUserDto>('/api/current-user')
    profileDisplayName.value = profile.displayName ?? ''
    authStore.setDisplayName(profile.displayName ?? null)
    profileLoaded.value = true
  } catch {
    snackbar.enqueueSnackbar('Failed to load profile', { variant: 'error' })
  } finally {
    profileLoading.value = false
  }
}

async function handleSaveDisplayName() {
  const displayName = profileDisplayName.value.trim()
  if (!displayName) {
    snackbar.enqueueSnackbar('Display name is required', { variant: 'error' })
    return
  }

  profileSaving.value = true
  try {
    const profile = await apiClient.put<CurrentUserDto>('/api/current-user/display-name', { displayName })
    profileDisplayName.value = profile.displayName ?? displayName
    authStore.setDisplayName(profile.displayName ?? displayName)
    snackbar.enqueueSnackbar('Display name updated', { variant: 'success' })
  } catch {
    snackbar.enqueueSnackbar('Failed to update display name', { variant: 'error' })
  } finally {
    profileSaving.value = false
  }
}

async function fetchAccounts() {
  accountsLoading.value = true
  try {
    accounts.value = await apiClient.get<AccountDto[]>('/api/accounts') ?? []
    accountsLoaded.value = true
  } catch {
    snackbar.enqueueSnackbar('Failed to load accounts', { variant: 'error' })
  } finally {
    accountsLoading.value = false
  }
}

function startEditAccount(account: AccountDto) {
  accountEditId.value = account.id
  accountEditName.value = account.name ?? ''
}

async function handleSaveAccountEdit(id: number) {
  const name = accountEditName.value.trim()
  if (!name) {
    snackbar.enqueueSnackbar('Account name is required', { variant: 'error' })
    return
  }

  try {
    await apiClient.put(`/api/accounts/${id}`, { name })
    snackbar.enqueueSnackbar('Account updated', { variant: 'success' })
    accountEditId.value = null
    void fetchAccounts()
  } catch {
    snackbar.enqueueSnackbar('Failed to update account', { variant: 'error' })
  }
}

async function handleAddAccount() {
  const name = accountNewName.value.trim()
  if (!name) {
    snackbar.enqueueSnackbar('Account name is required', { variant: 'error' })
    return
  }

  try {
    await apiClient.post('/api/accounts', { name })
    snackbar.enqueueSnackbar('Account added', { variant: 'success' })
    accountNewName.value = ''
    accountAddOpen.value = false
    void fetchAccounts()
  } catch {
    snackbar.enqueueSnackbar('Failed to add account', { variant: 'error' })
  }
}

async function handleSetDefaultAccount(id: number) {
  try {
    await apiClient.post(`/api/accounts/${id}/set-default`)
    snackbar.enqueueSnackbar('Default account updated', { variant: 'success' })
    void fetchAccounts()
  } catch {
    snackbar.enqueueSnackbar('Failed to set default', { variant: 'error' })
  }
}

async function fetchRules() {
  rulesLoading.value = true
  try {
    rules.value = await apiClient.get<RuleDto[]>('/api/rules') ?? []
  } catch {
    snackbar.enqueueSnackbar('Failed to load rules', { variant: 'error' })
  } finally {
    rulesLoading.value = false
  }
}

async function fetchRuleCategories() {
  try {
    categories.value = await apiClient.get<ExpenseCategoryDto[]>('/api/expense-categories') ?? []
  } catch {
    snackbar.enqueueSnackbar('Failed to load categories for rules', { variant: 'error' })
  }
}

function resetRuleForm() {
  ruleForm.value = { name: '', ruleRegex: '', expenseCategoryId: null }
}

function openAddRuleForCategory(expenseCategoryId: number | null, categoryLabel: string) {
  resetRuleForm()
  ruleForm.value.expenseCategoryId = expenseCategoryId
  addTargetCategoryLabel.value = categoryLabel
  addRuleOpen.value = true
}

function openEditRule(rule: RuleDto) {
  editRule.value = rule
  ruleForm.value = {
    name: rule.name ?? '',
    ruleRegex: rule.ruleRegex ?? '',
    expenseCategoryId: rule.expenseCategoryId ?? null,
  }
}

async function handleAddRule() {
  try {
    await apiClient.post('/api/rules', {
      name: ruleForm.value.name,
      ruleRegex: ruleForm.value.ruleRegex,
      expenseCategoryId: ruleForm.value.expenseCategoryId,
    })
    snackbar.enqueueSnackbar('Rule added', { variant: 'success' })
    addRuleOpen.value = false
    resetRuleForm()
    addTargetCategoryLabel.value = ''
    void fetchRules()
  } catch {
    snackbar.enqueueSnackbar('Failed to add rule', { variant: 'error' })
  }
}

async function handleEditRule() {
  if (!editRule.value) return
  try {
    await apiClient.put(`/api/rules/${editRule.value.id}`, {
      name: ruleForm.value.name,
      ruleRegex: ruleForm.value.ruleRegex,
      expenseCategoryId: ruleForm.value.expenseCategoryId,
    })
    snackbar.enqueueSnackbar('Rule updated', { variant: 'success' })
    editRule.value = null
    resetRuleForm()
    void Promise.all([fetchRules(), fetchRuleCategories()])
  } catch {
    snackbar.enqueueSnackbar('Failed to update rule', { variant: 'error' })
  }
}

async function handleDeleteRule() {
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

async function fetchManageCategories() {
  categoriesLoading.value = true
  try {
    manageCategories.value = await apiClient.get<ExpenseCategoryDto[]>(
      `/api/expense-categories?includeHidden=${showHiddenCategories.value}`,
    ) ?? []
    categoriesLoaded.value = true
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

watch(showHiddenCategories, () => {
  if (categoriesLoaded.value) {
    void fetchManageCategories()
  }
})

watch(addRuleOpen, (isOpen) => {
  if (!isOpen) {
    resetRuleForm()
    addTargetCategoryLabel.value = ''
  }
})

watch(openPanel, (panel) => {
  if (panel === PANEL_PROFILE && !profileLoaded.value) {
    void fetchCurrentUserProfile()
    return
  }

  if (panel === PANEL_ACCOUNTS && !accountsLoaded.value) {
    void fetchAccounts()
    return
  }

  if (panel === PANEL_RULES && !rulesLoaded.value) {
    rulesLoaded.value = true
    void Promise.all([fetchRules(), fetchRuleCategories()])
    return
  }

  if (panel === PANEL_CATEGORIES && !categoriesLoaded.value) {
    void fetchManageCategories()
  }
})
</script>

<template>
  <div>
    <h4 class="text-h4 mb-4">Settings</h4>

    <v-expansion-panels v-model="openPanel" variant="accordion">
      <v-expansion-panel>
        <v-expansion-panel-title>
          <div class="w-100">
            <div class="text-subtitle-1 font-weight-medium">Profile</div>
            <div class="text-body-2 text-medium-emphasis">Change how your name is shown in the app.</div>
          </div>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <div v-if="profileLoading" class="d-flex justify-center pa-8">
            <v-progress-circular indeterminate aria-label="Loading profile" />
          </div>
          <div v-else class="d-flex flex-column" style="gap: 12px; max-width: 420px;">
            <v-text-field
              v-model="profileDisplayName"
              label="Display Name"
              hint="Used in the header and assignee lists"
              persistent-hint
            />
            <div>
              <v-btn color="primary" :loading="profileSaving" @click="handleSaveDisplayName">Save Display Name</v-btn>
            </div>
          </div>
        </v-expansion-panel-text>
      </v-expansion-panel>

      <v-expansion-panel>
        <v-expansion-panel-title>
          <div class="w-100 d-flex justify-space-between align-center">
            <div>
              <div class="text-subtitle-1 font-weight-medium">Accounts</div>
              <div class="text-body-2 text-medium-emphasis">Add accounts, rename them, and choose the default.</div>
            </div>
            <v-chip size="small" variant="tonal">{{ accounts.length }}</v-chip>
          </div>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <div class="d-flex justify-space-between align-center mb-4">
            <h5 class="text-h5">Accounts</h5>
            <v-btn color="primary" @click="accountAddOpen = true">Add Account</v-btn>
          </div>

          <div v-if="accountsLoading" class="d-flex justify-center pa-8">
            <v-progress-circular indeterminate aria-label="Loading accounts" />
          </div>
          <v-list v-else>
            <v-list-item v-if="accounts.length === 0">
              <v-list-item-title>No accounts found.</v-list-item-title>
            </v-list-item>
            <v-card v-for="account in accounts" :key="account.id" class="mb-2">
              <v-list-item>
                <v-list-item-title>
                  <div class="d-flex align-center" style="gap: 8px;">
                    <v-text-field
                      v-if="accountEditId === account.id"
                      v-model="accountEditName"
                      density="compact"
                      hide-details
                      autofocus
                      style="max-width: 240px;"
                      @keydown.enter="handleSaveAccountEdit(account.id)"
                    />
                    <span v-else>{{ account.name }}</span>
                    <v-chip v-if="account.isDefault" size="small" color="primary">Default</v-chip>
                  </div>
                </v-list-item-title>
                <v-list-item-subtitle>
                  Balance: {{ formatCents(account.currentAmount) }} · Validated: {{ new Date(account.validatedDate).toLocaleDateString() }}
                </v-list-item-subtitle>
                <template #append>
                  <div v-if="accountEditId === account.id" class="d-flex" style="gap: 4px;">
                    <v-btn icon="mdi-content-save" color="primary" variant="text" aria-label="Save" @click="handleSaveAccountEdit(account.id)" />
                    <v-btn icon="mdi-close" variant="text" aria-label="Cancel" @click="accountEditId = null" />
                  </div>
                  <div v-else class="d-flex align-center" style="gap: 4px;">
                    <v-btn v-if="!account.isDefault" size="small" variant="text" @click="handleSetDefaultAccount(account.id)">Set Default</v-btn>
                    <v-btn icon="mdi-pencil" variant="text" aria-label="Edit account name" @click="startEditAccount(account)" />
                  </div>
                </template>
              </v-list-item>
            </v-card>
          </v-list>
        </v-expansion-panel-text>
      </v-expansion-panel>

      <v-expansion-panel>
        <v-expansion-panel-title>
          <div class="w-100 d-flex justify-space-between align-center">
            <div>
              <div class="text-subtitle-1 font-weight-medium">Import Rules</div>
              <div class="text-body-2 text-medium-emphasis">Control automatic category suggestions while importing.</div>
            </div>
            <v-chip size="small" variant="tonal">{{ rules.length }}</v-chip>
          </div>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <div v-if="rulesLoading" class="d-flex justify-center pa-8">
            <v-progress-circular indeterminate aria-label="Loading import rules" />
          </div>
          <div v-else>
            <v-card v-for="group in ruleGroups" :key="group.key" class="mb-4">
              <v-card-title class="d-flex justify-space-between align-center">
                <span>{{ group.label }}</span>
                <v-btn
                  size="small"
                  color="primary"
                  @click="openAddRuleForCategory(group.expenseCategoryId, group.label)"
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
                      <v-btn icon="mdi-pencil" variant="text" aria-label="Edit rule" @click="openEditRule(rule)" />
                      <v-btn icon="mdi-delete" variant="text" color="error" aria-label="Delete rule" @click="deleteRule = rule" />
                    </div>
                  </template>
                </v-list-item>
              </v-list>
            </v-card>
            <v-alert v-if="rules.length === 0" type="info" variant="tonal">No rules defined yet.</v-alert>
          </div>
        </v-expansion-panel-text>
      </v-expansion-panel>

      <v-expansion-panel>
        <v-expansion-panel-title>
          <div class="w-100 d-flex justify-space-between align-center">
            <div>
              <div class="text-subtitle-1 font-weight-medium">Expense Categories</div>
              <div class="text-body-2 text-medium-emphasis">Rename, hide, restore, or delete categories.</div>
            </div>
            <v-chip size="small" variant="tonal">{{ manageCategories.length }}</v-chip>
          </div>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
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
        </v-expansion-panel-text>
      </v-expansion-panel>
    </v-expansion-panels>

    <v-dialog v-model="accountAddOpen" max-width="480">
      <v-card>
        <v-card-title>Add Account</v-card-title>
        <v-card-text>
          <v-text-field label="Account Name" v-model="accountNewName" autofocus @keydown.enter="handleAddAccount" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="accountAddOpen = false">Cancel</v-btn>
          <v-btn color="primary" @click="handleAddAccount">Add</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="addRuleOpen" max-width="500">
      <v-card>
        <v-card-title>Add Rule</v-card-title>
        <v-card-text class="d-flex flex-column" style="gap: 16px;">
          <v-text-field label="Name" v-model="ruleForm.name" />
          <v-text-field label="Regex Pattern" v-model="ruleForm.ruleRegex" />
          <v-text-field label="Target Category" :model-value="addTargetCategoryLabel" readonly />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="addRuleOpen = false">Cancel</v-btn>
          <v-btn color="primary" @click="handleAddRule">Add</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog :model-value="!!editRule" max-width="500" @update:model-value="(val: boolean) => !val && (editRule = null)">
      <v-card>
        <v-card-title>Edit Rule</v-card-title>
        <v-card-text class="d-flex flex-column" style="gap: 16px;">
          <v-text-field label="Name" v-model="ruleForm.name" />
          <v-text-field label="Regex Pattern" v-model="ruleForm.ruleRegex" />
          <v-select label="Category" :items="categoryOptions" item-title="name" item-value="id" v-model="ruleForm.expenseCategoryId" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="editRule = null">Cancel</v-btn>
          <v-btn color="primary" @click="handleEditRule">Save</v-btn>
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
          <v-btn color="error" @click="handleDeleteRule">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

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
