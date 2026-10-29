import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import AddTransactionDialog from './AddTransactionDialog.vue'
import CategorySelector from './CategorySelector.vue'
import AmountField from './AmountField.vue'
import IncomeAllocationList from './IncomeAllocationList.vue'
import type { ExpenseCategoryDto } from '@/types'

const api = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn() }))
vi.mock('@/services/apiClient', () => ({ apiClient: api }))
enableAutoUnmount(afterEach)
beforeEach(() => {
  vi.stubGlobal('innerWidth', 390)
  vi.stubGlobal('visualViewport', { addEventListener: vi.fn(), removeEventListener: vi.fn(), width: 390, height: 844, offsetTop: 0, offsetLeft: 0 })
  api.get.mockReset().mockResolvedValue({})
  api.post.mockReset().mockResolvedValue(undefined)
})
afterEach(() => { document.body.innerHTML = '' })
const categories = [1, 2].map(id => ({ id, name: `Long household category ${id}`, budgetedAmount: 10000, budgetedPercentage: 10, usePercentage: false })) as ExpenseCategoryDto[]
async function editor() {
  const wrapper = mount(AddTransactionDialog, {
    props: { modelValue: true, categories },
    attachTo: document.body,
    global: { plugins: [createPinia(), createVuetify({ components, directives })] },
  })
  await flushPromises()
  return wrapper
}
async function save() {
  const button = [...document.querySelectorAll<HTMLButtonElement>('button')].find(b => b.textContent?.trim() === 'Save')!
  expect(button.disabled).toBe(false)
  button.click()
  await flushPromises()
}

describe('adaptive transaction editor', () => {
  it('resets canceled transfer values before reopening with a different destination or a normal expense', async () => {
    const wrapper = await editor()
    await wrapper.setProps({ modelValue: false, initialTransfer: { toCategoryId: 1, amount: 12345 } })
    await wrapper.setProps({ modelValue: true })
    let selectors = wrapper.findAllComponents(CategorySelector)
    expect(selectors.map(selector => selector.props('modelValue'))).toEqual([null, 1])
    const amountField = () => wrapper.findAllComponents(components.VTextField).find(field => field.props('label') === 'Amount ($)')!
    expect(amountField().props('modelValue')).toBe('123.45')
    selectors[0]!.vm.$emit('update:modelValue', 2)
    amountField().vm.$emit('update:modelValue', '99.00')
    const cancel = [...document.querySelectorAll<HTMLButtonElement>('button')].find(button => button.textContent?.trim() === 'Cancel')!
    cancel.click()
    await flushPromises()
    expect(api.post).not.toHaveBeenCalled()
    await wrapper.setProps({ modelValue: false, initialTransfer: { toCategoryId: 2, amount: null } })
    await wrapper.setProps({ modelValue: true })
    selectors = wrapper.findAllComponents(CategorySelector)
    expect(selectors.map(selector => selector.props('modelValue'))).toEqual([null, 2])
    expect(amountField().props('modelValue')).toBe('')
    await wrapper.setProps({ modelValue: false, initialTransfer: null })
    await wrapper.setProps({ modelValue: true })
    expect(wrapper.findAllComponents(CategorySelector).map(selector => selector.props('modelValue'))).toEqual([null])
    expect(wrapper.findAllComponents(AmountField)[0]!.props('modelValue')).toBe('')
  })

  it('keeps split categories and committed amounts in the expense request', async () => {
    const wrapper = await editor()
    wrapper.findAllComponents(CategorySelector)[0]!.vm.$emit('update:modelValue', 1)
    await flushPromises()
    await wrapper.findAllComponents(AmountField)[0]!.get('input').setValue('12.34')
    await wrapper.findAllComponents(AmountField)[0]!.get('input').trigger('blur')
    await flushPromises()
    wrapper.findAllComponents(CategorySelector)[1]!.vm.$emit('update:modelValue', 2)
    await flushPromises()
    await wrapper.findAllComponents(AmountField)[1]!.get('input').setValue('5.67')
    await wrapper.findAllComponents(AmountField)[1]!.get('input').trigger('blur')
    await flushPromises()
    await save()
    expect(api.post).toHaveBeenCalledWith('/api/transactions/transaction', expect.objectContaining({
      items: [{ expenseCategoryId: 1, amount: 1234 }, { expenseCategoryId: 2, amount: 567 }],
    }))
    expect(wrapper.emitted('success')).toHaveLength(1)
  })
  it('uses the phone type picker for income allocations and transfers without changing their payloads', async () => {
    const wrapper = await editor()
    const picker = wrapper.findComponent(components.VSelect)
    picker.vm.$emit('update:modelValue', 'income')
    await flushPromises()
    const incomeInput = document.querySelector<HTMLInputElement>('input[type="number"]')!
    incomeInput.value = '25'
    incomeInput.dispatchEvent(new Event('input', { bubbles: true }))
    wrapper.getComponent(IncomeAllocationList).vm.$emit('update:modelValue', { 1: '25' })
    await flushPromises()
    await save()
    expect(api.post).toHaveBeenLastCalledWith('/api/transactions/income', expect.objectContaining({ items: [{ expenseCategoryId: 1, amount: 2500 }] }))
    picker.vm.$emit('update:modelValue', 'transfer')
    await flushPromises()
    const transferInput = document.querySelector<HTMLInputElement>('input[type="number"]')!
    transferInput.value = '8'
    transferInput.dispatchEvent(new Event('input', { bubbles: true }))
    wrapper.findAllComponents(CategorySelector)[0]!.vm.$emit('update:modelValue', 1)
    wrapper.findAllComponents(CategorySelector)[1]!.vm.$emit('update:modelValue', 2)
    await flushPromises()
    await save()
    expect(api.post).toHaveBeenLastCalledWith('/api/transactions/transfer', expect.objectContaining({ amount: 800, fromCategoryId: 1, toCategoryId: 2 }))
  })
})
