import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import EditExpenseCategoryDialog from './EditExpenseCategoryDialog.vue'
import type { ExpenseCategoryDto } from '@/types'

const api = vi.hoisted(() => ({ put: vi.fn() }))
vi.mock('@/services/apiClient', () => ({ apiClient: api }))
enableAutoUnmount(afterEach)
beforeEach(() => {
  vi.stubGlobal('visualViewport', { addEventListener: vi.fn(), removeEventListener: vi.fn(), width: 1440, height: 900, offsetTop: 0, offsetLeft: 0 })
  api.put.mockReset().mockResolvedValue({})
})
afterEach(() => { document.body.innerHTML = '' })

const category: ExpenseCategoryDto = {
  id: 1, name: 'Groceries', description: 'Food', categoryName: 'Essentials', accountId: 3,
  budgetedAmount: 12000, budgetedPercentage: 0, cap: 20000, usePercentage: false,
  currentBalance: 0, isHidden: false, hasItems: true,
}

async function setup() {
  const wrapper = mount(EditExpenseCategoryDialog, {
    props: { modelValue: true, category },
    attachTo: document.body,
    global: { plugins: [createPinia(), createVuetify({ components, directives })] },
  })
  await flushPromises()
  return wrapper
}

describe('shared expense category editor', () => {
  it('saves percentage budgets and clears an optional cap without losing the account', async () => {
    const wrapper = await setup()
    wrapper.getComponent(components.VSelect).vm.$emit('update:modelValue', 'percentage')
    await flushPromises()
    const fields = wrapper.findAllComponents(components.VTextField)
    fields.find(field => field.props('label') === 'Budget')!.vm.$emit('update:modelValue', '15')
    fields.find(field => field.props('label') === 'Cap')!.vm.$emit('update:modelValue', '')
    await wrapper.getComponent(components.VForm).trigger('submit')
    await flushPromises()
    expect(api.put).toHaveBeenCalledExactlyOnceWith('/api/expense-categories/1', {
      name: 'Groceries', description: 'Food', categoryName: 'Essentials',
      budgetedAmount: 0, budgetedPercentage: 15, cap: null, accountId: 3,
    })
    expect(wrapper.emitted('success')).toHaveLength(1)
    expect(wrapper.emitted('update:modelValue')).toEqual([[false]])
  })

  it('cancels without saving and discards edits when reopened for another category', async () => {
    const wrapper = await setup()
    wrapper.findAllComponents(components.VTextField)[0]!.vm.$emit('update:modelValue', 'Unsaved name')
    await wrapper.findAllComponents(components.VBtn).find(button => button.text() === 'Cancel')!.trigger('click')
    expect(api.put).not.toHaveBeenCalled()
    expect(wrapper.emitted('update:modelValue')).toEqual([[false]])
    await wrapper.setProps({ modelValue: false })
    await wrapper.setProps({ modelValue: true, category: { ...category, id: 2, name: 'Savings', budgetedAmount: 98765 } })
    const fields = wrapper.findAllComponents(components.VTextField)
    expect(fields[0]!.props('modelValue')).toBe('Savings')
    expect(fields.find(field => field.props('label') === 'Budget')!.props('modelValue')).toBe('987.65')
  })

  it('keeps unsaved changes open after a failed save and allows retrying', async () => {
    api.put.mockRejectedValueOnce(new Error('Unavailable'))
    const wrapper = await setup()
    wrapper.findAllComponents(components.VTextField)[0]!.vm.$emit('update:modelValue', 'New name')
    await wrapper.getComponent(components.VForm).trigger('submit')
    await flushPromises()
    expect(wrapper.emitted('success')).toBeUndefined()
    expect(wrapper.emitted('update:modelValue')).toBeUndefined()
    expect(wrapper.findAllComponents(components.VTextField)[0]!.props('modelValue')).toBe('New name')
    await wrapper.getComponent(components.VForm).trigger('submit')
    await flushPromises()
    expect(api.put).toHaveBeenLastCalledWith('/api/expense-categories/1', expect.objectContaining({ name: 'New name' }))
    expect(wrapper.emitted('success')).toHaveLength(1)
  })
})
