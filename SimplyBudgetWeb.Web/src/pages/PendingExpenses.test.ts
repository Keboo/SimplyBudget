import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ref } from 'vue'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import PendingExpenses from './PendingExpenses.vue'

const api = vi.hoisted(() => ({ get: vi.fn(), post: vi.fn(), put: vi.fn(), delete: vi.fn() }))
vi.mock('@/services/apiClient', () => ({ apiClient: api }))
vi.mock('@/composables/useMonthQueryParam', () => ({ useMonthQueryParam: () => ({ currentMonth: ref(new Date(2026, 9, 1)) }) }))
enableAutoUnmount(afterEach)
afterEach(() => { document.body.innerHTML = '' })
const item = { id: 7, version: 'original-version', date: '2026-10-23', description: 'Long neighborhood grocery merchant', amount: 8427, isDebit: true, assigneeId: 1, assigneeName: 'Alexandra Household Reviewer', notes: 'Check receipt' }
beforeEach(() => {
  vi.stubGlobal('innerWidth', 390)
  vi.stubGlobal('visualViewport', { addEventListener: vi.fn(), removeEventListener: vi.fn(), width: 390, height: 844, offsetTop: 0, offsetLeft: 0 })
  api.get.mockReset().mockImplementation(async (url: string) => {
    if (url.startsWith('/api/pending-expenses?')) return [{ ...item }]
    if (url === '/api/assignees') return [{ id: 1, name: item.assigneeName }]
    if (url === '/api/pending-expenses/7') return { ...item, version: 'refreshed-version' }
    return []
  })
  api.post.mockReset().mockResolvedValue({})
  api.put.mockReset().mockResolvedValue({})
  api.delete.mockReset().mockResolvedValue({})
})
async function setup() {
  const wrapper = mount(PendingExpenses, {
    attachTo: document.body,
    global: {
      plugins: [createPinia(), createVuetify({ components, directives })],
      stubs: { ConvertPendingExpenseDialog: { props: ['modelValue', 'pendingExpense'], template: '<div v-if="modelValue" data-review>{{ pendingExpense.description }}</div>' }, AddRuleDialog: true },
    },
  })
  await flushPromises()
  return wrapper
}
async function click(text: string) {
  const dialog = document.querySelector('.v-dialog.v-overlay--active')
  const button = [...(dialog ?? document).querySelectorAll<HTMLElement>('button,.v-list-item')].find(e => e.textContent?.trim() === text)!
  expect(button, text).toBeDefined()
  button.click()
  await flushPromises()
}
describe('phone pending review', () => {
  it('opens explicit review and unassigns through a separate action while retaining version and notes', async () => {
    const wrapper = await setup()
    await click('Review / Apply')
    expect(wrapper.get('[data-review]').text()).toBe(item.description)
    await click(`Assigned to: ${item.assigneeName}`)
    await click('Unassign')
    expect(api.put).toHaveBeenCalledWith('/api/pending-expenses/7', { assigneeId: null, notes: 'Check receipt', version: 'original-version' })
    expect(wrapper.text()).toContain('Assigned to: Unassigned')
  })
  it('requires confirmation before bulk rules or deletion can run', async () => {
    await setup()
    await click('Manage')
    await click('Re-run Rules')
    expect(api.post).not.toHaveBeenCalled()
    expect(document.querySelector('[role="dialog"]')?.textContent).toContain('Suggested categories and notes may change')
    await click('Re-run Rules')
    expect(api.post).toHaveBeenCalledWith('/api/pending-expenses/reapply-rules')
    await click('Manage')
    await click('Delete All')
    expect(api.delete).not.toHaveBeenCalled()
    await click('Delete All')
    expect(api.delete).toHaveBeenCalledWith('/api/pending-expenses?month=2026-10-01')
  })
})
