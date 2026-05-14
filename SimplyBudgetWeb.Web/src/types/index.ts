// All amounts are integer cents (divide by 100 for display)

export interface BudgetCategoryDto {
  id: number
  name: string | null
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
  month: string
  categories: BudgetCategoryDto[]
}

export interface ExpenseCategoryDto {
  id: number
  name: string | null
  categoryName: string | null
  accountId: number | null
  budgetedAmount: number
  budgetedPercentage: number
  currentBalance: number
  cap: number | null
  isHidden: boolean
  usePercentage: boolean
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
  isDone: boolean
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
