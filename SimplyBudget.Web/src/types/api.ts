// API types matching the backend DTOs

export interface Account {
  id: number
  name: string
  isDefault: boolean
  currentBalance: number
  validatedDate: string
}

export interface ExpenseCategory {
  id: number
  name: string
  categoryName: string | null
  budgetedAmount: number
  budgetedPercentage: number
  currentBalance: number
  cap: number | null
  isHidden: boolean
  accountId: number | null
}

export interface BudgetCategory {
  id: number
  name: string
  categoryName: string | null
  budgetedAmount: number
  budgetedPercentage: number
  effectiveBudgetAmount: number
  spentThisMonth: number
  remainingThisMonth: number
  currentBalance: number
  cap: number | null
}

export interface BudgetOverview {
  year: number
  month: number
  totalBudget: number
  categories: BudgetCategory[]
}

export interface TransactionDetail {
  id: number
  categoryId: number
  categoryName: string | null
  amount: number
  ignoreBudget: boolean
}

export interface Transaction {
  id: number
  date: string
  description: string | null
  details: TransactionDetail[]
}

export interface CategoryRule {
  id: number
  name: string | null
  ruleRegex: string | null
  expenseCategoryId: number | null
}

export interface ImportRow {
  date: string
  description: string
  amount: number
  categoryId: number | null
}
