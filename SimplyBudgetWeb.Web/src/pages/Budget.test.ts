import { ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Budget from './Budget.vue'
import MonthPickerNav from '@/components/MonthPickerNav.vue'
import type { BudgetCategoryDto } from '@/types'

const mocks = vi.hoisted(() => ({ get: vi.fn(), push: vi.fn() }))
vi.mock('@/services/apiClient', () => ({ apiClient: { get: mocks.get } }))
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
  vi.unstubAllGlobals()
})
beforeEach(() => {
  mocks.get.mockReset()
  mocks.push.mockReset()
})

async function setup(overrides: Partial<BudgetCategoryDto> = {}) {
  mocks.get.mockImplementation(async (url: string) => url.startsWith('/api/budget?')
    ? { totalBudget: 30000, totalAccountAmount: 50000, estimatedMonthlyIncome: 40000, month: '2026-10', categories: [{ ...category, ...overrides }] }
    : [])
  const wrapper = mount(Budget, {
    attachTo: document.body,
    global: {
      plugins: [createPinia(), createVuetify({ components, directives })],
      stubs: { AddTransactionDialog: true },
    },
  })
  await flushPromises()
  return wrapper
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

  it.each([390, 1440])('keeps all averages in a closed disclosure at %ipx without navigating on interaction', async width => {
    vi.stubGlobal('innerWidth', width)
    const wrapper = await setup()
    const row = wrapper.get('.category-clickable-row')
    const details = row.get('details')
    expect(details.element.open).toBe(false)
    expect(details.get('summary').text()).toBe('Spending averages')
    expect(details.text()).toContain('3mo avg: $350.00')
    expect(details.text()).toContain('6mo avg: $280.00')
    expect(details.text()).toContain('12mo avg: $260.00')
    expect(row.findAll('.v-chip').filter(chip => chip.text().includes('avg:'))
      .every(chip => details.element.contains(chip.element))).toBe(true)
    expect(details.findAll('.v-chip').map(chip => chip.classes().includes('text-error'))).toEqual([true, false, false])
    await details.get('summary').trigger('click')
    await details.get('summary').trigger('keydown', { key: 'Enter' })
    expect(mocks.push).not.toHaveBeenCalled()

    await row.get('.budget-category-name').trigger('click')
    expect(mocks.push).toHaveBeenCalledExactlyOnceWith({ name: 'history', query: { categoryId: '1' } })
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
