import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import type { ExpenseCategoryDto } from '@/types'
import IncomeAllocationList from './IncomeAllocationList.vue'

const api = vi.hoisted(() => ({ get: vi.fn() }))
vi.mock('@/services/apiClient', () => ({ apiClient: api }))

enableAutoUnmount(afterEach)

function category(overrides: Partial<ExpenseCategoryDto> = {}): ExpenseCategoryDto {
  return {
    id: 1,
    name: 'Groceries',
    description: null,
    categoryName: null,
    accountId: null,
    budgetedAmount: 120000,
    budgetedPercentage: 0,
    currentBalance: 13383,
    cap: null,
    isHidden: false,
    usePercentage: false,
    hasItems: true,
    ...overrides,
  }
}

function mountList(categories: ExpenseCategoryDto[], modelValue: Record<number, string> = {}) {
  return mount(IncomeAllocationList, {
    props: { totalCents: 50000, categories, month: new Date(2026, 9, 25), modelValue },
    global: { stubs: { AmountField: true } },
  })
}

describe('IncomeAllocationList', () => {
  beforeEach(() => {
    api.get.mockReset().mockResolvedValue({
      1: { currentAmount: 20000, remainingAmount: 100000 },
    })
  })

  it.each([
    { currentBalance: 13383, usePercentage: false, expected: '$133.83' },
    { currentBalance: -27734, usePercentage: false, expected: '-$277.34' },
    { currentBalance: 202500, usePercentage: true, expected: '$2,025.00' },
    { currentBalance: -500, usePercentage: true, expected: '-$5.00' },
    { currentBalance: 0, usePercentage: false, expected: '$0.00' },
  ])('shows the signed category balance ($currentBalance, percentage: $usePercentage)', async ({
    currentBalance, usePercentage, expected,
  }) => {
    const wrapper = mountList([category({ currentBalance, usePercentage, budgetedPercentage: 10 })], { 1: '50.00' })
    await flushPromises()

    expect(wrapper.text()).toContain(`Current: ${expected}`)
    expect(wrapper.text()).not.toContain('Current: $200.00')
    expect(wrapper.text()).not.toContain('Current: $50.00')
  })

  it('keeps remaining budget suggestions separate from the current balance', async () => {
    const wrapper = mountList([category()])
    await flushPromises()

    expect(api.get).toHaveBeenCalledWith('/api/expense-categories/remaining-budget?month=2026-10-01')
    expect(wrapper.text()).toContain('Current: $133.83')
    expect(wrapper.get('.allocation-link').text()).toBe('$1,000.00')

    await wrapper.get('.allocation-link').trigger('click')

    expect(wrapper.emitted('update:modelValue')).toEqual([[{ 1: '500.00' }]])
    expect(wrapper.text()).toContain('Current: $133.83')
  })

  it('suggests an allocation to cover a negative balance', async () => {
    const wrapper = mountList([category({ currentBalance: -27734 })])
    await flushPromises()

    expect(wrapper.text()).toContain('Suggested allocation:')
    const suggestion = wrapper.get(
      '[aria-label="Allocate $277.34 toward the negative balance for Groceries"]',
    )
    expect(suggestion.text()).toBe('$277.34')

    await suggestion.trigger('click')

    expect(wrapper.emitted('update:modelValue')).toEqual([[{ 1: '277.34' }]])
  })

  it('limits a negative-balance suggestion to the income still available', async () => {
    const wrapper = mountList(
      [category({ currentBalance: -27734 }), category({ id: 2, name: 'Rent' })],
      { 2: '400.00' },
    )
    await flushPromises()

    const suggestion = wrapper.get(
      '[aria-label="Allocate $100.00 toward the negative balance for Groceries"]',
    )
    expect(suggestion.text()).toBe('$100.00')

    await suggestion.trigger('click')

    expect(wrapper.emitted('update:modelValue')).toEqual([[
      { 2: '400.00', 1: '100.00' },
    ]])
  })

  it('prompts for an income amount when a negative balance has no available suggestion', async () => {
    const wrapper = mountList([category({ currentBalance: -27734 })])
    await flushPromises()
    await wrapper.setProps({ totalCents: 0 })

    expect(wrapper.text()).toContain('Suggested allocation:')
    expect(wrapper.text()).toContain('Enter an income amount to see a suggestion.')
    expect(wrapper.find('[aria-label^="Allocate "]').exists()).toBe(false)
  })

  it('updates Current when the category balance changes, not when draft allocations change', async () => {
    const wrapper = mountList([category({ usePercentage: true, budgetedPercentage: 10 })])
    await flushPromises()

    await wrapper.get('.allocation-link').trigger('click')
    expect(wrapper.emitted('update:modelValue')).toEqual([[{ 1: '50.00' }]])
    await wrapper.setProps({ modelValue: { 1: '50.00' } })
    expect(wrapper.text()).toContain('Current: $133.83')

    await wrapper.setProps({ categories: [category({ currentBalance: -1000, usePercentage: true })] })
    expect(wrapper.text()).toContain('Current: -$10.00')
  })
})
