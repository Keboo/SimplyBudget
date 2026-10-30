import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Settings from './Settings.vue'
import EditExpenseCategoryDialog from '@/components/EditExpenseCategoryDialog.vue'

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
describe('focused settings editors', () => {
  it.each([390, 1440])('opens category fields in a dialog at %ipx and preserves all existing save fields', async width => {
    vi.stubGlobal('innerWidth', width)
    const wrapper = await setup('Expense Categories')
    if (width === 390) {
      await wrapper.get('[data-category-actions="1"]').trigger('click')
      await click('Edit category')
    } else {
      await wrapper.get('[data-category-edit="1"]').trigger('click')
    }
    const dialog = document.querySelector('[role="dialog"]')!
    expect(dialog.textContent).toContain('Budget type')
    expect(wrapper.find('.management-row input, .v-list-item input').exists()).toBe(false)
    const name = dialog.querySelector<HTMLInputElement>('input')!
    name.value = 'Renamed essentials'
    name.dispatchEvent(new Event('input', { bubbles: true }))
    await click('Save')
    expect(api.put).toHaveBeenCalledWith('/api/expense-categories/1', {
      name: 'Renamed essentials', description: 'Food and cleaning', categoryName: 'Essentials',
      budgetedAmount: 12000, budgetedPercentage: 0, cap: 20000, accountId: 3,
    })
    expect(wrapper.getComponent(EditExpenseCategoryDialog).props('modelValue')).toBe(false)
    expect(api.get.mock.calls.filter(([url]) => url === '/api/expense-categories?includeHidden=false')).toHaveLength(2)
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

describe('category edit dialog validation and recovery', () => {
  async function submitEditor() {
    document.querySelector<HTMLFormElement>('[role="dialog"] form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }))
    await flushPromises()
  }

  async function editCategory() {
    vi.stubGlobal('innerWidth', 1440)
    const wrapper = await setup('Expense Categories')
    await wrapper.get('[data-category-edit="1"]').trigger('click')
    await flushPromises()
    return wrapper
  }

  it('cancels without saving and discards draft fields when reopened', async () => {
    const wrapper = await editCategory()
    const editor = wrapper.getComponent(EditExpenseCategoryDialog)
    await editor.findAllComponents(components.VTextField).find(field => field.props('label') === 'Name')!.get('input').setValue('Discard this')
    await click('Cancel')
    expect(api.put).not.toHaveBeenCalled()
    await wrapper.get('[data-category-edit="1"]').trigger('click')
    await flushPromises()
    expect(editor.findAllComponents(components.VTextField).find(field => field.props('label') === 'Name')!.props('modelValue')).toBe(category.name)
  })

  it('validates fixed budgets, caps and percentages, then preserves the draft after a failed save for retry', async () => {
    const wrapper = await editCategory()
    const editor = wrapper.getComponent(EditExpenseCategoryDialog)
    const field = (label: string) => editor.findAllComponents(components.VTextField).find(field => field.props('label') === label)!
    await field('Budget').get('input').setValue('-1')
    await submitEditor()
    expect(editor.getComponent(components.VCard).text()).toContain('Budgeted amount must be 0 or greater')
    await field('Budget').get('input').setValue('42.35')
    await field('Cap').get('input').setValue('-1')
    await submitEditor()
    expect(editor.getComponent(components.VCard).text()).toContain('Cap must be a valid amount')
    await field('Cap').get('input').setValue('')
    editor.getComponent(components.VSelect).vm.$emit('update:modelValue', 'percentage')
    await flushPromises()
    await field('Budget').get('input').setValue('12.5')
    await submitEditor()
    expect(editor.getComponent(components.VCard).text()).toContain('Budgeted percentage must be a whole number between 1 and 100')
    expect(api.put).not.toHaveBeenCalled()

    await field('Budget').get('input').setValue('25')
    let rejectSave!: (error: Error) => void
    api.put.mockImplementationOnce(() => new Promise((_resolve, reject) => { rejectSave = reject }))
    await submitEditor()
    expect(editor.getComponent(components.VDialog).props('persistent')).toBe(true)
    expect(editor.findAllComponents(components.VBtn).find(button => button.text() === 'Cancel')!.props('disabled')).toBe(true)
    rejectSave(new Error('Please try again'))
    await flushPromises()
    expect(editor.props('modelValue')).toBe(true)
    expect(editor.getComponent(components.VCard).text()).toContain('Please try again')
    expect(field('Budget').props('modelValue')).toBe('25')
    await submitEditor()
    await flushPromises()
    expect(api.put).toHaveBeenLastCalledWith('/api/expense-categories/1', {
      name: category.name, description: category.description, categoryName: category.categoryName,
      budgetedAmount: 0, budgetedPercentage: 25, cap: null, accountId: 3,
    })
    expect(editor.props('modelValue')).toBe(false)
  })
})
