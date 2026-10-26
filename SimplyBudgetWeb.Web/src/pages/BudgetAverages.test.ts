import { ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Budget from './Budget.vue'
import type { BudgetCategoryDto, BudgetResponse, ExpenseCategoryMonthlyExpensesDto } from '@/types'

const api = vi.hoisted(() => ({ get: vi.fn() }))
vi.mock('@/services/apiClient', () => ({ apiClient: api }))
vi.mock('@/stores/auth', () => ({ useAuthStore: () => ({ getToken: vi.fn() }) }))
vi.mock('vue-router', () => ({ useRoute: () => ({ query: {} }), useRouter: () => ({ push: vi.fn() }) }))
vi.mock('@/composables/useMonthQueryParam', () => ({
  useMonthQueryParam: () => ({ currentMonth: ref(new Date(2026, 9, 1)) }),
}))
vi.mock('@/services/monthUpdatesHub', () => ({
  createMonthUpdatesHubClient: () => ({ start: vi.fn(), stop: vi.fn(), setMonth: vi.fn() }),
}))

let mountedBudget: VueWrapper | undefined
afterEach(() => {
  mountedBudget?.unmount()
  mountedBudget = undefined
  document.body.innerHTML = ''
  vi.unstubAllGlobals()
})
beforeEach(() => { api.get.mockReset() })

describe('budget spending averages', () => {
  it.each([390, 1440])('matches graph averages including fractional-cent rounding at %ipx', async width => {
    const category: BudgetCategoryDto = {
      id: 1, name: 'Car Insurance', description: null, categoryName: 'Transportation',
      accountId: null, budgetedAmount: 14000, budgetedPercentage: 0, currentBalance: 0,
      cap: null, isHidden: false, usePercentage: false, monthlyExpenses: 12693, monthlyAllocations: 0,
      threeMonthAverage: 14693 / 3, sixMonthAverage: 22693 / 6, twelveMonthAverage: 40693 / 12,
    }
    const budget: BudgetResponse = {
      totalBudget: 14000, totalAccountAmount: 0, estimatedMonthlyIncome: 0,
      month: '2026-10', categories: [category],
    }
    const chart: ExpenseCategoryMonthlyExpensesDto = {
      expenseCategoryId: 1, name: 'Car Insurance', budgetedAmount: 14000,
      budgetedPercentage: 0, usePercentage: false,
      months: [12000, 0, 0, 0, 0, 6000, 5000, 0, 3000, 2000, 0, 12693].map((amount, index) => ({
        month: index < 2 ? `2025-${11 + index}` : `2026-${String(index - 1).padStart(2, '0')}`,
        amount,
      })),
    }
    api.get.mockImplementation(async (url: string) => {
      if (url === '/api/budget?month=2026-10-01') return budget
      if (url === '/api/expense-categories/1/monthly-expenses?month=2026-10-01&months=12') return chart
      if (url.startsWith('/api/accounts?') || url === '/api/expense-categories') return []
      throw new Error(`Unexpected request: ${url}`)
    })

    vi.stubGlobal('innerWidth', width)
    vi.stubGlobal('visualViewport', {
      addEventListener: vi.fn(), removeEventListener: vi.fn(),
      width, height: 844, offsetTop: 0, offsetLeft: 0,
    })
    const wrapper = mount(Budget, {
      attachTo: document.body,
      global: {
        plugins: [createPinia(), createVuetify({ components, directives })],
        stubs: { RouterLink: true, AddTransactionDialog: true },
      },
    })
    mountedBudget = wrapper
    await flushPromises()

    const expectedAmounts = ['$48.98', '$37.82', '$33.91']
    if (width < 600) {
      const averages = wrapper.findAll('details').find(details => details.text().includes('Spending averages'))!
      expect(averages.findAll('div').map(div => div.text())).toEqual(
        expectedAmounts.map((amount, index) => `${[3, 6, 12][index]} months: ${amount}`),
      )
    } else {
      expect(wrapper.findAll('.avg-spend-chip').map(chip => chip.text())).toEqual(
        expectedAmounts.map((amount, index) => `${[3, 6, 12][index]}mo avg: ${amount}`),
      )
    }

    await wrapper.get('[aria-label="Open category chart"]').trigger('click')
    await flushPromises()
    expect(api.get).toHaveBeenCalledWith('/api/expense-categories/1/monthly-expenses?month=2026-10-01&months=12')
    const dialog = document.querySelector('[role="dialog"]')!
    expect(dialog).not.toBeNull()
    expect(Array.from(dialog.querySelectorAll('.v-chip'))
      .map(chip => chip.textContent!.trim()).filter(text => text.includes('mo avg:'))).toEqual(
      expectedAmounts.map((amount, index) => `${[3, 6, 12][index]}mo avg: ${amount}`),
    )
  })
})
