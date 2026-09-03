<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import { useAuthStore } from '@/stores/auth'
import type {
  RuleDto,
  ExternalLinkRuleDto,
  ExpenseCategoryDto,
  AccountDto,
  CalculatorTaxOptionsDto,
  CurrentUserDto,
} from '@/types'
import { formatCents, centsToDollars, dollarsToCents } from '@/utils/currency'
import { resetExternalLinkRulesCache } from '@/utils/externalLinks'
import CategorySelector from '@/components/CategorySelector.vue'

const snackbar = useSnackbarStore()
const authStore = useAuthStore()

const PANEL_PROFILE = 0
const PANEL_TAX_OPTIONS = 1
const PANEL_ACCOUNTS = 2
const PANEL_RULES = 3
const PANEL_EXTERNAL_LINKS = 4
const PANEL_CATEGORIES = 5

const openPanel = ref<number | undefined>(undefined)

const profileLoaded = ref(false)
const profileLoading = ref(false)
const profileSaving = ref(false)
const profileDisplayName = ref(authStore.displayName ?? authStore.account?.name ?? '')

interface TaxOptionFormRow {
  name: string
  percentage: string
}

const taxOptionsLoading = ref(false)
const taxOptionsLoaded = ref(false)
const taxOptionsSaving = ref(false)
const taxOptions = ref<TaxOptionFormRow[]>([])
const taxDefaultKey = ref('none')

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
const deleteRule = ref<RuleDto | null>(null)
const editRule = ref<RuleDto | null>(null)
const ruleForm = ref({ name: '', ruleRegex: '', notes: '', minimumAmount: '', maximumAmount: '', expenseCategoryId: null as number | null })

/** Converts a dollars text field into cents, treating blank input as "no limit". */
function optionalDollarsToCents(value: string): number | null {
  const trimmed = value.trim()
  if (trimmed.length === 0) return null
  const parsed = Number.parseFloat(trimmed)
  return Number.isNaN(parsed) ? null : Math.round(parsed * 100)
}

function ruleAmountRangeText(rule: RuleDto): string | null {
  if (rule.minimumAmount === null && rule.maximumAmount === null) return null
  if (rule.minimumAmount !== null && rule.maximumAmount !== null)
    return `${formatCents(rule.minimumAmount)} – ${formatCents(rule.maximumAmount)}`
  if (rule.minimumAmount !== null) return `at least ${formatCents(rule.minimumAmount)}`
  return `at most ${formatCents(rule.maximumAmount as number)}`
}

const externalLinks = ref<ExternalLinkRuleDto[]>([])
const externalLinksLoading = ref(false)
const externalLinksLoaded = ref(false)
const addExternalLinkOpen = ref(false)
const editExternalLink = ref<ExternalLinkRuleDto | null>(null)
const deleteExternalLink = ref<ExternalLinkRuleDto | null>(null)
const externalLinkForm = ref({ name: '', ruleRegex: '', url: '' })

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

  groups.sort((a, b) => a.label.localeCompare(b.label))

  return groups
})

const categoriesLoaded = ref(false)
const categoriesLoading = ref(false)
const showHiddenCategories = ref(false)
const manageCategories = ref<ExpenseCategoryDto[]>([])
const editCategoryId = ref<number | null>(null)
const editCategoryForm = ref({
  name: '',
  description: '',
  categoryName: '',
  budgetType: 'fixed' as 'fixed' | 'percentage',
  budgetedAmount: '0.00',
  budgetedPercentage: '0',
  cap: '',
})
const deleteCategory = ref<ExpenseCategoryDto | null>(null)
const addCategoryOpen = ref(false)
const addCategorySaving = ref(false)
const newCategoryForm = ref({
  name: '',
  description: '',
  categoryName: '',
  budgetType: 'fixed' as 'fixed' | 'percentage',
  budgetedAmount: '0.00',
  budgetedPercentage: '0',
  cap: '',
  accountId: null as number | null,
})

