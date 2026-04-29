import type {
  Account, ExpenseCategory, BudgetOverview,
  Transaction, CategoryRule, ImportRow
} from '../types/api'

const base = '/api'

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    headers: { 'Content-Type': 'application/json', ...options?.headers },
    ...options
  })
  if (!res.ok) {
    const text = await res.text().catch(() => res.statusText)
    throw new Error(`${res.status}: ${text}`)
  }
  if (res.status === 204) return undefined as unknown as T
  return res.json() as Promise<T>
}

// Accounts
export const getAccounts = () => request<Account[]>(`${base}/accounts`)
export const createAccount = (data: { name: string; validatedDate?: string }) =>
  request<Account>(`${base}/accounts`, { method: 'POST', body: JSON.stringify(data) })
export const updateAccount = (id: number, data: { name: string; validatedDate?: string }) =>
  request<Account>(`${base}/accounts/${id}`, { method: 'PUT', body: JSON.stringify(data) })
export const deleteAccount = (id: number) =>
  request<void>(`${base}/accounts/${id}`, { method: 'DELETE' })

// Expense Categories
export const getExpenseCategories = (showHidden = false) =>
  request<ExpenseCategory[]>(`${base}/expense-categories?showHidden=${showHidden}`)
export const createExpenseCategory = (data: Partial<ExpenseCategory>) =>
  request<ExpenseCategory>(`${base}/expense-categories`, { method: 'POST', body: JSON.stringify(data) })
export const updateExpenseCategory = (id: number, data: Partial<ExpenseCategory>) =>
  request<ExpenseCategory>(`${base}/expense-categories/${id}`, { method: 'PUT', body: JSON.stringify(data) })
export const hideExpenseCategory = (id: number) =>
  request<void>(`${base}/expense-categories/${id}/hide`, { method: 'PATCH' })
export const showExpenseCategory = (id: number) =>
  request<void>(`${base}/expense-categories/${id}/show`, { method: 'PATCH' })
export const deleteExpenseCategory = (id: number) =>
  request<void>(`${base}/expense-categories/${id}`, { method: 'DELETE' })

// Budget
export const getBudget = (year?: number, month?: number) => {
  const params = new URLSearchParams()
  if (year) params.set('year', String(year))
  if (month) params.set('month', String(month))
  return request<BudgetOverview>(`${base}/budget?${params}`)
}

// Transactions
export const getTransactions = (params: {
  year?: number; month?: number; search?: string;
  categoryIds?: number[]; accountId?: number
}) => {
  const p = new URLSearchParams()
  if (params.year) p.set('year', String(params.year))
  if (params.month) p.set('month', String(params.month))
  if (params.search) p.set('search', params.search)
  if (params.categoryIds?.length) params.categoryIds.forEach(id => p.append('categoryIds', String(id)))
  if (params.accountId) p.set('accountId', String(params.accountId))
  return request<Transaction[]>(`${base}/transactions?${p}`)
}

export const createTransaction = (data: {
  date: string; description: string; ignoreBudget: boolean;
  items: { categoryId: number; amount: number }[]
}) => request<number>(`${base}/transactions`, { method: 'POST', body: JSON.stringify(data) })

export const createIncome = (data: {
  date: string; description: string; ignoreBudget: boolean;
  items: { categoryId: number; amount: number }[]
}) => request<number>(`${base}/transactions/income`, { method: 'POST', body: JSON.stringify(data) })

export const createTransfer = (data: {
  date: string; description?: string; ignoreBudget: boolean;
  fromCategoryId: number; toCategoryId: number; amount: number
}) => request<number>(`${base}/transactions/transfer`, { method: 'POST', body: JSON.stringify(data) })

export const deleteTransaction = (id: number) =>
  request<void>(`${base}/transactions/${id}`, { method: 'DELETE' })

// Category Rules
export const getCategoryRules = () => request<CategoryRule[]>(`${base}/category-rules`)
export const createCategoryRule = (data: Partial<CategoryRule>) =>
  request<CategoryRule>(`${base}/category-rules`, { method: 'POST', body: JSON.stringify(data) })
export const updateCategoryRule = (id: number, data: Partial<CategoryRule>) =>
  request<CategoryRule>(`${base}/category-rules/${id}`, { method: 'PUT', body: JSON.stringify(data) })
export const deleteCategoryRule = (id: number) =>
  request<void>(`${base}/category-rules/${id}`, { method: 'DELETE' })
export const matchCategoryRule = (description: string) =>
  request<{ categoryId: number | null }>(`${base}/category-rules/match?description=${encodeURIComponent(description)}`)

// Import
export const parseImportFile = async (file: File): Promise<ImportRow[]> => {
  const form = new FormData()
  form.append('file', file)
  const res = await fetch(`${base}/import`, { method: 'POST', body: form })
  if (!res.ok) throw new Error(`${res.status}: ${res.statusText}`)
  return res.json()
}
export const confirmImport = (transactions: ImportRow[]) =>
  request<{ imported: number; skipped: number }>(`${base}/import/confirm`, {
    method: 'POST', body: JSON.stringify({ transactions })
  })
