import { afterEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import ReceiptSplitDialog from './ReceiptSplitDialog.vue'

const api = vi.hoisted(() => ({ get: vi.fn() }))
vi.mock('@/services/apiClient', () => ({ apiClient: api }))
enableAutoUnmount(afterEach)
afterEach(() => {
  document.body.innerHTML = ''
  vi.clearAllMocks()
})

const categories = [
  { id: 1, name: 'Groceries', description: null, categoryName: null, accountId: 1, budgetedAmount: 0, budgetedPercentage: 0, currentBalance: 0, cap: null, isHidden: false, usePercentage: false, hasItems: true },
  { id: 2, name: 'Household', description: null, categoryName: null, accountId: 1, budgetedAmount: 0, budgetedPercentage: 0, currentBalance: 0, cap: null, isHidden: false, usePercentage: false, hasItems: true },
]

const receipt = {
  id: 3,
  isLinked: false,
  merchantName: 'Market',
  transactionDate: '2026-10-28',
  totalAmountCents: 1500,
  notes: null,
  processingStatus: 'reviewed',
  processingMessage: null,
  fileName: 'receipt.jpg',
  uploadedAtUtc: '2026-10-28T12:00:00Z',
  lineItems: [
    { id: 11, description: 'Apples', amountCents: 1000 },
    { id: 12, description: 'Soap', amountCents: 500 },
  ],
}

async function setup(suggestedCategoryId: number | null = 1) {
  api.get.mockResolvedValue({ options: [{ name: 'Local tax', percentage: 5, isDefault: true }] })
  vi.stubGlobal('visualViewport', {
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    width: 1024,
    height: 768,
    offsetTop: 0,
    offsetLeft: 0,
  })
  const wrapper = mount(ReceiptSplitDialog, {
    attachTo: document.body,
    props: {
      modelValue: true,
      receipt,
      categories,
      pendingAmountCents: 1575,
      suggestedCategoryId,
    },
    global: {
      plugins: [createPinia(), createVuetify({ components, directives })],
      stubs: {
        CategorySelector: {
          props: ['modelValue', 'categories', 'label'],
          template: `<select :aria-label="label" :value="modelValue ?? ''" @change="$emit('update:modelValue', $event.target.value === '' ? null : Number($event.target.value))">
            <option value="">Unassigned</option>
            <option v-for="category in categories" :key="category.id" :value="category.id">{{ category.name }}</option>
          </select>`,
        },
        ReceiptPreview: true,
      },
    },
  })
  await flushPromises()
  return wrapper
}

function findDialogButton(text: string) {
  return [...document.body.querySelectorAll<HTMLButtonElement>('.v-overlay--active button')]
    .find(button => button.textContent?.includes(text))
}

async function setSelectValue(select: HTMLSelectElement, value: string) {
  select.value = value
  select.dispatchEvent(new Event('change', { bubbles: true }))
  await flushPromises()
}

describe('ReceiptSplitDialog', () => {
  it('groups extracted items by category, applies the chosen tax rate, and emits transaction splits', async () => {
    const wrapper = await setup()
    const selectors = [...document.body.querySelectorAll<HTMLSelectElement>('.v-overlay--active select')]
    expect(selectors).toHaveLength(3)
    await setSelectValue(selectors[2]!, '2')

    expect(document.body.querySelector('.v-overlay--active')?.textContent).toContain('Remaining to allocate')
    expect(findDialogButton('Apply 2 splits')?.disabled).toBe(true)
    expect(document.body.querySelector('.v-overlay--active')?.textContent).toContain('$0.75')
    const taxRadioGroup = wrapper.findComponent({ name: 'VRadioGroup' })
    taxRadioGroup.vm.$emit('update:modelValue', 'tax-0')
    await flushPromises()

    expect(document.body.querySelector('.v-overlay--active')?.textContent).toContain('Matches pending expense')
    const apply = findDialogButton('Apply 2 splits')
    expect(apply).toBeDefined()
    apply!.click()
    await flushPromises()

    expect(wrapper.emitted('apply')).toEqual([[
      [
        { expenseCategoryId: 1, amount: 1050 },
        { expenseCategoryId: 2, amount: 525 },
      ],
    ]])
  })

  it('requires every included line to have a category and the totals to match the pending expense', async () => {
    const wrapper = await setup(null)
    expect(findDialogButton('Apply')?.disabled).toBe(true)
    expect(document.body.querySelector('.v-overlay--active')?.textContent).toContain('Assign or exclude 2 items')

    await setSelectValue([...document.body.querySelectorAll<HTMLSelectElement>('.v-overlay--active select')][0]!, '1')
    findDialogButton('Assign unassigned')!.click()
    wrapper.findComponent({ name: 'VRadioGroup' }).vm.$emit('update:modelValue', 'tax-0')
    await flushPromises()
    expect(document.body.querySelector('.v-overlay--active')?.textContent).toContain('Matches pending expense')
    expect(findDialogButton('Apply 1 split')?.disabled).toBe(false)
  })
})
