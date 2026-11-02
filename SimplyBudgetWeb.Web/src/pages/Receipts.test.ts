import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import type { ExpenseCategoryDto, PendingExpenseDto, ReceiptDto } from '@/types'
import { useSnackbarStore } from '@/stores/snackbar'

const api = vi.hoisted(() => ({
  get: vi.fn(),
  upload: vi.fn(),
  put: vi.fn(),
  download: vi.fn(),
  delete: vi.fn(),
  post: vi.fn(),
}))
const flatten = vi.hoisted(() => vi.fn())
const detect = vi.hoisted(() => vi.fn())

vi.mock('@/services/apiClient', () => ({ apiClient: api }))
vi.mock('@/services/receiptCornerDetection', () => ({ detectReceiptCorners: detect }))
vi.mock('@/utils/receiptImage', async importOriginal => ({
  ...await importOriginal<typeof import('@/utils/receiptImage')>(),
  flattenReceiptImage: flatten,
}))

import Receipts from './Receipts.vue'
import ConvertPendingExpenseDialog from '@/components/ConvertPendingExpenseDialog.vue'
import ReceiptPreview from '@/components/ReceiptPreview.vue'
import ReceiptImageAdjustment from '@/components/ReceiptImageAdjustment.vue'
import { imageBoundary } from '@/utils/receiptImage'

enableAutoUnmount(afterEach)

