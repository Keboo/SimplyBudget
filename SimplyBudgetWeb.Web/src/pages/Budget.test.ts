import { ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Budget from './Budget.vue'
import MonthPickerNav from '@/components/MonthPickerNav.vue'
import AddTransactionDialog from '@/components/AddTransactionDialog.vue'
import EditExpenseCategoryDialog from '@/components/EditExpenseCategoryDialog.vue'
import CategorySelector from '@/components/CategorySelector.vue'
import type { BudgetCategoryDto, ExpenseCategoryDto } from '@/types'

const mocks = vi.hoisted(() => ({ get: vi.fn(), put: vi.fn(), post: vi.fn(), push: vi.fn() }))
vi.mock('@/services/apiClient', () => ({ apiClient: mocks }))
vi.mock('@/stores/auth', () => ({ useAuthStore: () => ({ getToken: vi.fn() }) }))
vi.mock('vue-router', () => ({ useRouter: () => ({ push: mocks.push }) }))
vi.mock('@/composables/useMonthQueryParam', () => ({
  useMonthQueryParam: () => ({ currentMonth: ref(new Date(2026, 9, 1)) }),
}))
vi.mock('@/services/monthUpdatesHub', () => ({
  createMonthUpdatesHubClient: () => ({ start: vi.fn(), stop: vi.fn(), setMonth: vi.fn() }),
}))

const category: BudgetCategoryDto = {
  id: 1, name: 'Groceries', description: 'Household food', categoryName: 'Essentials',
  accountId: null, cap: null, isHidden: false, usePercentage: false,
  currentBalance: 12500, budgetedAmount: 30000, budgetedPercentage: 0,
  monthlyAllocations: 25000, monthlyExpenses: 17500,
  threeMonthAverage: 35000, sixMonthAverage: 28000, twelveMonthAverage: 26000,
}
const editableCategory: ExpenseCategoryDto = { ...category, hasItems: true, accountId: 3, cap: 50000, budgetedAmount: 45000 }

afterEach(() => {
  document.body.innerHTML = ''
  vi.unstubAllGlobals()
})
enableAutoUnmount(afterEach)
beforeEach(() => {
  vi.stubGlobal('visualViewport', { addEventListener: vi.fn(), removeEventListener: vi.fn(), width: 1440, height: 900, offsetTop: 0, offsetLeft: 0 })
  mocks.get.mockReset()
  mocks.put.mockReset().mockResolvedValue({})
  mocks.post.mockReset().mockResolvedValue({})
  mocks.push.mockReset()
})

async function setup(overrides: Partial<BudgetCategoryDto> = {}, stubTransactions = true) {
  mocks.get.mockImplementation(async (url: string) => url.startsWith('/api/budget?')
    ? { totalBudget: 30000, totalAccountAmount: 50000, estimatedMonthlyIncome: 40000, month: '2026-10', categories: [{ ...category, ...overrides }] }
    : url === '/api/expense-categories' ? [editableCategory, { ...editableCategory, id: 2, name: 'Savings' }] : [])
  const wrapper = mount(Budget, {
    attachTo: document.body,
    global: {
      plugins: [createPinia(), createVuetify({ components, directives })],
      stubs: { AddTransactionDialog: stubTransactions },
    },
  })
  await flushPromises()
  return wrapper
}

async function clickAction(text: string) {
  const action = [...document.querySelectorAll<HTMLElement>('button,.v-list-item')]
    .find(element => element.textContent?.trim() === text)!
  expect(action, text).toBeDefined()
  action.click()
  await flushPromises()
}

describe('budget expense rows', () => {
  it.each([
    { balance: 12500, label: '$125.00', color: 'success', allocated: 25000, allocationLabel: '$250.00', usePercentage: false },
    { balance: -12500, label: '-$125.00', color: 'error', allocated: 0, allocationLabel: '$0.00', usePercentage: true },
  ])('shows the $color balance beside the name and actual monthly allocation', async ({ balance, label, color, allocated, allocationLabel, usePercentage }) => {
    const wrapper = await setup({ currentBalance: balance, monthlyAllocations: allocated, usePercentage, budgetedPercentage: 15 })
    const row = wrapper.get('.category-clickable-row')
    const heading = row.get('.budget-category-heading')
    expect(heading.get('.budget-category-name').text()).toBe('Groceries')
    expect(heading.getComponent(components.VChip).text()).toBe(`Balance: ${label}`)
    expect(heading.getComponent(components.VChip).props('color')).toBe(color)
    expect(row.get('.budget-chip-row').text()).toContain(`Allocated: ${allocationLabel}`)
    expect(row.get('[title="Allocated in October 2026"]').text()).toBe(`Allocated: ${allocationLabel}`)
    expect(row.get('.budget-chip-row').text()).toContain(usePercentage ? 'Budget: 15%' : 'Budget: $300.00')
    expect(row.get('.budget-chip-row').text()).toContain('Spent: $175.00')
  })

  it.each([390, 1440])('reveals averages inline from an icon toggle at %ipx without navigating', async width => {
    vi.stubGlobal('innerWidth', width)
    const wrapper = await setup()
    const row = wrapper.get('.category-clickable-row')
    expect(row.find('details').exists()).toBe(false)
    const toggle = row.get('.budget-averages-toggle')
    const averages = row.get('#budget-averages-1')
    expect(toggle.attributes('aria-label')).toBe('Show spending averages')
    expect(toggle.attributes('aria-expanded')).toBe('false')
    expect(toggle.attributes('aria-controls')).toBe('budget-averages-1')
    expect(averages.isVisible()).toBe(false)
    expect(averages.element.parentElement).toBe(row.get('.budget-chip-row').element)
    expect(row.findAll('.v-chip').filter(chip => chip.text().includes('avg:'))
      .every(chip => averages.element.contains(chip.element))).toBe(true)

    await toggle.trigger('click')
    expect(averages.isVisible()).toBe(true)
    expect(toggle.attributes('aria-expanded')).toBe('true')
    expect(toggle.attributes('aria-label')).toBe('Hide spending averages')
    expect(averages.findAll('.v-chip').map(chip => chip.text())).toEqual(['3mo avg: $350.00', '6mo avg: $280.00', '12mo avg: $260.00'])
    expect(averages.findAll('.v-chip').map(chip => chip.classes().includes('text-error'))).toEqual([true, false, false])
    await toggle.trigger('keydown', { key: 'Enter' })
    await toggle.trigger('click')
    expect(averages.isVisible()).toBe(false)
    expect(mocks.push).not.toHaveBeenCalled()

    await row.get('.budget-category-name').trigger('click')
    expect(mocks.push).toHaveBeenCalledExactlyOnceWith({ name: 'history', query: { categoryId: '1' } })
  })

  it('expands averages independently per category', async () => {
    mocks.get.mockImplementation(async (url: string) => url.startsWith('/api/budget?')
      ? { totalBudget: 30000, totalAccountAmount: 50000, estimatedMonthlyIncome: 40000, month: '2026-10', categories: [category, { ...category, id: 2, name: 'Gas' }] }
      : [])
    const wrapper = mount(Budget, {
      attachTo: document.body,
      global: { plugins: [createPinia(), createVuetify({ components, directives })], stubs: { AddTransactionDialog: true } },
    })
    await flushPromises()
    await wrapper.get('[aria-controls="budget-averages-2"]').trigger('click')
    expect(wrapper.get('#budget-averages-1').isVisible()).toBe(false)
    expect(wrapper.get('#budget-averages-2').isVisible()).toBe(true)
  })

  it('refreshes the allocation and its month context when the selected month changes', async () => {
    const wrapper = await setup()
    mocks.get.mockImplementation(async (url: string) => url.startsWith('/api/budget?')
      ? { totalBudget: 30000, totalAccountAmount: 50000, estimatedMonthlyIncome: 40000, month: '2026-11', categories: [{ ...category, monthlyAllocations: 42000 }] }
      : [])
    wrapper.getComponent(MonthPickerNav).vm.$emit('update:modelValue', new Date(2026, 10, 1))
    await flushPromises()
    expect(mocks.get).toHaveBeenCalledWith('/api/budget?month=2026-11-01')
    expect(wrapper.get('[title="Allocated in November 2026"]').text()).toBe('Allocated: $420.00')
    expect(wrapper.find('[title="Allocated in October 2026"]').exists()).toBe(false)
  })

  it.each([390, 1440])('edits the current category from its menu at %ipx and refreshes budget and category data', async width => {
    vi.stubGlobal('innerWidth', width)
    const wrapper = await setup()
    await wrapper.get('[data-category-actions="1"]').trigger('click')
    await flushPromises()
    await clickAction('Edit category')
    expect(mocks.push).not.toHaveBeenCalled()
    const editor = wrapper.getComponent(EditExpenseCategoryDialog)
    expect(editor.props('modelValue')).toBe(true)
    const fields = editor.findAllComponents(components.VTextField)
    expect(fields.find(field => field.props('label') === 'Budget')?.props('modelValue')).toBe('450.00')
    fields.find(field => field.props('label') === 'Name')!.vm.$emit('update:modelValue', 'Weekly groceries')
    await flushPromises()
    await clickAction('Save')
    expect(mocks.put).toHaveBeenCalledExactlyOnceWith('/api/expense-categories/1', {
      name: 'Weekly groceries', description: 'Household food', categoryName: 'Essentials',
      budgetedAmount: 45000, budgetedPercentage: 0, cap: 50000, accountId: 3,
    })
    expect(editor.props('modelValue')).toBe(false)
    expect(mocks.get.mock.calls.filter(([url]) => url === '/api/expense-categories')).toHaveLength(2)
    expect(mocks.get.mock.calls.filter(([url]) => url.startsWith('/api/budget?'))).toHaveLength(2)
  })

  it.each([
    { width: 390, balance: -12345, amount: '123.45' },
    { width: 1440, balance: -1, amount: '0.01' },
    { width: 390, balance: 0, amount: '' },
    { width: 1440, balance: 12500, amount: '' },
  ])('prefills a transfer using the displayed $balance balance at $width px', async ({ width, balance, amount }) => {
    vi.stubGlobal('innerWidth', width)
    const wrapper = await setup({ currentBalance: balance }, false)
    await wrapper.get('[data-category-actions="1"]').trigger('click')
    await flushPromises()
    await clickAction('Transfer to category')
    expect(mocks.push).not.toHaveBeenCalled()
    const dialog = wrapper.getComponent(AddTransactionDialog)
    const selectors = dialog.findAllComponents(CategorySelector)
    expect(selectors.map(selector => [selector.props('label'), selector.props('modelValue')]))
      .toEqual([['From Category', null], ['To Category', 1]])
    const amountField = dialog.findAllComponents(components.VTextField).find(field => field.props('label') === 'Amount ($)')!
    expect(amountField.props('modelValue')).toBe(amount)
    selectors[0]!.vm.$emit('update:modelValue', 2)
    if (amount === '') amountField.vm.$emit('update:modelValue', '10.25')
    await flushPromises()
    await clickAction('Save')
    expect(mocks.post).toHaveBeenCalledExactlyOnceWith('/api/transactions/transfer', expect.objectContaining({
      fromCategoryId: 2, toCategoryId: 1, amount: amount === '' ? 1025 : Math.abs(balance),
    }))
    expect(dialog.props('modelValue')).toBe(false)
    expect(mocks.get.mock.calls.filter(([url]) => url.startsWith('/api/budget?'))).toHaveLength(2)
    await wrapper.get('[aria-label="Add Transaction"]').trigger('click')
    await flushPromises()
    expect(dialog.findAllComponents(CategorySelector).map(selector => selector.props('label'))).toEqual(['Category'])
    expect(dialog.props('initialTransfer')).toBeNull()
  })

  it('opens category history through the action menu without activating the row twice', async () => {
    const wrapper = await setup()
    await wrapper.get('[data-category-actions="1"]').trigger('click')
    await flushPromises()
    expect(mocks.push).not.toHaveBeenCalled()
    await clickAction('View history')
    expect(mocks.push).toHaveBeenCalledExactlyOnceWith({ name: 'history', query: { categoryId: '1' } })
  })
})