function parseOptionalNonNegativeDollars(value: string): { valid: boolean; value: number | null } {
  const trimmed = value.trim()
  if (trimmed.length === 0) {
    return { valid: true, value: null }
  }

  const parsed = Number.parseFloat(trimmed)
  if (!Number.isFinite(parsed) || parsed < 0) {
    return { valid: false, value: null }
  }

  return { valid: true, value: Math.round(parsed * 100) }
}

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

function applyTaxOptionsResponse(response: CalculatorTaxOptionsDto) {
  const options = response.options ?? []
  taxOptions.value = options.map(option => ({
    name: option.name ?? '',
    percentage: option.percentage.toString(),
  }))

  const defaultIndex = options.findIndex(option => option.isDefault)
  taxDefaultKey.value = defaultIndex >= 0 ? `tax-${defaultIndex}` : 'none'
}

async function fetchTaxOptions() {
  taxOptionsLoading.value = true
  try {
    const response = await apiClient.get<CalculatorTaxOptionsDto>('/api/calculator-tax-options')
    applyTaxOptionsResponse(response)
    taxOptionsLoaded.value = true
  } catch {
    snackbar.enqueueSnackbar('Failed to load tax options', { variant: 'error' })
  } finally {
    taxOptionsLoading.value = false
  }
}

function addTaxOption() {
  taxOptions.value.push({ name: '', percentage: '' })
}

function removeTaxOption(index: number) {
  taxOptions.value.splice(index, 1)

  if (taxDefaultKey.value === `tax-${index}`) {
    taxDefaultKey.value = 'none'
    return
  }

  if (!taxDefaultKey.value.startsWith('tax-')) return
  const selectedIndex = Number.parseInt(taxDefaultKey.value.slice(4), 10)
  if (Number.isInteger(selectedIndex) && selectedIndex > index) {
    taxDefaultKey.value = `tax-${selectedIndex - 1}`
  }
}

