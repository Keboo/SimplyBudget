// All amounts are integer cents (divide by 100 for display)

export interface BudgetCategoryDto {
  id: number
  name: string | null
  description: string | null
  categoryName: string | null
  accountId: number | null
  budgetedAmount: number
  budgetedPercentage: number
  currentBalance: number
  cap: number | null
  isHidden: boolean
  usePercentage: boolean
  monthlyExpenses: number
  monthlyAllocations: number
  threeMonthAverage: number
  sixMonthAverage: number
  twelveMonthAverage: number
}

export interface BudgetResponse {
  totalBudget: number
  totalAccountAmount: number
  month: string
  categories: BudgetCategoryDto[]
}

export interface ExpenseCategoryDto {
  id: number
  name: string | null
  description: string | null
  categoryName: string | null
  accountId: number | null
  budgetedAmount: number
  budgetedPercentage: number
  currentBalance: number
  cap: number | null
  isHidden: boolean
  usePercentage: boolean
  hasItems: boolean
}

export interface ExpenseCategoryMonthlyExpensePointDto {
  month: string
  amount: number
}

export interface ExpenseCategoryMonthlyExpensesDto {
  expenseCategoryId: number
  name: string | null
  budgetedAmount: number
  budgetedPercentage: number
  usePercentage: boolean
  months: ExpenseCategoryMonthlyExpensePointDto[]
}

export interface AccountDto {
  id: number
  name: string | null
  validatedDate: string
  isDefault: boolean
  currentAmount: number
}

export interface HistoryItemDto {
  id: number
  date: string
  description: string | null
  notes: string | null
  isTransfer: boolean
  details: HistoryDetailDto[]
}

export interface HistoryDetailDto {
  id: number
  expenseCategoryId: number
  categoryName: string | null
  amount: number
  ignoreBudget: boolean
}

export interface RuleDto {
  id: number
  name: string | null
  ruleRegex: string | null
  expenseCategoryId: number | null
  categoryName: string | null
}

export interface ImportItemDto {
  date: string
  description: string | null
  amount: number
  isDebit: boolean
  suggestedCategoryId: number | null
  suggestedCategoryName: string | null
  isChecked: boolean
  isDuplicate: boolean
}

export interface AssigneeDto {
  id: number
  name: string | null
}

export interface CurrentUserDto {
  displayName: string
  email: string | null
}

export interface PendingExpenseDto {
  id: number
  version: string
  date: string
  description: string | null
  amount: number
  isDebit: boolean
  notes: string | null
  assigneeId: number | null
  assigneeName: string | null
  suggestedCategoryId: number | null
  suggestedCategoryName: string | null
}

export interface OldestPendingExpenseMonthDto {
  month: string | null
}

export interface PendingExpenseUpdateRequest {
  assigneeId: number | null
  notes: string | null
  version: string
}

export interface ConvertPendingExpenseItemRequest {
  expenseCategoryId: number
  amount: number
}

export interface ConvertPendingExpenseRequest {
  description: string
  date: string
  items: ConvertPendingExpenseItemRequest[]
  version: string
  ignoreBudget: boolean
  notes: string | null
}

export interface TransactionItemRequest {
  expenseCategoryId: number
  amount: number
}

export interface TransactionRequest {
  description: string
  date: string
  items: TransactionItemRequest[]
}

export interface TransferRequest {
  description: string
  date: string
  amount: number
  fromCategoryId: number
  toCategoryId: number
}

export interface BudgetDataExportPackageDto {
  formatVersion: number
  exportedAtUtc: string
  source: string | null
  accounts: BudgetDataExportAccountDto[]
  categories: BudgetDataExportCategoryDto[]
  items: BudgetDataExportItemDto[]
  itemDetails: BudgetDataExportItemDetailDto[]
  rules: BudgetDataExportRuleDto[]
  metadata: BudgetDataExportMetadataDto[]
}

export interface BudgetDataExportAccountDto {
  id: number
  name: string | null
  validatedDate: string
  isDefault: boolean
}

export interface BudgetDataExportCategoryDto {
  id: number
  name: string | null
  description: string | null
  categoryName: string | null
  accountId: number | null
  budgetedAmount: number
  budgetedPercentage: number
  currentBalance: number
  cap: number | null
  isHidden: boolean
}

export interface BudgetDataExportItemDto {
  id: number
  date: string
  description: string | null
  notes: string | null
}

export interface BudgetDataExportItemDetailDto {
  id: number
  expenseCategoryItemId: number
  expenseCategoryId: number
  amount: number
  ignoreBudget: boolean
}

export interface BudgetDataExportRuleDto {
  id: number
  name: string | null
  ruleRegex: string | null
  expenseCategoryId: number | null
}

export interface BudgetDataExportMetadataDto {
  id: number
  key: string | null
  value: string | null
}
