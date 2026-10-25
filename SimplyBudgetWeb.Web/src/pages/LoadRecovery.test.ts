import { ref } from 'vue'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Budget from './Budget.vue'
import History from './History.vue'
import PendingExpenses from './PendingExpenses.vue'
import MonthPickerNav from '@/components/MonthPickerNav.vue'

const api = vi.hoisted(() => ({ get: vi.fn() }))
vi.mock('@/services/apiClient', () => ({ apiClient: api }))
vi.mock('@/stores/auth', () => ({ useAuthStore: () => ({ getToken: vi.fn() }) }))
vi.mock('vue-router', () => ({ useRoute: () => ({ query: {} }), useRouter: () => ({ push: vi.fn() }) }))
vi.mock('@/composables/useMonthQueryParam', () => ({ useMonthQueryParam: () => ({ currentMonth: ref(new Date(2026, 9, 1)) }) }))
vi.mock('@/services/monthUpdatesHub', () => ({ createMonthUpdatesHubClient: () => ({ start: vi.fn(), stop: vi.fn(), setMonth: vi.fn() }) }))
enableAutoUnmount(afterEach)
afterEach(() => { document.body.innerHTML = '' })
beforeEach(() => {
  api.get.mockReset()
})
const category = { id: 1, name: 'Synthetic groceries', categoryName: 'Essentials', description: '', currentBalance: 100, budgetedAmount: 200, budgetedPercentage: 0, monthlyExpenses: 100, threeMonthAverage: 100, sixMonthAverage: 100, twelveMonthAverage: 100, usePercentage: false }
const cases = [
  { component: Budget, path: '/api/budget', label: 'budget', response: { totalBudget: 200, totalAccountAmount: 400, estimatedMonthlyIncome: 300, categories: [category] }, content: 'Synthetic groceries', empty: 'No budget categories yet' },
  { component: History, path: '/api/history', label: 'expenses', response: [{ id: 1, date: '2026-10-01', description: 'Synthetic groceries', isTransfer: false, details: [{ id: 1, expenseCategoryId: 1, categoryName: 'Food', amount: -100 }] }], content: 'Synthetic groceries', empty: 'No transactions for this month' },
  { component: PendingExpenses, path: '/api/pending-expenses', label: 'pending expenses', response: [{ id: 1, date: '2026-10-01', description: 'Synthetic groceries', amount: 100, isDebit: true, assigneeId: null }], content: 'Synthetic groceries', empty: 'No pending expenses for this month' },
]
describe.each(cases)('$label recovery surface', entry => {
  it('does not render failure as empty and retries into a successful result', async () => {
    let fail = true
    api.get.mockImplementation(async (url: string) => {
      if (url.startsWith(entry.path + '?')) {
        if (fail) throw new Error('Synthetic offline service')
        return entry.response
      }
      return []
    })
    const wrapper = mount(entry.component, { attachTo: document.body, global: { plugins: [createPinia(), createVuetify({ components, directives })], stubs: { RouterLink: true, AddTransactionDialog: true, ConvertPendingExpenseDialog: true, AddRuleDialog: true } } })
    await flushPromises()
    expect(wrapper.text()).toContain('Synthetic offline service')
    expect(wrapper.text()).not.toContain(entry.empty)
    const requestsBefore = api.get.mock.calls.filter(([url]) => String(url).startsWith(entry.path + '?')).length
    fail = false
    await wrapper.get(`[aria-label="Retry ${entry.label}"]`).trigger('click')
    await flushPromises()
    expect(api.get.mock.calls.filter(([url]) => String(url).startsWith(entry.path + '?'))).toHaveLength(requestsBefore + 1)
    expect(wrapper.text()).toContain(entry.content)
    expect(wrapper.text()).not.toContain('Synthetic offline service')
    if (entry.path !== '/api/budget') {
      const search = wrapper.findAllComponents(components.VTextField).find(field => field.props('label') === 'Search')!
      await search.get('input').setValue('Nothing matches')
      expect(wrapper.text()).toContain('match your filters')
      await wrapper.findAll('button').find(button => button.text() === 'Clear filters')!.trigger('click')
      expect(wrapper.text()).toContain(entry.content)
    }
    fail = true
    wrapper.getComponent(MonthPickerNav).vm.$emit('update:modelValue', new Date(2026, 9, 1))
    await flushPromises()
    expect(wrapper.text()).toContain('Showing last loaded data; it may be out of date.')
    expect(wrapper.text()).toContain(entry.content)
    wrapper.getComponent(MonthPickerNav).vm.$emit('update:modelValue', new Date(2026, 10, 1))
    await flushPromises()
    expect(wrapper.text()).not.toContain(entry.content)
    expect(wrapper.text()).not.toContain('Showing last loaded data')
  })
  it('shows a genuine empty state with a next action after a successful empty response', async () => {
    api.get.mockImplementation(async (url: string) => url.startsWith('/api/budget?')
      ? { totalBudget: 0, totalAccountAmount: 0, estimatedMonthlyIncome: 0, categories: [] }
      : [])
    const wrapper = mount(entry.component, { attachTo: document.body, global: { plugins: [createPinia(), createVuetify({ components, directives })], stubs: { RouterLink: true, AddTransactionDialog: true, ConvertPendingExpenseDialog: true, AddRuleDialog: true } } })
    await flushPromises()
    expect(wrapper.text()).toContain(entry.empty)
    expect(wrapper.text()).not.toContain('Could not load')
    expect(wrapper.text()).toContain(entry.path === '/api/budget' ? 'Manage categories' : entry.path === '/api/history' ? 'Add transaction' : 'Import transactions')
  })
})