async function handleSaveTaxOptions() {
  const normalizedOptions = taxOptions.value.map((option, index) => {
    const name = option.name.trim()
    const percentage = Number.parseFloat(option.percentage)
    return {
      name,
      percentage,
      isDefault: taxDefaultKey.value === `tax-${index}`,
    }
  })

  const firstMissingName = normalizedOptions.find(option => option.name.length === 0)
  if (firstMissingName) {
    snackbar.enqueueSnackbar('Every tax option must include a name', { variant: 'error' })
    return
  }

  const invalidPercentage = normalizedOptions.find(option => !Number.isFinite(option.percentage) || option.percentage <= 0 || option.percentage > 100)
  if (invalidPercentage) {
    snackbar.enqueueSnackbar('Tax percentages must be greater than 0 and no more than 100', { variant: 'error' })
    return
  }

  const names = normalizedOptions.map(option => option.name.toLocaleLowerCase())
  if (new Set(names).size !== names.length) {
    snackbar.enqueueSnackbar('Tax option names must be unique', { variant: 'error' })
    return
  }

  taxOptionsSaving.value = true
  try {
    const response = await apiClient.put<CalculatorTaxOptionsDto>('/api/calculator-tax-options', {
      options: normalizedOptions,
    })
    applyTaxOptionsResponse(response)
    snackbar.enqueueSnackbar('Tax options updated', { variant: 'success' })
  } catch (error) {
    snackbar.enqueueSnackbar(error instanceof Error ? error.message : 'Failed to save tax options', { variant: 'error' })
  } finally {
    taxOptionsSaving.value = false
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
  ruleForm.value = { name: '', ruleRegex: '', notes: '', minimumAmount: '', maximumAmount: '', expenseCategoryId: null }
}

function openAddRule() {
  resetRuleForm()
  addRuleOpen.value = true
}

function openEditRule(rule: RuleDto) {
  editRule.value = rule
  ruleForm.value = {
    name: rule.name ?? '',
    ruleRegex: rule.ruleRegex ?? '',
    notes: rule.notes ?? '',
    minimumAmount: rule.minimumAmount !== null ? centsToDollars(rule.minimumAmount) : '',
    maximumAmount: rule.maximumAmount !== null ? centsToDollars(rule.maximumAmount) : '',
    expenseCategoryId: rule.expenseCategoryId ?? null,
  }
}

async function handleAddRule() {
  try {
    await apiClient.post('/api/rules', {
      name: ruleForm.value.name,
      ruleRegex: ruleForm.value.ruleRegex,
      notes: ruleForm.value.notes,
      minimumAmount: optionalDollarsToCents(ruleForm.value.minimumAmount),
      maximumAmount: optionalDollarsToCents(ruleForm.value.maximumAmount),
      expenseCategoryId: ruleForm.value.expenseCategoryId,
    })
    snackbar.enqueueSnackbar('Rule added', { variant: 'success' })
    addRuleOpen.value = false
    resetRuleForm()
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
      notes: ruleForm.value.notes,
      minimumAmount: optionalDollarsToCents(ruleForm.value.minimumAmount),
      maximumAmount: optionalDollarsToCents(ruleForm.value.maximumAmount),
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

async function fetchExternalLinks() {
  externalLinksLoading.value = true
  try {
    externalLinks.value = await apiClient.get<ExternalLinkRuleDto[]>('/api/external-links') ?? []
  } catch {
    snackbar.enqueueSnackbar('Failed to load external links', { variant: 'error' })
  } finally {
    externalLinksLoading.value = false
  }
}

function resetExternalLinkForm() {
  externalLinkForm.value = { name: '', ruleRegex: '', url: '' }
}

function openAddExternalLink() {
  resetExternalLinkForm()
  addExternalLinkOpen.value = true
}

function openEditExternalLink(link: ExternalLinkRuleDto) {
  editExternalLink.value = link
  externalLinkForm.value = {
    name: link.name ?? '',
    ruleRegex: link.ruleRegex ?? '',
    url: link.url ?? '',
  }
}

async function handleAddExternalLink() {
  try {
    await apiClient.post('/api/external-links', { ...externalLinkForm.value })
    snackbar.enqueueSnackbar('External link added', { variant: 'success' })
    addExternalLinkOpen.value = false
    resetExternalLinkForm()
    resetExternalLinkRulesCache()
    void fetchExternalLinks()
  } catch (err) {
    snackbar.enqueueSnackbar(err instanceof Error ? err.message : 'Failed to add external link', { variant: 'error' })
  }
}

async function handleEditExternalLink() {
  if (!editExternalLink.value) return
  try {
    await apiClient.put(`/api/external-links/${editExternalLink.value.id}`, { ...externalLinkForm.value })
    snackbar.enqueueSnackbar('External link updated', { variant: 'success' })
    editExternalLink.value = null
    resetExternalLinkForm()
    resetExternalLinkRulesCache()
    void fetchExternalLinks()
  } catch (err) {
    snackbar.enqueueSnackbar(err instanceof Error ? err.message : 'Failed to update external link', { variant: 'error' })
  }
}

async function handleDeleteExternalLink() {
  if (!deleteExternalLink.value) return
  try {
    await apiClient.delete(`/api/external-links/${deleteExternalLink.value.id}`)
    snackbar.enqueueSnackbar('External link deleted', { variant: 'success' })
    deleteExternalLink.value = null
    resetExternalLinkRulesCache()
    void fetchExternalLinks()
  } catch {
    snackbar.enqueueSnackbar('Failed to delete external link', { variant: 'error' })
  }
}

async function fetchManageCategories() {
  categoriesLoading.value = true
  try {
    const fetchedCategories = await apiClient.get<ExpenseCategoryDto[]>(
      `/api/expense-categories?includeHidden=${showHiddenCategories.value}`,
    ) ?? []
    manageCategories.value = fetchedCategories.sort((a, b) =>
      (a.name ?? '').localeCompare(b.name ?? '', undefined, { sensitivity: 'base' }) || (a.id - b.id),
    )
    categoriesLoaded.value = true
  } catch {
    snackbar.enqueueSnackbar('Failed to load expense categories', { variant: 'error' })
  } finally {
    categoriesLoading.value = false
  }
}

function startEditCategory(category: ExpenseCategoryDto) {
  editCategoryId.value = category.id
  editCategoryForm.value = {
    name: category.name ?? '',
    description: category.description ?? '',
    categoryName: category.categoryName ?? '',
    budgetType: category.usePercentage ? 'percentage' : 'fixed',
    budgetedAmount: centsToDollars(category.budgetedAmount),
    budgetedPercentage: category.budgetedPercentage.toString(),
    cap: category.cap === null ? '' : centsToDollars(category.cap),
  }
}

async function handleSaveCategoryEdit(category: ExpenseCategoryDto) {
  const budgetType = editCategoryForm.value.budgetType
  const budgetedAmount = dollarsToCents(editCategoryForm.value.budgetedAmount)
  const budgetedPercentage = Number.parseInt(editCategoryForm.value.budgetedPercentage, 10)
  const parsedCap = parseOptionalNonNegativeDollars(editCategoryForm.value.cap)

  if (budgetType === 'fixed' && (Number.isNaN(budgetedAmount) || budgetedAmount < 0)) {
    snackbar.enqueueSnackbar('Budgeted amount must be 0 or greater', { variant: 'error' })
    return
  }

  if (budgetType === 'percentage' && (!Number.isInteger(budgetedPercentage) || budgetedPercentage <= 0 || budgetedPercentage > 100)) {
    snackbar.enqueueSnackbar('Budgeted percentage must be a whole number between 1 and 100', { variant: 'error' })
    return
  }

  if (!parsedCap.valid) {
    snackbar.enqueueSnackbar('Cap must be a valid amount that is 0 or greater', { variant: 'error' })
    return
  }

  try {
    await apiClient.put(`/api/expense-categories/${category.id}`, {
      name: editCategoryForm.value.name,
      description: editCategoryForm.value.description,
      categoryName: editCategoryForm.value.categoryName,
      budgetedAmount: budgetType === 'fixed' ? budgetedAmount : 0,
      budgetedPercentage: budgetType === 'percentage' ? budgetedPercentage : 0,
      cap: parsedCap.value,
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

function resetNewCategoryForm() {
  newCategoryForm.value = {
    name: '',
    description: '',
    categoryName: '',
    budgetType: 'fixed',
    budgetedAmount: '0.00',
    budgetedPercentage: '0',
    cap: '',
    accountId: null,
  }
}

function openAddCategory() {
  resetNewCategoryForm()
  addCategoryOpen.value = true
  if (!accountsLoaded.value) {
    void fetchAccounts()
  }
}

async function handleAddCategory() {
  const name = newCategoryForm.value.name.trim()
  if (!name) {
    snackbar.enqueueSnackbar('Category name is required', { variant: 'error' })
    return
  }

  const budgetType = newCategoryForm.value.budgetType
  const budgetedAmount = dollarsToCents(newCategoryForm.value.budgetedAmount)
  const budgetedPercentage = Number.parseInt(newCategoryForm.value.budgetedPercentage, 10)
  const parsedCap = parseOptionalNonNegativeDollars(newCategoryForm.value.cap)

  if (budgetType === 'fixed' && (Number.isNaN(budgetedAmount) || budgetedAmount < 0)) {
    snackbar.enqueueSnackbar('Budgeted amount must be 0 or greater', { variant: 'error' })
    return
  }

  if (budgetType === 'percentage' && (!Number.isInteger(budgetedPercentage) || budgetedPercentage <= 0 || budgetedPercentage > 100)) {
    snackbar.enqueueSnackbar('Budgeted percentage must be a whole number between 1 and 100', { variant: 'error' })
    return
  }

  if (!parsedCap.valid) {
    snackbar.enqueueSnackbar('Cap must be a valid amount that is 0 or greater', { variant: 'error' })
    return
  }

  addCategorySaving.value = true
  try {
    await apiClient.post('/api/expense-categories', {
      name,
      description: newCategoryForm.value.description,
      categoryName: newCategoryForm.value.categoryName,
      budgetedAmount: budgetType === 'fixed' ? budgetedAmount : 0,
      budgetedPercentage: budgetType === 'percentage' ? budgetedPercentage : 0,
      cap: parsedCap.value,
      accountId: newCategoryForm.value.accountId,
    })
    snackbar.enqueueSnackbar('Category added', { variant: 'success' })
    addCategoryOpen.value = false
    resetNewCategoryForm()
    void fetchManageCategories()
  } catch (err) {
    snackbar.enqueueSnackbar(err instanceof Error ? err.message : 'Failed to add category', { variant: 'error' })
  } finally {
    addCategorySaving.value = false
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
  }
})

watch(addExternalLinkOpen, (isOpen) => {
  if (!isOpen) {
    resetExternalLinkForm()
  }
})

watch(addCategoryOpen, (isOpen) => {
  if (!isOpen) {
    resetNewCategoryForm()
  }
})

watch(openPanel, (panel) => {
  if (panel === PANEL_PROFILE && !profileLoaded.value) {
    void fetchCurrentUserProfile()
    return
  }

  if (panel === PANEL_TAX_OPTIONS && !taxOptionsLoaded.value) {
    void fetchTaxOptions()
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

  if (panel === PANEL_EXTERNAL_LINKS && !externalLinksLoaded.value) {
    externalLinksLoaded.value = true
    void fetchExternalLinks()
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
          <div>
            <div class="text-subtitle-1 font-weight-medium">Calculator Tax Options</div>
            <div class="text-body-2 text-medium-emphasis">Manage named tax percentages used in amount calculators.</div>
          </div>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <div v-if="taxOptionsLoading" class="d-flex justify-center pa-8">
            <v-progress-circular indeterminate aria-label="Loading calculator tax options" />
          </div>
          <div v-else class="d-flex flex-column" style="gap: 12px;">
            <v-radio-group
              v-model="taxDefaultKey"
              label="Default selected tax"
              density="compact"
              hide-details
            >
              <v-radio label="None" value="none" />
              <v-radio
                v-for="(option, index) in taxOptions"
                :key="`default-tax-${index}`"
                :label="option.name.trim() || `Tax option ${index + 1}`"
                :value="`tax-${index}`"
              />
            </v-radio-group>

            <v-alert v-if="taxOptions.length === 0" type="info" variant="tonal">
              No tax options defined. Add one to make it available in the calculator.
            </v-alert>

            <div
              v-for="(option, index) in taxOptions"
              :key="`tax-option-${index}`"
              class="d-flex align-center flex-wrap"
              style="gap: 8px;"
            >
              <v-text-field
                v-model="option.name"
                label="Name"
                density="compact"
                hide-details
                style="max-width: 240px;"
              />
              <v-text-field
                v-model="option.percentage"
                label="Percentage"
                type="number"
                step="0.001"
                min="0"
                density="compact"
                hide-details
                suffix="%"
                style="max-width: 180px;"
              />
              <v-btn
                icon="mdi-delete"
                variant="text"
                color="error"
                aria-label="Remove tax option"
                @click="removeTaxOption(index)"
              />
            </div>

            <div class="d-flex flex-wrap" style="gap: 8px;">
              <v-btn variant="outlined" @click="addTaxOption">Add Tax Option</v-btn>
              <v-btn color="primary" :loading="taxOptionsSaving" @click="handleSaveTaxOptions">Save Tax Options</v-btn>
            </div>
          </div>
        </v-expansion-panel-text>
      </v-expansion-panel>

      <v-expansion-panel>
        <v-expansion-panel-title>
          <div>
            <div class="text-subtitle-1 font-weight-medium">Accounts</div>
            <div class="text-body-2 text-medium-emphasis">Add accounts, rename them, and choose the default.</div>
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
          <div>
            <div class="text-subtitle-1 font-weight-medium">Import Rules</div>
            <div class="text-body-2 text-medium-emphasis">Control automatic category suggestions while importing.</div>
          </div>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <div v-if="rulesLoading" class="d-flex justify-center pa-8">
            <v-progress-circular indeterminate aria-label="Loading import rules" />
          </div>
          <div v-else>
            <div class="d-flex justify-end mb-4">
              <v-btn color="primary" @click="openAddRule">Add Rule</v-btn>
            </div>
            <v-card v-for="group in ruleGroups" :key="group.key" class="mb-4">
              <v-card-title>{{ group.label }}</v-card-title>
              <v-divider />
              <v-list>
                <v-list-item v-if="group.rules.length === 0">
                  <v-list-item-title>No rules in this category.</v-list-item-title>
                </v-list-item>
                <v-list-item v-for="rule in group.rules" :key="rule.id">
                  <v-list-item-title>{{ rule.name }}</v-list-item-title>
                  <v-list-item-subtitle>Pattern: {{ rule.ruleRegex ?? '—' }}</v-list-item-subtitle>
                  <v-list-item-subtitle v-if="rule.notes">Notes: {{ rule.notes }}</v-list-item-subtitle>
                  <v-list-item-subtitle v-if="ruleAmountRangeText(rule)">Amount: {{ ruleAmountRangeText(rule) }}</v-list-item-subtitle>
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
          <div>
            <div class="text-subtitle-1 font-weight-medium">External Links</div>
            <div class="text-body-2 text-medium-emphasis">Show a link on transactions whose description matches a pattern.</div>
          </div>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <div v-if="externalLinksLoading" class="d-flex justify-center pa-8">
            <v-progress-circular indeterminate aria-label="Loading external links" />
          </div>
          <div v-else>
            <div class="d-flex justify-end mb-4">
              <v-btn color="primary" @click="openAddExternalLink">Add External Link</v-btn>
            </div>
            <v-card v-if="externalLinks.length > 0">
              <v-list>
                <v-list-item v-for="link in externalLinks" :key="link.id">
                  <v-list-item-title>{{ link.name }}</v-list-item-title>
                  <v-list-item-subtitle>Pattern: {{ link.ruleRegex ?? '—' }}</v-list-item-subtitle>
                  <v-list-item-subtitle>Link: {{ link.url ?? '—' }}</v-list-item-subtitle>
                  <template #append>
                    <div class="d-flex" style="gap: 4px;">
                      <v-btn icon="mdi-pencil" variant="text" aria-label="Edit external link" @click="openEditExternalLink(link)" />
                      <v-btn icon="mdi-delete" variant="text" color="error" aria-label="Delete external link" @click="deleteExternalLink = link" />
                    </div>
                  </template>
                </v-list-item>
              </v-list>
            </v-card>
            <v-alert v-else type="info" variant="tonal">No external links defined yet.</v-alert>
          </div>
        </v-expansion-panel-text>
      </v-expansion-panel>

      <v-expansion-panel>
        <v-expansion-panel-title>
          <div>
            <div class="text-subtitle-1 font-weight-medium">Expense Categories</div>
            <div class="text-body-2 text-medium-emphasis">Rename, hide, restore, or delete categories.</div>
          </div>
        </v-expansion-panel-title>
        <v-expansion-panel-text>
          <div class="d-flex justify-space-between align-center mb-4">
            <h5 class="text-h5">Expense Categories</h5>
            <div class="d-flex align-center" style="gap: 16px;">
              <v-switch
                v-model="showHiddenCategories"
                label="Show hidden"
                density="compact"
                hide-details
                color="primary"
              />
              <v-btn color="primary" prepend-icon="mdi-plus" @click="openAddCategory">Add Category</v-btn>
            </div>
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
                  <div v-if="editCategoryId === category.id" class="d-flex flex-wrap align-center" style="gap: 8px;">
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
                    <v-text-field
                      v-model="editCategoryForm.description"
                      label="Description"
                      density="compact"
                      hide-details
                      style="min-width: 260px;"
                      @keydown.enter="handleSaveCategoryEdit(category)"
                    />
                    <v-btn-toggle
                      v-model="editCategoryForm.budgetType"
                      mandatory
                      density="compact"
                      divided
                    >
                      <v-btn value="fixed" size="small">Fixed</v-btn>
                      <v-btn value="percentage" size="small">Percentage</v-btn>
                    </v-btn-toggle>
                    <v-text-field
                      v-if="editCategoryForm.budgetType === 'fixed'"
                      v-model="editCategoryForm.budgetedAmount"
                      label="Budget"
                      type="number"
                      step="0.01"
                      min="0"
                      density="compact"
                      hide-details
                      prefix="$"
                      style="max-width: 160px;"
                      @keydown.enter="handleSaveCategoryEdit(category)"
                    />
                    <v-text-field
                      v-else
                      v-model="editCategoryForm.budgetedPercentage"
                      label="Budget"
                      type="number"
                      step="1"
                      min="1"
                      max="100"
                      density="compact"
                      hide-details
                      suffix="%"
                      style="max-width: 160px;"
                      @keydown.enter="handleSaveCategoryEdit(category)"
                    />
                    <v-text-field
                      v-model="editCategoryForm.cap"
                      label="Cap"
                      type="number"
                      step="0.01"
                      min="0"
                      density="compact"
                      hide-details
                      prefix="$"
                      :disabled="editCategoryForm.budgetType !== 'fixed'"
                      style="max-width: 160px;"
                      @keydown.enter="handleSaveCategoryEdit(category)"
                    />
                  </div>
                  <div v-else class="d-flex align-center" style="gap: 8px;">
                    <span>{{ category.name }}</span>
                    <v-chip v-if="category.isHidden" size="small">Hidden</v-chip>
                  </div>
                </v-list-item-title>
                <v-list-item-subtitle v-if="editCategoryId !== category.id">
                  <div>{{ category.categoryName ?? 'No group' }}</div>
                  <div v-if="category.description">{{ category.description }}</div>
                  <div v-if="category.cap !== null">Cap: {{ formatCents(category.cap) }}</div>
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
          <v-textarea
            label="Notes"
            rows="2"
            auto-grow
            hint="Added to matching pending expenses. A rule can add notes without setting a category."
            persistent-hint
            v-model="ruleForm.notes"
          />
          <div class="d-flex" style="gap: 16px;">
            <v-text-field
              label="Min Amount"
              type="number"
              prefix="$"
              hint="Optional. Only match amounts at or above this."
              persistent-hint
              v-model="ruleForm.minimumAmount"
            />
            <v-text-field
              label="Max Amount"
              type="number"
              prefix="$"
              hint="Optional. Only match amounts at or below this."
              persistent-hint
              v-model="ruleForm.maximumAmount"
            />
          </div>
          <CategorySelector
            label="Target Category"
            :categories="categories"
            null-option-label="None"
            :clearable="false"
            v-model="ruleForm.expenseCategoryId"
          />
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
          <v-textarea
            label="Notes"
            rows="2"
            auto-grow
            hint="Added to matching pending expenses. A rule can add notes without setting a category."
            persistent-hint
            v-model="ruleForm.notes"
          />
          <div class="d-flex" style="gap: 16px;">
            <v-text-field
              label="Min Amount"
              type="number"
              prefix="$"
              hint="Optional. Only match amounts at or above this."
              persistent-hint
              v-model="ruleForm.minimumAmount"
            />
            <v-text-field
              label="Max Amount"
              type="number"
              prefix="$"
              hint="Optional. Only match amounts at or below this."
              persistent-hint
              v-model="ruleForm.maximumAmount"
            />
          </div>
          <CategorySelector
            label="Category"
            :categories="categories"
            null-option-label="None"
            :clearable="false"
            v-model="ruleForm.expenseCategoryId"
          />
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

    <v-dialog v-model="addExternalLinkOpen" max-width="500">
      <v-card>
        <v-card-title>Add External Link</v-card-title>
        <v-card-text class="d-flex flex-column" style="gap: 16px;">
          <v-text-field label="Name" v-model="externalLinkForm.name" />
          <v-text-field label="Regex Pattern" v-model="externalLinkForm.ruleRegex" hint="Matched against the description, case insensitive" persistent-hint />
          <v-text-field label="URL" v-model="externalLinkForm.url" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="addExternalLinkOpen = false">Cancel</v-btn>
          <v-btn color="primary" @click="handleAddExternalLink">Add</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog :model-value="!!editExternalLink" max-width="500" @update:model-value="(val: boolean) => !val && (editExternalLink = null)">
      <v-card>
        <v-card-title>Edit External Link</v-card-title>
        <v-card-text class="d-flex flex-column" style="gap: 16px;">
          <v-text-field label="Name" v-model="externalLinkForm.name" />
          <v-text-field label="Regex Pattern" v-model="externalLinkForm.ruleRegex" hint="Matched against the description, case insensitive" persistent-hint />
          <v-text-field label="URL" v-model="externalLinkForm.url" />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="editExternalLink = null">Cancel</v-btn>
          <v-btn color="primary" @click="handleEditExternalLink">Save</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog :model-value="!!deleteExternalLink" max-width="480" @update:model-value="(val: boolean) => !val && (deleteExternalLink = null)">
      <v-card>
        <v-card-title>Confirm Delete</v-card-title>
        <v-card-text>Delete external link "{{ deleteExternalLink?.name }}"?</v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn @click="deleteExternalLink = null">Cancel</v-btn>
          <v-btn color="error" @click="handleDeleteExternalLink">Delete</v-btn>
        </v-card-actions>
      </v-card>
    </v-dialog>

    <v-dialog v-model="addCategoryOpen" max-width="520">
      <v-card>
        <v-card-title>Add Expense Category</v-card-title>
        <v-card-text class="d-flex flex-column" style="gap: 16px;">
          <v-text-field
            v-model="newCategoryForm.name"
            label="Name"
            autofocus
            @keydown.enter="handleAddCategory"
          />
          <v-text-field
            v-model="newCategoryForm.categoryName"
            label="Group"
            hint="Optional. Categories with the same group are shown together."
            persistent-hint
            @keydown.enter="handleAddCategory"
          />
          <v-text-field
            v-model="newCategoryForm.description"
            label="Description"
            @keydown.enter="handleAddCategory"
          />
          <v-select
            v-model="newCategoryForm.accountId"
            :items="accounts"
            item-title="name"
            item-value="id"
            label="Account"
            clearable
            hint="Leave blank to use the default account."
            persistent-hint
          />
          <v-btn-toggle v-model="newCategoryForm.budgetType" mandatory density="compact" divided>
            <v-btn value="fixed" size="small">Fixed</v-btn>
            <v-btn value="percentage" size="small">Percentage</v-btn>
          </v-btn-toggle>
          <v-text-field
            v-if="newCategoryForm.budgetType === 'fixed'"
            v-model="newCategoryForm.budgetedAmount"
            label="Budget"
            type="number"
            step="0.01"
            min="0"
            prefix="$"
            @keydown.enter="handleAddCategory"
          />
          <v-text-field
            v-else
            v-model="newCategoryForm.budgetedPercentage"
            label="Budget"
            type="number"
            step="1"
            min="1"
            max="100"
            suffix="%"
            @keydown.enter="handleAddCategory"
          />
          <v-text-field
            v-model="newCategoryForm.cap"
            label="Cap"
            type="number"
            step="0.01"
            min="0"
            prefix="$"
            hint="Optional maximum running balance for fixed-budget categories."
            persistent-hint
            :disabled="newCategoryForm.budgetType !== 'fixed'"
            @keydown.enter="handleAddCategory"
          />
        </v-card-text>
        <v-card-actions>
          <v-spacer />
          <v-btn :disabled="addCategorySaving" @click="addCategoryOpen = false">Cancel</v-btn>
          <v-btn color="primary" :loading="addCategorySaving" @click="handleAddCategory">Add</v-btn>
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
