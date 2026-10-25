import { afterEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Import from './Import.vue'

const api = vi.hoisted(() => ({ post: vi.fn(), get: vi.fn() }))
const push = vi.hoisted(() => vi.fn())
vi.mock('@/services/apiClient', () => ({ apiClient: api }))
vi.mock('vue-router', () => ({ useRouter: () => ({ push }) }))
enableAutoUnmount(afterEach)
afterEach(() => { document.body.innerHTML = '' })

describe('phone CSV review', () => {
  it('retains duplicate selection and selected count when importing transaction cards', async () => {
    vi.stubGlobal('innerWidth', 390)
    const items = [
      { date: '2026-10-23', description: 'Long household purchase', amount: 1234, isDebit: true, isChecked: true, isDuplicate: false },
      { date: '2026-10-23', description: 'Possible repeated purchase', amount: 2500, isDebit: false, isChecked: false, isDuplicate: true },
    ]
    api.post.mockReset().mockImplementation(async (url: string) => url === '/api/import/parse' ? items : {})
    api.get.mockReset().mockResolvedValue({ month: '2026-09-01' })
    const wrapper = mount(Import, { attachTo: document.body, global: { plugins: [createPinia(), createVuetify({ components, directives })] } })
    const input = wrapper.get('input[accept=".csv,text/csv"]')
    const file = new File(['csv'], 'transactions.csv', { type: 'text/csv' })
    Object.defineProperty(file, 'text', { value: async () => 'Date,Description,Amount' })
    Object.defineProperty(input.element, 'files', { value: { item: () => file } })
    await input.trigger('change')
    await wrapper.findAll('button').find(b => b.text() === 'Parse')!.trigger('click')
    await flushPromises()
    expect(wrapper.find('table').exists()).toBe(false)
    expect(wrapper.text()).toContain('1 selected')
    wrapper.getComponent(components.VSwitch).vm.$emit('update:modelValue', true)
    await flushPromises()
    expect(wrapper.text()).toContain('Possible duplicate')
    const checkboxes = wrapper.findAllComponents(components.VCheckbox)
    checkboxes[1]!.vm.$emit('update:modelValue', true)
    await flushPromises()
    expect(wrapper.text()).toContain('2 selected')
    await wrapper.findAll('button').find(b => b.text() === 'Import 2 Items')!.trigger('click')
    await flushPromises()
    expect(api.post).toHaveBeenCalledWith('/api/import/save', expect.arrayContaining([
      expect.objectContaining({ description: 'Possible repeated purchase', isChecked: true, isDuplicate: true, amount: 2500 }),
    ]))
    expect(push).toHaveBeenCalledWith({ path: '/pending-expenses', query: { month: '2026-09' } })
  })
})