describe.each(cases.filter(entry => entry.path !== '/api/pending-expenses'))('$label account feedback', entry => {
  it.each([390, 1440])('keeps account recovery outside collapsed disclosures at %ipx', async width => {
    const originalWidth = window.innerWidth
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: width })
    let accountRequest: Promise<unknown> = Promise.reject(new Error('Synthetic accounts offline'))
    api.get.mockImplementation((url: string) => {
      if (url.startsWith('/api/accounts?')) return accountRequest
      return Promise.resolve(url.startsWith(entry.path + '?') ? entry.response : [])
    })
    const wrapper = mount(entry.component, { attachTo: document.body, global: { plugins: [createPinia(), createVuetify({ components, directives })], stubs: { RouterLink: true, AddTransactionDialog: true, AddRuleDialog: true } } })
    Object.defineProperty(window, 'innerWidth', { configurable: true, value: originalWidth })
    await flushPromises()

    const disclosure = wrapper.findAll('details').find(details => details.text().includes('Account balances through'))
    if (disclosure) expect(disclosure.element.open).toBe(width >= 600)
    const retry = wrapper.get('[aria-label="Retry account balances"]')
    expect(retry.element.closest('details')).toBeNull()
    expect(wrapper.text()).toContain('Synthetic accounts offline')
    expect(wrapper.text()).not.toContain('No accounts found.')

    let completeAccounts!: (value: unknown) => void
    accountRequest = new Promise(resolve => { completeAccounts = resolve })
    const requestsBefore = api.get.mock.calls.filter(([url]) => String(url).startsWith('/api/accounts?')).length
    await retry.trigger('click')
    expect(wrapper.find('[aria-label="Loading account balances"]').exists()).toBe(true)
    expect(wrapper.text()).not.toContain('No accounts found.')
    completeAccounts([{ id: 1, name: 'Synthetic checking', currentAmount: 12345 }])
    await flushPromises()
    expect(api.get.mock.calls.filter(([url]) => String(url).startsWith('/api/accounts?'))).toHaveLength(requestsBefore + 1)
    expect(wrapper.text()).toContain('Synthetic checking')
    expect(wrapper.text()).not.toContain('Synthetic accounts offline')
    expect(wrapper.find('[aria-label="Loading account balances"]').exists()).toBe(false)

    accountRequest = new Promise(resolve => { completeAccounts = resolve })
    wrapper.getComponent(MonthPickerNav).vm.$emit('update:modelValue', new Date(2026, 9, 1))
    await flushPromises()
    const refreshing = wrapper.findAll('[role="status"]').find(status => status.text().includes('Refreshing account balances'))
    expect(refreshing).toBeDefined()
    expect(refreshing!.element.closest('details')).toBeNull()
    expect(wrapper.text()).toContain('Synthetic checking')
    completeAccounts([{ id: 1, name: 'Synthetic checking', currentAmount: 12345 }])
    await flushPromises()

    accountRequest = Promise.reject(new Error('Synthetic accounts offline'))
    wrapper.getComponent(MonthPickerNav).vm.$emit('update:modelValue', new Date(2026, 9, 1))
    await flushPromises()
    const staleFeedback = wrapper.findAll('[role="alert"]').find(alert => alert.text().includes('Could not load account balances'))!
    expect(staleFeedback.text()).toContain('Showing last loaded data; it may be out of date.')
    expect(staleFeedback.element.closest('details')).toBeNull()
    expect(wrapper.text()).toContain('Synthetic checking')

    accountRequest = Promise.reject(new Error('Synthetic accounts offline'))
    wrapper.getComponent(MonthPickerNav).vm.$emit('update:modelValue', new Date(2026, 10, 1))
    await flushPromises()
    expect(wrapper.text()).not.toContain('Synthetic checking')
    expect(wrapper.text()).not.toContain('Showing last loaded data')
    expect(wrapper.text()).not.toContain('No accounts found.')
    expect(wrapper.get('[aria-label="Retry account balances"]').element.closest('details')).toBeNull()

    accountRequest = Promise.resolve([])
    await wrapper.get('[aria-label="Retry account balances"]').trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('No accounts found.')
    expect(wrapper.text()).not.toContain('Synthetic accounts offline')
    expect(wrapper.find('[aria-label="Retry account balances"]').exists()).toBe(false)
  })
})