function receipt(overrides: Partial<ReceiptDto> = {}): ReceiptDto {
  return {
    id: 1,
    isLinked: false,
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

async function editReceipt(wrapper: Awaited<ReturnType<typeof mountPage>>) {
  await wrapper.findAll('button').find(button => button.text() === 'Edit receipt')!.trigger('click')
}

async function clickDialogButton(text: string) {
  const button = [...document.querySelectorAll<HTMLButtonElement>('.v-overlay--active button')]
    .find(button => button.textContent?.trim() === text)
  expect(button, `Dialog button: ${text}`).toBeDefined()
  button!.click()
  await flushPromises()
}

function dialogRadio(value: string) {
  return document.querySelector<HTMLInputElement>(`.v-overlay--active input[type="radio"][value="${value}"]`)!
}

async function selectReceipt(value: string) {
  dialogRadio(value).click()
  await flushPromises()
}

function receiptToggle() {
  return document.querySelector<HTMLButtonElement>('.v-overlay--active .v-expansion-panel-title')!
}

async function toggleReceipts() {
  receiptToggle().click()
  await flushPromises()
}

describe('Receipts page', () => {
  afterEach(() => { document.body.innerHTML = '' })
  beforeEach(() => {
    vi.stubGlobal('innerWidth', 1024)
    vi.stubGlobal('visualViewport', { addEventListener: vi.fn(), removeEventListener: vi.fn(), width: 375, height: 812, offsetTop: 0, offsetLeft: 0 })
    setActivePinia(createPinia())
    api.get.mockReset().mockResolvedValue([])
    api.upload.mockReset()
    api.put.mockReset()
    api.delete.mockReset().mockResolvedValue(undefined)
    api.post.mockReset().mockResolvedValue(undefined)
    api.download.mockReset().mockResolvedValue({ blob: new Blob(['img']), fileName: null })
    flatten.mockReset().mockResolvedValue(new Blob(['flat'], { type: 'image/jpeg' }))
    detect.mockReset().mockResolvedValue(null)
    URL.createObjectURL = vi.fn(() => 'blob:preview')
    URL.revokeObjectURL = vi.fn()
  })

  it('drills into phone review and preserves drafts when returning to the list and switching receipts', async () => {
    vi.stubGlobal('innerWidth', 390)
    api.get.mockResolvedValue([receipt(), receipt({ id: 2, merchantName: 'Second shop' })])
    const wrapper = await mountPage()
    expect(document.querySelector('.v-dialog.v-overlay--active')).toBeNull()
    await wrapper.get('[data-receipt-id="1"]').trigger('click')
    await clickDialogButton('Edit receipt')
    const merchant = document.querySelector<HTMLInputElement>('.receipt-editor input')!
    merchant.value = 'Draft cafe'
    merchant.dispatchEvent(new Event('input', { bubbles: true }))
    await clickDialogButton('Back to list')
    await wrapper.get('[data-receipt-id="2"]').trigger('click')
    await clickDialogButton('Edit receipt')
    expect(document.querySelector<HTMLInputElement>('.receipt-editor input')!.value).toBe('Second shop')
    await clickDialogButton('Back to list')
    await wrapper.get('[data-receipt-id="1"]').trigger('click')
    await clickDialogButton('Edit receipt')
    expect(document.querySelector<HTMLInputElement>('.receipt-editor input')!.value).toBe('Draft cafe')
    expect(api.put).not.toHaveBeenCalled()
  })

  it('saves a phone photo without opening review or asking for recognized details', async () => {
    vi.stubGlobal('innerWidth', 390)
    api.upload.mockResolvedValue(receipt({ id: 7 }))
    const wrapper = await mountPage()
    await chooseFile(wrapper)
    expect(api.upload).toHaveBeenCalledOnce()
    expect(document.querySelector('.v-dialog.v-overlay--active')).toBeNull()
    expect(wrapper.get('[data-receipt-id="7"]').text()).toContain('$12.50')
    expect(useSnackbarStore().messages.slice(-1)[0]?.text).toBe('Receipt saved')
  })

  it('keeps a possible duplicate without interrupting capture and warns non-modally', async () => {
    api.get.mockResolvedValue([receipt({ id: 1, merchantName: 'CORNER-CAFE' })])
    api.upload.mockResolvedValue(receipt({ id: 7 }))
    const wrapper = await mountPage()

    await chooseFile(wrapper)
    expect(wrapper.find('[data-receipt-id="7"]').exists()).toBe(true)
    expect(document.querySelector('.v-overlay--active')).toBeNull()
    expect(useSnackbarStore().messages.slice(-1)[0]).toMatchObject({
      text: expect.stringContaining('may duplicate'),
      variant: 'warning',
    })
    expect(api.delete).not.toHaveBeenCalled()
  })

  it('does not warn when receipt totals differ', async () => {
    api.get.mockResolvedValue([receipt({ id: 1 })])
    api.upload.mockResolvedValue(receipt({ id: 7, totalAmountCents: 1251 }))
    const wrapper = await mountPage()

    await chooseFile(wrapper)

    expect(document.querySelector('.v-overlay--active')).toBeNull()
    expect(wrapper.find('[data-receipt-id="7"]').exists()).toBe(true)
  })

  it('shows only capture actions before a photo is chosen', async () => {
    const wrapper = await mountPage()

    expect(wrapper.text()).toContain('No pending receipts.')
    expect(wrapper.text()).toContain('Take a photo')
    expect(wrapper.text()).toContain('Choose file')
    expect(wrapper.find('.receipt-capture input:not([type="file"])').exists()).toBe(false)
    expect(wrapper.text()).not.toContain('Upload receipt')
    expect(wrapper.text()).not.toContain('Adjust corners')
  })

  it('shows the most recently uploaded receipts first', async () => {
    api.get.mockResolvedValue([
      receipt({ id: 1, uploadedAtUtc: '2026-03-04T10:00:00Z' }),
      receipt({ id: 2, uploadedAtUtc: '2026-03-06T10:00:00Z' }),
    ])
    const wrapper = await mountPage()

    expect(wrapper.findAll('.v-list-item').map(item => item.attributes('data-receipt-id'))).toEqual(['2', '1'])
  })

  it('automatically saves the original when no corners are detected', async () => {
    api.upload.mockResolvedValue(receipt())
    const wrapper = await mountPage()

    await chooseFile(wrapper)

    expect(flatten).not.toHaveBeenCalled()
    expect((api.upload.mock.calls[0]![1] as FormData).get('imageGeometry')).toBeNull()
    expect(wrapper.text()).not.toContain('Apply flat view')
  })

  it('auto-crops to AI-detected receipt corners and uploads that geometry with the untouched original', async () => {
    const detected = { version: 1, corners: [{ x: 0.2, y: 0.1 }, { x: 0.8, y: 0.12 }, { x: 0.78, y: 0.9 }, { x: 0.22, y: 0.88 }] }
    detect.mockResolvedValue(detected)
    api.upload.mockResolvedValue(receipt())
    const wrapper = await mountPage()
    await chooseFile(wrapper)

    expect(detect).toHaveBeenCalledWith(expect.any(File))
    expect(flatten).toHaveBeenCalledWith(expect.any(File), detected)
    const form = api.upload.mock.calls[0]![1] as FormData
    expect(JSON.parse(form.get('imageGeometry') as string)).toEqual(detected)
    expect((form.get('image') as File).size).toBe(3)
  })

  it('waits for detection before automatically uploading and prevents concurrent captures', async () => {
    let finish!: (value: unknown) => void
    detect.mockImplementation(() => new Promise(resolve => { finish = resolve }))
    api.upload.mockResolvedValue(receipt())
    const wrapper = await mountPage()
    await chooseFile(wrapper)
    expect(wrapper.text()).toContain('Scanning receipt')
    expect(api.upload).not.toHaveBeenCalled()
    expect(wrapper.findAll('button').find(button => button.text() === 'Take a photo')!.attributes('disabled')).toBeDefined()
    finish({ version: 1, corners: imageBoundary().corners })
    await flushPromises()
    expect(api.upload).toHaveBeenCalledOnce()
    expect(JSON.parse((api.upload.mock.calls[0]![1] as FormData).get('imageGeometry') as string)).toEqual(imageBoundary())
  })

  it('silently saves the original when automatic cropping is unavailable', async () => {
    api.upload.mockResolvedValue(receipt())
    detect.mockRejectedValue(new Error('Automatic receipt cropping is not configured.'))
    const wrapper = await mountPage()
    await chooseFile(wrapper)
    expect(flatten).not.toHaveBeenCalled()
    expect(api.upload).toHaveBeenCalledOnce()
    expect((api.upload.mock.calls[0]![1] as FormData).get('imageGeometry')).toBeNull()
    expect(document.querySelector('.v-overlay--active')).toBeNull()
  })

  it('uploads the image as multipart form data and selects the new receipt', async () => {
    api.upload.mockResolvedValue(receipt({ id: 5 }))
    const wrapper = await mountPage()
    await chooseFile(wrapper)

    expect(api.upload).toHaveBeenCalledOnce()
    const [url, form] = api.upload.mock.calls[0] as [string, FormData]
    expect(url).toBe('/api/receipts')
    const image = form.get('image') as File
    expect(image.name).toBe('photo.heic.jpg')
    expect(image.type).toBe('image/jpeg')
    expect(image.size).toBe(3)
    expect(form.get('imageGeometry')).toBeNull()
    expect(wrapper.text()).not.toContain('Save corrections')
    expect(wrapper.text()).toContain('Corner Cafe')
    expect(useSnackbarStore().messages.slice(-1)[0]).toMatchObject({
      text: 'Receipt saved',
      variant: 'success',
    })
    expect(wrapper.find('.receipt-capture input:not([type="file"])').exists()).toBe(false)
  })

  it('selects the new upload from Categorized and retains drafts when switching back', async () => {
    api.get.mockResolvedValue([
      receipt({ id: 2, merchantName: 'Categorized store', isLinked: true }),
      receipt(),
    ])
    api.upload.mockResolvedValue(receipt({ id: 5, merchantName: 'New upload' }))
    const wrapper = await mountPage()
    await wrapper.findAll('button').find(button => button.text() === 'Categorized (1)')!.trigger('click')
    await editReceipt(wrapper)
    await wrapper.get('input[maxlength="300"]').setValue('Categorized correction')
    await chooseFile(wrapper)
    expect(wrapper.get('button[aria-pressed="true"]').text()).toBe('Pending (2)')
    await editReceipt(wrapper)
    expect((wrapper.get('input[maxlength="300"]').element as HTMLInputElement).value).toBe('New upload')
    expect(api.download).toHaveBeenLastCalledWith('/api/receipts/5/image')
    await wrapper.get('input[maxlength="300"]').setValue('New correction')
    await wrapper.findAll('button').find(button => button.text() === 'Categorized (1)')!.trigger('click')
    await editReceipt(wrapper)
    expect((wrapper.get('input[maxlength="300"]').element as HTMLInputElement).value).toBe('Categorized correction')
    await wrapper.findAll('button').find(button => button.text() === 'Pending (2)')!.trigger('click')
    await editReceipt(wrapper)
    api.get.mockResolvedValue([receipt({ id: 5, merchantName: 'New upload' }), receipt()])
    await wrapper.get('button[aria-label="Refresh receipts"]').trigger('click')
    await flushPromises()
    expect((wrapper.get('input[maxlength="300"]').element as HTMLInputElement).value).toBe('New correction')
  })

  it('asks for only missing details when extraction could not run', async () => {
    api.upload.mockResolvedValue(receipt({ transactionDate: null, totalAmountCents: null, processingMessage: 'Receipt extraction is not configured.' }))
    const wrapper = await mountPage()
    await chooseFile(wrapper)

    expect(document.querySelector('.v-overlay--active')?.textContent).toContain('Your photo is saved')
    expect(document.querySelector('.v-overlay--active input[type="date"]')).not.toBeNull()
    expect(document.querySelector('.v-overlay--active input[type="number"]')).not.toBeNull()
    expect(document.querySelector('.v-overlay--active')?.textContent).not.toContain('Merchant')
  })

  it.each([
    { transactionDate: null, totalAmountCents: 1250, dateMissing: true, totalMissing: false },
    { transactionDate: '2026-03-04T00:00:00', totalAmountCents: null, dateMissing: false, totalMissing: true },
  ])('asks only for the unrecognized field: $dateMissing / $totalMissing', async ({ transactionDate, totalAmountCents, dateMissing, totalMissing }) => {
    api.upload.mockResolvedValue(receipt({ transactionDate, totalAmountCents }))
    const wrapper = await mountPage()
    await chooseFile(wrapper)
    expect(Boolean(document.querySelector('.v-overlay--active input[type="date"]'))).toBe(dateMissing)
    expect(Boolean(document.querySelector('.v-overlay--active input[type="number"]'))).toBe(totalMissing)
    await clickDialogButton('Later')
    expect(document.querySelector('.v-overlay--active')).toBeNull()
    expect(wrapper.find('[data-receipt-id="1"]').exists()).toBe(true)
    expect(api.put).not.toHaveBeenCalled()
  })

  it('persists missing details, preserves extraction, and retries a failed save without reuploading', async () => {
    api.upload.mockResolvedValue(receipt({ totalAmountCents: null, notes: 'Keep note' }))
    api.put.mockRejectedValueOnce(new Error('Could not save details'))
      .mockResolvedValue(receipt({ totalAmountCents: 1575, notes: 'Keep note' }))
    const wrapper = await mountPage()
    await chooseFile(wrapper)
    await clickDialogButton('Save')
    expect(document.querySelector('.v-overlay--active')?.textContent).toContain('positive total')
    expect(api.put).not.toHaveBeenCalled()
    const amount = document.querySelector<HTMLInputElement>('.v-overlay--active input[type="number"]')!
    amount.value = '15.75'
    amount.dispatchEvent(new Event('input', { bubbles: true }))
    await clickDialogButton('Save')
    expect(document.querySelector('.v-overlay--active')?.textContent).toContain('Could not save details')
    await clickDialogButton('Save')
    expect(api.put).toHaveBeenLastCalledWith('/api/receipts/1', {
      merchantName: 'Corner Cafe',
      transactionDate: '2026-03-04T00:00:00',
      totalAmountCents: 1575,
      notes: 'Keep note',
      lineItems: [{ description: 'Latte', amountCents: 450 }],
    })
    expect(api.upload).toHaveBeenCalledOnce()
    expect(document.querySelector('.v-overlay--active')).toBeNull()
    expect(wrapper.get('[data-receipt-id="1"]').text()).toContain('$15.75')
  })

  it('does not ask for merchant or optional line items when date and total were read', async () => {
    api.upload.mockResolvedValue(receipt({ merchantName: null, lineItems: [], processingMessage: 'Check optional details' }))
    const wrapper = await mountPage()
    await chooseFile(wrapper)
    expect(document.querySelector('.v-overlay--active')).toBeNull()
    expect(api.put).not.toHaveBeenCalled()
    expect(useSnackbarStore().messages.slice(-1)[0]?.text).toBe('Receipt saved')
  })

  it('falls back to the untouched original if flattening the detected corners fails', async () => {
    detect.mockResolvedValue(imageBoundary())
    flatten.mockRejectedValue(new Error('Could not flatten'))
    api.upload.mockResolvedValue(receipt())
    const wrapper = await mountPage()
    await chooseFile(wrapper)
    expect(api.upload).toHaveBeenCalledOnce()
    const form = api.upload.mock.calls[0]![1] as FormData
    expect(form.get('imageGeometry')).toBeNull()
    expect((form.get('image') as File).size).toBe(3)
    expect(document.querySelector('.v-overlay--active')).toBeNull()
  })

  it('automatically saves successive camera captures without additional actions', async () => {
    api.upload.mockResolvedValueOnce(receipt({ id: 7 })).mockResolvedValueOnce(receipt({ id: 8 }))
    const wrapper = await mountPage()
    const camera = wrapper.get('input[capture="environment"]')
    for (const name of ['one.jpg', 'two.jpg']) {
      Object.defineProperty(camera.element, 'files', { value: [new File(['raw'], name, { type: 'image/jpeg' })], configurable: true })
      await camera.trigger('change')
      await flushPromises()
    }
    expect(api.upload).toHaveBeenCalledTimes(2)
    expect(wrapper.find('[data-receipt-id="7"]').exists()).toBe(true)
    expect(wrapper.find('[data-receipt-id="8"]').exists()).toBe(true)
    expect(document.querySelector('.v-overlay--active')).toBeNull()
  })

  it('reveals extra controls only on edit and persists manually adjusted corners', async () => {
    api.get.mockResolvedValue([receipt({ imageGeometry: imageBoundary() })])
    api.put.mockResolvedValue(receipt({ imageGeometry: imageBoundary() }))
    const wrapper = await mountPage()
    expect(wrapper.text()).not.toContain('Open original receipt')
    expect(wrapper.text()).not.toContain('Save corrections')
    expect(wrapper.text()).not.toContain('Adjust image')
    await editReceipt(wrapper)
    expect(wrapper.text()).toContain('Open original receipt')
    await wrapper.findAll('button').find(button => button.text() === 'Adjust image')!.trigger('click')
    expect(wrapper.findAll('button').find(button => button.text() === 'Save corrections')!.attributes('disabled')).toBeDefined()
    await wrapper.get('input[aria-label="Top-left X percent"]').setValue('5')
    await wrapper.get('input[aria-label="Top-left Y percent"]').setValue('5')
    await wrapper.findAll('button').find(button => button.text() === 'Apply flat view')!.trigger('click')
    await flushPromises()
    await wrapper.findAll('button').find(button => button.text() === 'Save corrections')!.trigger('click')
    await flushPromises()
    expect(api.put).toHaveBeenCalledWith('/api/receipts/1', expect.objectContaining({
      updateImageGeometry: true,
      imageGeometry: { version: 1, corners: [{ x: 0.05, y: 0.05 }, { x: 1, y: 0 }, { x: 1, y: 1 }, { x: 0, y: 1 }] },
    }))
    expect(detect).not.toHaveBeenCalled()
  })

  it('shows the server error and allows retrying when the upload fails', async () => {
    api.upload.mockRejectedValue(new Error('Receipt storage is not configured.'))
    const wrapper = await mountPage()
    await chooseFile(wrapper)

    expect(useSnackbarStore().messages.slice(-1)[0]).toMatchObject({
      text: 'Receipt storage is not configured.',
      variant: 'error',
    })
    api.upload.mockResolvedValue(receipt({ id: 5 }))
    await wrapper.findAll('button').find(button => button.text() === 'Retry upload')!.trigger('click')
    await flushPromises()
    expect(api.upload).toHaveBeenCalledTimes(2)
    expect(wrapper.find('[data-receipt-id="5"]').exists()).toBe(true)
  })

  it('loads saved receipts, shows the first one, and saves corrections', async () => {
    api.get.mockResolvedValue([receipt()])
    api.put.mockResolvedValue(receipt({ merchantName: 'Fixed Cafe' }))
    const wrapper = await mountPage()

    expect(wrapper.text()).toContain('Corner Cafe')
    expect(api.download).toHaveBeenCalledWith('/api/receipts/1/image')

    await editReceipt(wrapper)
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

  it('defaults to unattached receipts and keeps categorized receipts secondary', async () => {
    api.get.mockResolvedValue([receipt({ id: 2, merchantName: 'Archived store', isLinked: true }), receipt()])
    const wrapper = await mountPage()
    await editReceipt(wrapper)
    expect(wrapper.text()).toContain('Pending (1)')
    expect(wrapper.text()).toContain('Categorized (1)')
    expect(wrapper.text()).not.toContain('Archived store')
    await wrapper.findAll('button').find(button => button.text() === 'Categorized (1)')!.trigger('click')
    await flushPromises()
    expect(wrapper.text()).toContain('Archived store')
    expect(wrapper.text()).toContain('Attached to expense')
  })

  it('retains unsaved corrections across refreshes and receipt switches', async () => {
    api.get.mockResolvedValue([
      receipt(),
      receipt({ id: 2, merchantName: 'Other store', uploadedAtUtc: '2026-03-03T10:00:00Z' }),
    ])
    const wrapper = await mountPage()
    await editReceipt(wrapper)
    await wrapper.findAll('input').find(input => (input.element as HTMLInputElement).value === 'Corner Cafe')!.setValue('My correction')
    await wrapper.get('button[aria-label="Refresh receipts"]').trigger('click')
    await flushPromises()
    expect(wrapper.findAll('input').some(input => (input.element as HTMLInputElement).value === 'My correction')).toBe(true)
    await wrapper.findAll('.v-list-item').find(item => item.text().includes('Other store'))!.trigger('click')
    await wrapper.findAll('.v-list-item').find(item => item.text().includes('Corner Cafe'))!.trigger('click')
    await editReceipt(wrapper)
    expect(wrapper.findAll('input').some(input => (input.element as HTMLInputElement).value === 'My correction')).toBe(true)
  })

  it('keeps capture minimal while analysis is running', async () => {
    let finish!: (value: ReceiptDto) => void
    api.upload.mockImplementation(() => new Promise<ReceiptDto>(resolve => { finish = resolve }))
    const wrapper = await mountPage()
    await chooseFile(wrapper)
    expect(wrapper.text()).toContain('Saving and reading receipt')
    expect(wrapper.find('.receipt-capture input:not([type="file"])').exists()).toBe(false)
    expect(wrapper.text()).not.toContain('Adjust corners')
    finish(receipt())
    await flushPromises()
    expect(api.put).not.toHaveBeenCalled()
    expect(document.querySelector('.v-overlay--active')).toBeNull()
  })

  it('rejects unsupported files before image processing', async () => {
    const wrapper = await mountPage()
    await chooseFile(wrapper, new File(['pdf'], 'receipt.pdf', { type: 'application/pdf' }))
    expect(flatten).not.toHaveBeenCalled()
    expect(api.upload).not.toHaveBeenCalled()
    expect(useSnackbarStore().messages.slice(-1)[0]?.text).toContain('JPEG, PNG, or WebP')
  })

  it('resets manual corners, rotates via corner order, and drops applied geometry when skipping', async () => {
    const wrapper = mount(ReceiptImageAdjustment, {
      props: { file: new File(['raw'], 'photo.jpg'), originalUrl: 'blob:preview' },
      global: { plugins: [createVuetify({ components, directives })] },
    })
    await flushPromises()
    const button = (text: string) => wrapper.findAll('button').find(item => item.text() === text)!
    await button('Adjust corners').trigger('click')
    await wrapper.get('input[aria-label="Top-left X percent"]').setValue('15')
    await button('Reset corners').trigger('click')
    expect((wrapper.get('input[aria-label="Top-left X percent"]').element as HTMLInputElement).value).toBe('0')
    await button('Rotate flat view 90° clockwise').trigger('click')
    await button('Apply flat view').trigger('click')
    await flushPromises()
    const [a, b, c, d] = imageBoundary().corners
    expect(flatten).toHaveBeenCalledWith(expect.any(File), { version: 1, corners: [d, a, b, c] })
    await button('Skip — use original').trigger('click')
    expect(wrapper.find('img[alt="Flattened receipt preview"]').exists()).toBe(false)
    expect(wrapper.emitted('geometry')?.slice(-1)[0]).toEqual([null])
  })

  it('discards a late adjustment after choosing a different original and on unmount', async () => {
    let finish!: (value: Blob) => void
    flatten.mockImplementationOnce(() => new Promise<Blob>(resolve => { finish = resolve }))
    const wrapper = mount(ReceiptImageAdjustment, {
      props: { file: new File(['one'], 'one.jpg'), originalUrl: 'blob:one' },
      global: { plugins: [createVuetify({ components, directives })] },
    })
    await wrapper.findAll('button').find(button => button.text() === 'Adjust corners')!.trigger('click')
    await wrapper.findAll('button').find(button => button.text() === 'Apply flat view')!.trigger('click')
    await wrapper.setProps({ file: new File(['two'], 'two.jpg'), originalUrl: 'blob:two' })
    finish(new Blob(['stale']))
    await flushPromises()
    expect(URL.createObjectURL).not.toHaveBeenCalled()
    expect(wrapper.get('img').attributes('src')).toBe('blob:two')
    expect(wrapper.emitted('geometry')?.slice(-1)[0]).toEqual([null])
    flatten.mockImplementationOnce(() => new Promise<Blob>(resolve => { finish = resolve }))
    await wrapper.findAll('button').find(button => button.text() === 'Adjust corners')!.trigger('click')
    await wrapper.findAll('button').find(button => button.text() === 'Apply flat view')!.trigger('click')
    wrapper.unmount()
    finish(new Blob(['stale unmounted']))
    await flushPromises()
    expect(URL.createObjectURL).not.toHaveBeenCalled()
  })

  it('confirms deletion and keeps the receipt and explicit error when the API rejects it', async () => {
    api.get.mockResolvedValue([receipt()])
    api.delete.mockRejectedValueOnce(new Error('Receipt storage unavailable'))
    const wrapper = await mountPage()
    await editReceipt(wrapper)
    await wrapper.findAll('button').find(button => button.text() === 'Delete receipt')!.trigger('click')
    await flushPromises()
    const confirm = [...document.querySelectorAll('button')].find(button => button.textContent?.trim() === 'Delete receipt' && button.closest('.v-overlay'))!
    expect(api.delete).not.toHaveBeenCalled()
    confirm.click()
    await flushPromises()
    expect(api.delete).toHaveBeenCalledWith('/api/receipts/1')
    expect(document.body.textContent).toContain('Receipt storage unavailable')
    expect(wrapper.text()).toContain('Corner Cafe')
    confirm.click()
    await flushPromises()
    expect(wrapper.text()).toContain('No pending receipts.')
  })

  it('confirms categorized deletion keeps expense data and removes the receipt only after success', async () => {
    api.get.mockResolvedValue([receipt({ isLinked: true })])
    api.delete.mockRejectedValueOnce(new Error('Still processing; try again'))
    const wrapper = await mountPage()
    await wrapper.findAll('button').find(button => button.text() === 'Categorized (1)')!.trigger('click')
    await flushPromises()
    await editReceipt(wrapper)
    await wrapper.findAll('button').find(button => button.text() === 'Delete receipt')!.trigger('click')
    await flushPromises()
    expect(document.body.textContent).toContain('Permanently delete Corner Cafe and its original image?')
    expect(document.body.textContent).toContain('Expense data, including all amounts, categories, and descriptions, will be kept.')
    const confirm = [...document.querySelectorAll('button')].find(button => button.textContent?.trim() === 'Delete receipt' && button.closest('.v-overlay'))!
    expect(confirm.disabled).toBe(false)
    expect(api.delete).not.toHaveBeenCalled()
    confirm.click()
    await flushPromises()
    expect(document.body.textContent).toContain('Still processing; try again')
    expect(wrapper.text()).toContain('Categorized (1)')
    confirm.click()
    await flushPromises()
    expect(api.delete).toHaveBeenCalledWith('/api/receipts/1')
    expect(wrapper.text()).toContain('Categorized (0)')
    expect(wrapper.text()).toContain('No categorized receipts yet.')
    expect(document.querySelector('.v-overlay--active')).toBeNull()
  })

  async function conversionDialog(pendingOverrides: Partial<PendingExpenseDto> = {}) {
    const pending: PendingExpenseDto = {
      id: 10, version: 'version', description: 'Bank description', amount: 1250,
      date: '2026-03-04T00:00:00', isDebit: true, notes: null, assigneeId: null, assigneeName: null,
      suggestedCategoryId: 1, suggestedCategoryName: null, suggestedCategoryDescription: null, ...pendingOverrides,
    }
    const wrapper = mount(ConvertPendingExpenseDialog, {
      props: { modelValue: true, pendingExpense: pending, categories: [{ id: 1, name: 'Food' } as ExpenseCategoryDto] },
      global: { plugins: [createVuetify({ components, directives })] },
      attachTo: document.body,
    })
    await flushPromises()
    return wrapper
  }

  it('selects and removes one receipt without changing the editable expense fields', async () => {
    api.get.mockResolvedValue([receipt(), receipt(), receipt({ id: 2, isLinked: true })])
    const wrapper = await conversionDialog()
    const radios = wrapper.findAllComponents({ name: 'VRadio' })
    expect(radios).toHaveLength(2) // No receipt plus one unique unused receipt.
    const group = wrapper.getComponent({ name: 'VRadioGroup' })
    group.vm.$emit('update:modelValue', 1)
    await flushPromises()
    const descriptionInput = document.querySelector('input[value="Bank description"]')
    expect(descriptionInput).not.toBeNull()
    await wrapper.setProps({ pendingExpense: { ...wrapper.props('pendingExpense')!, notes: 'Saved note', version: 'new-version' } })
    expect((descriptionInput as HTMLInputElement).value).toBe('Bank description')
    group.vm.$emit('update:modelValue', null)
    await flushPromises()
    const apply = [...document.querySelectorAll('button')].find(button => button.textContent?.trim() === 'Apply')!
    apply.click()
    await flushPromises()
    expect(api.post).toHaveBeenCalledWith('/api/pending-expenses/10/convert', expect.objectContaining({
      description: 'Bank description', date: '2026-03-04', receiptId: null,
      version: 'new-version', items: [{ expenseCategoryId: 1, amount: 1250 }],
    }))
  })

  it('submits the selected receipt and discards it when the same expense is reopened', async () => {
    api.get.mockResolvedValue([receipt()])
    const wrapper = await conversionDialog()
    wrapper.getComponent({ name: 'VRadioGroup' }).vm.$emit('update:modelValue', 1)
    await flushPromises()
    ;[...document.querySelectorAll('button')].find(button => button.textContent?.trim() === 'Apply')!.click()
    await flushPromises()
    expect(api.post).toHaveBeenCalledWith('/api/pending-expenses/10/convert', expect.objectContaining({ receiptId: 1 }))
    await wrapper.setProps({ modelValue: false })
    api.get.mockResolvedValue([])
    await wrapper.setProps({ modelValue: true })
    await flushPromises()
    expect(wrapper.getComponent({ name: 'VRadioGroup' }).props('modelValue')).toBe(null)
  })

  it('browses incomplete pending receipts, previews, unselects and submits without changing expense fields', async () => {
    api.get.mockImplementation(async (url: string) => url === '/api/receipts'
      ? [receipt(), receipt({ id: 7, merchantName: 'Incomplete receipt', totalAmountCents: null, transactionDate: null }),
        receipt({ id: 7, merchantName: 'Incomplete receipt', totalAmountCents: null, transactionDate: null }),
        receipt({ id: 8, merchantName: 'Already attached', isLinked: true })]
      : [receipt()])
    const wrapper = await conversionDialog()
    await wrapper.findAllComponents({ name: 'VTextField' }).find(field => field.props('label') === 'Description')!.get('input').setValue('My expense')
    await wrapper.findAllComponents({ name: 'VTextField' }).find(field => field.props('label') === 'Date')!.get('input').setValue('2026-03-08')
    await clickDialogButton('Browse all pending receipts')
    expect(api.get).toHaveBeenCalledWith('/api/receipts')
    expect(document.querySelectorAll('.v-overlay--active input[type="radio"]')).toHaveLength(3)
    expect(document.body.textContent).toContain('Incomplete receipt · Date not set · Total not set')
    expect(document.body.textContent).not.toContain('Already attached')
    await selectReceipt('7')
    await clickDialogButton('View receipt')
    expect(api.download).toHaveBeenCalledWith('/api/receipts/7/image')
    await selectReceipt('1')
    expect([...document.querySelectorAll<HTMLInputElement>('.v-overlay--active input[type="radio"]:checked')].map(input => input.value)).toEqual(['1'])
    expect(document.body.textContent).not.toContain('Hide receipt')
    document.querySelector<HTMLInputElement>('.v-overlay--active input[type="radio"]')!.click()
    await flushPromises()
    expect(wrapper.getComponent({ name: 'VRadioGroup' }).props('modelValue')).toBe(null)
    await selectReceipt('7')
    await clickDialogButton('Apply')
    expect(api.post).toHaveBeenCalledWith('/api/pending-expenses/10/convert', expect.objectContaining({
      description: 'My expense', date: '2026-03-08', receiptId: 7,
      items: [{ expenseCategoryId: 1, amount: 1250 }],
    }))
  })

  it('keeps a manual selection and preview when suggested matches arrive late', async () => {
    let finish!: (value: ReceiptDto[]) => void
    api.get.mockImplementation((url: string) => url === '/api/receipts'
      ? Promise.resolve([receipt({ id: 7, merchantName: 'Manual choice', totalAmountCents: null })])
      : new Promise<ReceiptDto[]>(resolve => { finish = resolve }))
    await conversionDialog()
    await toggleReceipts()
    await clickDialogButton('Browse all pending receipts')
    await selectReceipt('7')
    await clickDialogButton('View receipt')
    finish([receipt()])
    await flushPromises()
    expect(dialogRadio('7').checked).toBe(true)
    expect(document.body.textContent).toContain('Hide receipt')
    await clickDialogButton('Refresh matches')
    finish([receipt({ id: 7, merchantName: 'Manual choice' }), receipt()])
    await flushPromises()
    expect(document.querySelectorAll('.v-overlay--active input[type="radio"][value="7"]')).toHaveLength(1)
    expect(dialogRadio('7').checked).toBe(true)
    await clickDialogButton('Apply')
    expect(api.post).toHaveBeenCalledWith('/api/pending-expenses/10/convert', expect.objectContaining({ receiptId: 7 }))
  })

  it.each(['switch', 'close'] as const)('ignores late browse responses after dialog %s', async action => {
    let finish!: (value: ReceiptDto[]) => void
    api.get.mockImplementation((url: string) => url === '/api/receipts'
      ? new Promise<ReceiptDto[]>(resolve => { finish = resolve })
      : Promise.resolve([receipt(), receipt({ id: 2 })]))
    const wrapper = await conversionDialog()
    await clickDialogButton('Browse all pending receipts')
    if (action === 'switch') {
      await wrapper.setProps({ pendingExpense: { ...wrapper.props('pendingExpense')!, id: 11 } })
    } else {
      await clickDialogButton('Cancel')
      await wrapper.setProps({ modelValue: false })
      await wrapper.setProps({ modelValue: true })
    }
    finish([receipt({ id: 7, merchantName: 'Stale browse result' })])
    await flushPromises()
    expect(document.body.textContent).not.toContain('Stale browse result')
    expect(document.body.textContent).toContain('Browse all pending receipts')
    expect(dialogRadio('7')).toBeNull()
    await clickDialogButton('Apply')
    expect(api.post).toHaveBeenCalledWith(`/api/pending-expenses/${action === 'switch' ? 11 : 10}/convert`, expect.objectContaining({ receiptId: null }))
  })

  it.each(['switch', 'close'] as const)('ignores late match results after dialog %s', async action => {
    let finish!: (value: ReceiptDto[]) => void
    api.get.mockImplementationOnce(() => new Promise<ReceiptDto[]>(resolve => { finish = resolve }))
    const wrapper = await conversionDialog()
    api.get.mockResolvedValue([receipt({ id: 9, merchantName: 'New match' })])
    if (action === 'switch') {
      await wrapper.setProps({ pendingExpense: { ...wrapper.props('pendingExpense')!, id: 11 } })
    } else {
      await clickDialogButton('Cancel')
      await wrapper.setProps({ modelValue: false })
      await wrapper.setProps({ modelValue: true })
    }
    await flushPromises()
    finish([receipt({ merchantName: 'Stale match' })])
    await flushPromises()
    expect(document.body.textContent).toContain('New match')
    expect(document.body.textContent).not.toContain('Stale match')
  })

  it('collapses receipts without matches and keeps browsing available on expansion', async () => {
    await conversionDialog()
    expect(receiptToggle().getAttribute('aria-expanded')).toBe('false')
    expect(receiptToggle().textContent).toContain('Receipt (optional)')
    expect(receiptToggle().textContent).toContain('No matches')
    expect(document.body.textContent).not.toContain('No matching pending receipts')
    expect(document.querySelector('.v-overlay--active input[type="radio"]')).toBeNull()
    await toggleReceipts()
    expect(receiptToggle().getAttribute('aria-expanded')).toBe('true')
    expect(document.body.textContent).toContain('No matching pending receipts')
    api.get.mockResolvedValue([receipt({ id: 7, merchantName: 'Manual receipt', totalAmountCents: null })])
    await clickDialogButton('Browse all pending receipts')
    await selectReceipt('7')
    await toggleReceipts()
    expect(receiptToggle().getAttribute('aria-expanded')).toBe('false')
    expect(receiptToggle().textContent).toContain('Manual receipt')
    await clickDialogButton('Apply')
    expect(api.post).toHaveBeenCalledWith('/api/pending-expenses/10/convert', expect.objectContaining({ receiptId: 7 }))
  })

  it('automatically selects the closest matching receipt and submits it', async () => {
    api.get.mockResolvedValue([
      receipt({ id: 3, merchantName: 'Two days off', transactionDate: '2026-03-06T00:00:00' }),
      receipt({ id: 4, merchantName: 'Same day', transactionDate: '2026-03-04T00:00:00' }),
    ])
    await conversionDialog()
    expect(dialogRadio('4').checked).toBe(true)
    expect(receiptToggle().textContent).toContain('Same day')
    await clickDialogButton('Apply')
    expect(api.post).toHaveBeenCalledWith('/api/pending-expenses/10/convert', expect.objectContaining({ receiptId: 4 }))
  })

  it('does not guess between equally close matches', async () => {
    api.get.mockResolvedValue([
      receipt({ id: 3, transactionDate: '2026-03-03T00:00:00' }),
      receipt({ id: 4, transactionDate: '2026-03-05T00:00:00' }),
    ])
    await conversionDialog()
    expect(receiptToggle().textContent).toContain('2 suggested matches')
    await clickDialogButton('Apply')
    expect(api.post).toHaveBeenCalledWith('/api/pending-expenses/10/convert', expect.objectContaining({ receiptId: null }))
  })

  it('keeps an explicit "No receipt" choice when matches refresh', async () => {
    api.get.mockResolvedValue([receipt()])
    const wrapper = await conversionDialog()
    expect(dialogRadio('1').checked).toBe(true)
    wrapper.getComponent({ name: 'VRadioGroup' }).vm.$emit('update:modelValue', null)
    await flushPromises()
    await clickDialogButton('Refresh matches')
    expect(dialogRadio('1').checked).toBe(false)
    await clickDialogButton('Apply')
    expect(api.post).toHaveBeenCalledWith('/api/pending-expenses/10/convert', expect.objectContaining({ receiptId: null }))
  })

  it('opens matching receipts by default but preserves a manual collapse during refresh', async () => {
    api.get.mockResolvedValue([receipt(), receipt({ id: 2 })])
    const wrapper = await conversionDialog()
    expect(receiptToggle().getAttribute('aria-expanded')).toBe('true')
    expect(receiptToggle().textContent).toContain('2 suggested matches')
    let finish!: (value: ReceiptDto[]) => void
    api.get.mockImplementationOnce(() => new Promise<ReceiptDto[]>(resolve => { finish = resolve }))
    await clickDialogButton('Refresh matches')
    await toggleReceipts()
    expect(receiptToggle().getAttribute('aria-expanded')).toBe('false')
    finish([receipt(), receipt({ id: 2 }), receipt({ id: 3 })])
    await flushPromises()
    expect(receiptToggle().getAttribute('aria-expanded')).toBe('false')
    expect(receiptToggle().textContent).toContain('3 suggested matches')
    await wrapper.setProps({ modelValue: false })
    api.get.mockResolvedValue([])
    await wrapper.setProps({ modelValue: true })
    await flushPromises()
    expect(receiptToggle().getAttribute('aria-expanded')).toBe('false')
    expect(receiptToggle().textContent).toContain('No matches')
  })

  it('keeps an explicitly opened empty section open when matches refresh', async () => {
    await conversionDialog()
    await toggleReceipts()
    await clickDialogButton('Refresh matches')
    expect(receiptToggle().getAttribute('aria-expanded')).toBe('true')
    expect(document.body.textContent).toContain('No matching pending receipts')
  })

  it('opens receipt lookup failures with a retry and allows conversion without a receipt', async () => {
    api.get.mockRejectedValueOnce(new Error('Matching unavailable'))
    await conversionDialog()
    expect(receiptToggle().getAttribute('aria-expanded')).toBe('true')
    expect(document.body.textContent).toContain('Matching unavailable')
    await clickDialogButton('Retry matches')
    expect(receiptToggle().getAttribute('aria-expanded')).toBe('false')
    expect(receiptToggle().textContent).toContain('No matches')
    await clickDialogButton('Apply')
    expect(api.post).toHaveBeenCalledWith('/api/pending-expenses/10/convert', expect.objectContaining({ receiptId: null }))
  })

  it('does not show receipt controls for income', async () => {
    await conversionDialog({ isDebit: false })
    expect(receiptToggle()).toBeNull()
    expect(api.get).not.toHaveBeenCalledWith(expect.stringContaining('/api/receipts'))
  })

  it('ignores stale preview downloads and supports PDFs with an original-document link', async () => {
    let finish!: (value: { blob: Blob }) => void
    api.download.mockImplementationOnce(() => new Promise<{ blob: Blob }>(resolve => { finish = resolve }))
    const wrapper = mount(ReceiptPreview, {
      props: { receiptId: 1 },
      global: { plugins: [createVuetify({ components, directives })] },
    })
    api.download.mockResolvedValue({ blob: new Blob(['pdf'], { type: 'application/pdf' }) })
    await wrapper.setProps({ receiptId: 2 })
    await flushPromises()
    finish({ blob: new Blob(['old image'], { type: 'image/jpeg' }) })
    await flushPromises()
    expect(URL.createObjectURL).toHaveBeenCalledOnce()
    expect(wrapper.find('iframe[title="Receipt PDF"]').exists()).toBe(true)
    expect(wrapper.get('a').text()).toContain('Open original receipt')
  })

  it('defaults shared previews to flat output, toggles original view, and keeps the original link and URL lifecycle', async () => {
    vi.mocked(URL.createObjectURL).mockReturnValueOnce('blob:original').mockReturnValueOnce('blob:flat')
    const geometry = imageBoundary()
    const wrapper = mount(ReceiptPreview, {
      props: { receiptId: 1, imageGeometry: geometry },
      global: { plugins: [createVuetify({ components, directives })] },
    })
    await flushPromises()
    expect(flatten).toHaveBeenCalledWith(expect.any(Blob), geometry)
    expect(wrapper.getComponent({ name: 'VImg' }).props('src')).toBe('blob:flat')
    expect(wrapper.get('a').attributes('href')).toBe('blob:original')
    await wrapper.findAll('button').find(button => button.text() === 'Show original')!.trigger('click')
    expect(wrapper.getComponent({ name: 'VImg' }).props('src')).toBe('blob:original')
    await wrapper.findAll('button').find(button => button.text() === 'Show flat view')!.trigger('click')
    expect(wrapper.getComponent({ name: 'VImg' }).props('src')).toBe('blob:flat')
    wrapper.unmount()
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:original')
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:flat')
  })

  it('ignores stale flattened results after a geometry change on the same receipt', async () => {
    let finish!: (value: Blob) => void
    flatten.mockImplementationOnce(() => new Promise<Blob>(resolve => { finish = resolve }))
    vi.mocked(URL.createObjectURL).mockReturnValueOnce('blob:first-original').mockReturnValueOnce('blob:second-original').mockReturnValueOnce('blob:second-flat')
    const wrapper = mount(ReceiptPreview, {
      props: { receiptId: 1, imageGeometry: imageBoundary() },
      global: { plugins: [createVuetify({ components, directives })] },
    })
    await flushPromises()
    const changed = imageBoundary()
    changed.corners[0] = { x: 0.1, y: 0.1 }
    await wrapper.setProps({ imageGeometry: changed })
    await flushPromises()
    finish(new Blob(['stale flat']))
    await flushPromises()
    expect(URL.createObjectURL).toHaveBeenCalledTimes(3)
    expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:first-original')
    expect(flatten).toHaveBeenLastCalledWith(expect.any(Blob), changed)
    expect(wrapper.getComponent({ name: 'VImg' }).props('src')).toBe('blob:second-flat')
  })

  it('falls back to the original when correction fails and never processes legacy receipts or PDFs', async () => {
    flatten.mockRejectedValueOnce(new Error('Decode failed'))
    const wrapper = mount(ReceiptPreview, {
      props: { receiptId: 1, imageGeometry: imageBoundary() },
      global: { plugins: [createVuetify({ components, directives })] },
    })
    await flushPromises()
    expect(wrapper.text()).toContain('Showing the original')
    expect(wrapper.getComponent({ name: 'VImg' }).props('alt')).toBe('Original: receipt')
    await wrapper.setProps({ imageGeometry: null })
    await flushPromises()
    expect(flatten).toHaveBeenCalledOnce()
    api.download.mockResolvedValue({ blob: new Blob(['pdf'], { type: 'application/pdf' }) })
    await wrapper.setProps({ receiptId: 2, imageGeometry: imageBoundary() })
    await flushPromises()
    expect(wrapper.find('iframe').exists()).toBe(true)
    expect(flatten).toHaveBeenCalledOnce()
  })
})
