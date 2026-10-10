import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import type { ReceiptDto } from '@/types'
import { useSnackbarStore } from '@/stores/snackbar'

const api = vi.hoisted(() => ({
  get: vi.fn(),
  upload: vi.fn(),
  put: vi.fn(),
  download: vi.fn(),
}))
const flatten = vi.hoisted(() => vi.fn())

vi.mock('@/services/apiClient', () => ({ apiClient: api }))
vi.mock('@/utils/receiptImage', () => ({ flattenReceiptImage: flatten }))

import Receipts from './Receipts.vue'

function receipt(overrides: Partial<ReceiptDto> = {}): ReceiptDto {
  return {
    id: 1,
    merchantName: 'Corner Cafe',
    transactionDate: '2026-03-04T00:00:00',
    totalAmountCents: 1250,
    notes: null,
    processingStatus: 'needs-review',
    processingMessage: null,
    fileName: 'receipt.jpg',
    uploadedAtUtc: '2026-03-04T10:00:00Z',
    lineItems: [{ id: 1, description: 'Latte', amountCents: 450 }],
    ...overrides,
  } as ReceiptDto
}

async function mountPage() {
  const wrapper = mount(Receipts, {
    global: { plugins: [createVuetify({ components, directives })] },
    attachTo: document.body,
  })
  await flushPromises()
  return wrapper
}

async function chooseFile(wrapper: Awaited<ReturnType<typeof mountPage>>, file = new File(['raw'], 'photo.heic.jpg', { type: 'image/jpeg' })) {
  const input = wrapper.get('input[type="file"]')
  Object.defineProperty(input.element, 'files', { value: [file], configurable: true })
  await input.trigger('change')
  await flushPromises()
}

function uploadButton(wrapper: Awaited<ReturnType<typeof mountPage>>) {
  return wrapper.findAll('button').find(button => button.text().includes('Upload receipt'))!
}

describe('Receipts page', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    api.get.mockReset().mockResolvedValue([])
    api.upload.mockReset()
    api.put.mockReset()
    api.download.mockReset().mockResolvedValue({ blob: new Blob(['img']), fileName: null })
    flatten.mockReset().mockImplementation(async (file: File) =>
      new File([file], 'photo.jpg', { type: 'image/jpeg' }))
    URL.createObjectURL = vi.fn(() => 'blob:preview')
    URL.revokeObjectURL = vi.fn()
  })

  it('shows an empty state and keeps upload disabled until an image is chosen', async () => {
    const wrapper = await mountPage()

    expect(wrapper.text()).toContain('No receipts yet.')
    expect(uploadButton(wrapper).attributes('disabled')).toBeDefined()
  })

  it('flattens the chosen photo and enables upload', async () => {
    const wrapper = await mountPage()

    await chooseFile(wrapper)

    expect(flatten).toHaveBeenCalledOnce()
    expect(uploadButton(wrapper).attributes('disabled')).toBeUndefined()
  })

  it('reports image processing failures and keeps upload disabled', async () => {
    flatten.mockRejectedValue(new Error('Image processing did not initialize'))
    const wrapper = await mountPage()

    await chooseFile(wrapper)

    const snackbar = useSnackbarStore()
    expect(snackbar.messages.slice(-1)[0]).toMatchObject({ text: 'Image processing did not initialize', variant: 'error' })
    expect(uploadButton(wrapper).attributes('disabled')).toBeDefined()
  })

  it('uploads the image as multipart form data and selects the new receipt', async () => {
    api.upload.mockResolvedValue(receipt({ id: 5 }))
    const wrapper = await mountPage()
    await chooseFile(wrapper)

    await uploadButton(wrapper).trigger('click')
    await flushPromises()

    expect(api.upload).toHaveBeenCalledOnce()
    const [url, form] = api.upload.mock.calls[0] as [string, FormData]
    expect(url).toBe('/api/receipts')
    const image = form.get('image') as File
    expect(image.name).toBe('photo.jpg')
    expect(image.type).toBe('image/jpeg')
    expect(wrapper.text()).toContain('Review receipt')
    expect(wrapper.text()).toContain('Corner Cafe')
    expect(useSnackbarStore().messages.slice(-1)[0]).toMatchObject({
      text: 'Receipt uploaded. Review the extracted details.',
      variant: 'success',
    })
    expect(uploadButton(wrapper).attributes('disabled')).toBeDefined()
  })

  it('warns when extraction could not run but the receipt was saved', async () => {
    api.upload.mockResolvedValue(receipt({ processingMessage: 'Receipt extraction is not configured.' }))
    const wrapper = await mountPage()
    await chooseFile(wrapper)

    await uploadButton(wrapper).trigger('click')
    await flushPromises()

    expect(useSnackbarStore().messages.slice(-1)[0]).toMatchObject({
      text: 'Receipt extraction is not configured.',
      variant: 'warning',
    })
  })

  it('shows the server error and allows retrying when the upload fails', async () => {
    api.upload.mockRejectedValue(new Error('Receipt storage is not configured.'))
    const wrapper = await mountPage()
    await chooseFile(wrapper)

    await uploadButton(wrapper).trigger('click')
    await flushPromises()

    expect(useSnackbarStore().messages.slice(-1)[0]).toMatchObject({
      text: 'Receipt storage is not configured.',
      variant: 'error',
    })
    expect(uploadButton(wrapper).attributes('disabled')).toBeUndefined()
  })

  it('loads saved receipts, shows the first one, and saves corrections', async () => {
    api.get.mockResolvedValue([receipt()])
    api.put.mockResolvedValue(receipt({ merchantName: 'Fixed Cafe' }))
    const wrapper = await mountPage()

    expect(wrapper.text()).toContain('Corner Cafe')
    expect(api.download).toHaveBeenCalledWith('/api/receipts/1/image')

    const merchant = wrapper.findAll('input').find(input => (input.element as HTMLInputElement).value === 'Corner Cafe')!
    await merchant.setValue('Fixed Cafe')
    const save = wrapper.findAll('button').find(button => button.text().includes('Save corrections'))!
    await save.trigger('click')
    await flushPromises()

    expect(api.put).toHaveBeenCalledWith('/api/receipts/1', expect.objectContaining({
      merchantName: 'Fixed Cafe',
      totalAmountCents: 1250,
      lineItems: [{ description: 'Latte', amountCents: 450 }],
    }))
    expect(useSnackbarStore().messages.slice(-1)[0]).toMatchObject({ text: 'Receipt saved', variant: 'success' })
  })

  it('reports a failure to load receipts', async () => {
    api.get.mockRejectedValue(new Error('HTTP error! status: 500'))
    await mountPage()

    expect(useSnackbarStore().messages.slice(-1)[0]).toMatchObject({ text: 'HTTP error! status: 500', variant: 'error' })
  })
})
