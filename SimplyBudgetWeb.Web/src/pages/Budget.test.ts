import { ref } from 'vue'
import { afterAll, afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
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
import type { BudgetCategoryDto } from '@/types'

const mocks = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn(), put: vi.fn(), push: vi.fn() }))
vi.mock('@/services/apiClient', () => ({ apiClient: { get: mocks.get, post: mocks.post, put: mocks.put } }))
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

enableAutoUnmount(afterEach)
afterEach(() => {
  document.body.innerHTML = ''
})
afterAll(() => vi.unstubAllGlobals())
beforeEach(() => {
  vi.stubGlobal('innerWidth', 1024)
  vi.stubGlobal('visualViewport', { addEventListener: vi.fn(), removeEventListener: vi.fn(), width: 1440, height: 900, offsetTop: 0, offsetLeft: 0 })
  mocks.get.mockReset()
  mocks.post.mockReset().mockResolvedValue(undefined)
  mocks.put.mockReset().mockResolvedValue(undefined)
  mocks.push.mockReset()
})

async function setup(overrides: Partial<BudgetCategoryDto> = {}, realDialogs = false, extraCategories: BudgetCategoryDto[] = []) {
  const categories = [{ ...category, ...overrides }, ...extraCategories]
  mocks.get.mockImplementation(async (url: string) => url.startsWith('/api/budget?')
    ? { totalBudget: 30000, totalAccountAmount: 50000, estimatedMonthlyIncome: 40000, month: '2026-10', categories }
    : url === '/api/expense-categories' ? categories.map(category => ({ ...category, hasItems: true }))
    : [])
  const wrapper = mount(Budget, {
    attachTo: document.body,
    global: {
      plugins: [createPinia(), createVuetify({ components, directives })],
      stubs: { AddTransactionDialog: !realDialogs },
    },
  })
  await flushPromises()
  return wrapper
}

describe('budget category actions', () => {
  async function chooseAction(wrapper: Awaited<ReturnType<typeof setup>>, categoryId: number, title: string) {
    const button = wrapper.get(`[data-category-actions="${categoryId}"]`)
    expect(button.attributes('aria-haspopup')).toBe('menu')
    await button.trigger('click')
    await flushPromises()
    const action = [...document.querySelectorAll<HTMLElement>('.v-overlay--active [role="menuitem"]')].find(item => item.textContent?.trim() === title)!
    expect(action, title).toBeDefined()
    action.click()
    await flushPromises()
    expect(mocks.push).not.toHaveBeenCalled()
  }

  async function dialogButton(title: string) {
    const button = [...document.querySelectorAll<HTMLButtonElement>('[role="dialog"] button')].find(button => button.textContent?.trim() === title)!
    button.click()
    await flushPromises()
  }

  it('edits the selected category in a dialog and refreshes budget and category details after saving', async () => {
    const wrapper = await setup({}, true)
    await chooseAction(wrapper, 1, 'Edit category')
    const editor = wrapper.getComponent(EditExpenseCategoryDialog)
    expect(document.querySelector('[role="dialog"]')?.getAttribute('aria-label')).toBe('Edit expense category')
    expect(editor.props('category')?.id).toBe(1)
    const name = editor.findAllComponents(components.VTextField).find(field => field.props('label') === 'Name')!
    await name.get('input').setValue('Updated groceries')
    await dialogButton('Save')
    expect(mocks.put).toHaveBeenCalledExactlyOnceWith('/api/expense-categories/1', {
      name: 'Updated groceries', description: 'Household food', categoryName: 'Essentials',
      budgetedAmount: 30000, budgetedPercentage: 0, cap: null, accountId: null,
    })
    expect(editor.props('modelValue')).toBe(false)
    expect(mocks.get.mock.calls.filter(([url]) => url === '/api/budget?month=2026-10-01')).toHaveLength(2)
    expect(mocks.get.mock.calls.filter(([url]) => url === '/api/expense-categories')).toHaveLength(2)
  })

  it('prefills a positive deficit amount and destination but requires a different source and explicit Save', async () => {
    const wrapper = await setup({ currentBalance: -1234 }, true, [{ ...category, id: 2, name: 'Savings' }])
    await chooseAction(wrapper, 1, 'Create transfer')
    const editor = wrapper.getComponent(AddTransactionDialog)
    const selectors = editor.findAllComponents(CategorySelector)
    expect(selectors.map(selector => [selector.props('label'), selector.props('modelValue')])).toEqual([
      ['From Category', null], ['To Category', 1],
    ])
    const amount = editor.findAllComponents(components.VTextField).find(field => field.props('label') === 'Amount ($)')!
    expect(amount.props('modelValue')).toBe('12.34')
    const save = editor.findAllComponents(components.VBtn).find(button => button.text() === 'Save')!
    expect(save.props('disabled')).toBe(true)
    expect(mocks.post).not.toHaveBeenCalled()
    selectors[0]!.vm.$emit('update:modelValue', 1)
    await flushPromises()
    expect(save.props('disabled')).toBe(true)
    selectors[0]!.vm.$emit('update:modelValue', 2)
    await flushPromises()
    expect(save.props('disabled')).toBe(false)
    expect(mocks.post).not.toHaveBeenCalled()
    await dialogButton('Save')
    expect(mocks.post).toHaveBeenCalledExactlyOnceWith('/api/transactions/transfer', {
      description: '', date: expect.stringMatching(/^\d{4}-\d{2}-\d{2}$/),
      amount: 1234, fromCategoryId: 2, toCategoryId: 1,
    })
    expect(editor.props('modelValue')).toBe(false)
    expect(mocks.get.mock.calls.filter(([url]) => url === '/api/budget?month=2026-10-01')).toHaveLength(2)
    expect(mocks.get.mock.calls.filter(([url]) => url.startsWith('/api/accounts?'))).toHaveLength(2)
  })

  it('clears prior amounts and source when reopening for zero/positive balances and preserves normal entry defaults', async () => {
    const wrapper = await setup({ currentBalance: -501 }, true, [
      { ...category, id: 2, name: 'Zero balance', currentBalance: 0 },
      { ...category, id: 3, name: 'Positive balance', currentBalance: 700 },
    ])
    const editor = wrapper.getComponent(AddTransactionDialog)
    const amount = () => editor.findAllComponents(components.VTextField).find(field => field.props('label') === 'Amount ($)')!
    await chooseAction(wrapper, 1, 'Create transfer')
    expect(amount().props('modelValue')).toBe('5.01')
    editor.findAllComponents(CategorySelector)[0]!.vm.$emit('update:modelValue', 3)
    await dialogButton('Cancel')
    for (const id of [2, 3]) {
      await chooseAction(wrapper, id, 'Create transfer')
      expect(amount().props('modelValue')).toBe('')
      expect(editor.findAllComponents(CategorySelector).map(selector => selector.props('modelValue'))).toEqual([null, id])
      await amount().get('input').setValue('88.00')
      await dialogButton('Cancel')
    }
    await wrapper.get('[aria-label="Add Transaction"]').trigger('click')
    await flushPromises()
    expect(editor.findAllComponents(CategorySelector).map(selector => selector.props('label'))).toEqual(['Category'])
    expect(editor.getComponent(components.VTabs).props('modelValue')).toBe('transaction')
    expect(mocks.post).not.toHaveBeenCalled()
  })
})

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
})
