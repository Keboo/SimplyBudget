import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Settings from './Settings.vue'

const api = vi.hoisted(() => ({ get: vi.fn(), put: vi.fn(), post: vi.fn() }))
vi.mock('@/services/apiClient', () => ({ apiClient: api }))
vi.mock('@/stores/auth', () => ({ useAuthStore: () => ({ displayName: 'Reviewer' }) }))
enableAutoUnmount(afterEach)
afterEach(() => { document.body.innerHTML = '' })
const category = { id: 1, name: 'Long household essentials', description: 'Food and cleaning', categoryName: 'Essentials', usePercentage: false, budgetedAmount: 12000, budgetedPercentage: 10, cap: 20000, accountId: 3, hasItems: true, isHidden: false }
beforeEach(() => {
  vi.stubGlobal('innerWidth', 390)
  vi.stubGlobal('visualViewport', { addEventListener: vi.fn(), removeEventListener: vi.fn(), width: 390, height: 844, offsetTop: 0, offsetLeft: 0 })
  api.get.mockReset().mockImplementation(async (url: string) => url.startsWith('/api/expense-categories') ? [{ ...category }] : url === '/api/accounts' ? [{ id: 3, name: 'Household checking', currentAmount: 25000, validatedDate: '2026-10-01', isDefault: true }] : [])
  api.put.mockReset().mockResolvedValue({})
  api.post.mockReset().mockResolvedValue({})
})
async function click(text: string) {
  const button = [...document.querySelectorAll<HTMLElement>('button,.v-list-item')].find(e => e.textContent?.trim() === text)!
  expect(button, text).toBeDefined()
  button.click()
  await flushPromises()
}
async function setup(panel: string) {
  const wrapper = mount(Settings, { attachTo: document.body, global: { plugins: [createPinia(), createVuetify({ components, directives })] } })
  await flushPromises()
  const button = wrapper.findAll('button').find(b => b.text().startsWith(panel))!
  await button.trigger('click')
  await flushPromises()
  return wrapper
}
describe('focused phone settings editors', () => {
  it('opens category fields in a focused dialog and preserves all existing save fields', async () => {
    const wrapper = await setup('Expense Categories')
    await wrapper.get('[data-category-actions="1"]').trigger('click')
    await click('Edit category')
    const dialog = document.querySelector('[role="dialog"]')!
    expect(dialog.textContent).toContain('Budget type')
    const name = dialog.querySelector<HTMLInputElement>('input')!
    name.value = 'Renamed essentials'
    name.dispatchEvent(new Event('input', { bubbles: true }))
    await click('Save')
    expect(api.put).toHaveBeenCalledWith('/api/expense-categories/1', {
      name: 'Renamed essentials', description: 'Food and cleaning', categoryName: 'Essentials',
      budgetedAmount: 12000, budgetedPercentage: 0, cap: 20000, accountId: 3,
    })
  })
  it('keeps account edits out of the list and cancels without a mutation', async () => {
    const wrapper = await setup('Accounts')
    await wrapper.get('[data-account-actions="3"]').trigger('click')
    await click('Edit account name')
    expect(document.querySelector('[role="dialog"]')?.textContent).toContain('Account Name')
    await click('Cancel')
    expect(api.put).not.toHaveBeenCalled()
    expect(wrapper.text()).toContain('Household checking')
  })
})
