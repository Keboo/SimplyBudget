<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { ReceiptDto, ReceiptUpdateRequest, ReceiptImageGeometry } from '@/types'
import { centsToDollars, dollarsToCents, formatCents } from '@/utils/currency'
import ReceiptPreview from '@/components/ReceiptPreview.vue'
import ReceiptImageAdjustment from '@/components/ReceiptImageAdjustment.vue'

interface EditableLine {
  description: string
  amount: string
}

const snackbar = useSnackbarStore()
const receiptInput = ref<HTMLInputElement | null>(null)
const cameraInput = ref<HTMLInputElement | null>(null)
const receipts = ref<ReceiptDto[]>([])
const selectedId = ref<number | null>(null)
const selectedReceipt = computed(() => receipts.value.find(receipt => receipt.id === selectedId.value) ?? null)
const imageFile = ref<File | null>(null)
const imagePreviewUrl = ref('')
const imageGeometry = ref<ReceiptImageGeometry | null>(null)
const view = ref('pending')
const visibleReceipts = computed(() => receipts.value.filter(receipt => Boolean(receipt.isLinked) === (view.value === 'categorized')))
const pendingCount = computed(() => receipts.value.filter(receipt => !receipt.isLinked).length)
const loadError = ref('')
const actionError = ref('')
const deleteOpen = ref(false)
const deleting = ref(false)
const draftReceiptId = ref<number | null>(null)
const drafts = new Map<number, ReceiptUpdateRequest>()
const uploadOverrides = ref({ merchant: '', date: '', total: '' })
const merchantName = ref('')
const transactionDate = ref('')
const total = ref('')
const notes = ref('')
const lineItems = ref<EditableLine[]>([])
const loadingReceipts = ref(false)
const preparingImage = ref(false)
const uploading = ref(false)
const saving = ref(false)

function releaseUrl(url: string) {
  if (url) URL.revokeObjectURL(url)
}

function chooseImage() {
  receiptInput.value?.click()
}

function takePhoto() {
  cameraInput.value?.click()
}

async function loadReceipts() {
  loadingReceipts.value = true
  loadError.value = ''
  try {
    receipts.value = await apiClient.get<ReceiptDto[]>('/api/receipts') ?? []
    if (!visibleReceipts.value.some(receipt => receipt.id === selectedId.value))
      selectedId.value = visibleReceipts.value[0]?.id ?? null
  } catch (error: unknown) {
    loadError.value = error instanceof Error ? error.message : 'Failed to load receipts'
    snackbar.enqueueSnackbar(error instanceof Error ? error.message : 'Failed to load receipts', { variant: 'error' })
  } finally {
    loadingReceipts.value = false
  }
}

function snapshot(): ReceiptUpdateRequest {
  return {
    merchantName: merchantName.value,
    transactionDate: transactionDate.value,
    totalAmountCents: total.value.trim() ? dollarsToCents(total.value) : null,
    notes: notes.value,
    lineItems: lineItems.value.map(line => ({ description: line.description, amountCents: dollarsToCents(line.amount) })),
  }
}

watch(selectedReceipt, receipt => {
  if (draftReceiptId.value !== null) drafts.set(draftReceiptId.value, snapshot())
  const draft = receipt ? drafts.get(receipt.id) : undefined
  merchantName.value = draft?.merchantName ?? receipt?.merchantName ?? ''
  transactionDate.value = (draft?.transactionDate ?? receipt?.transactionDate)?.split('T')[0] ?? ''
  const amount = draft ? draft.totalAmountCents : receipt?.totalAmountCents
  total.value = amount == null ? '' : centsToDollars(amount)
  notes.value = draft?.notes ?? receipt?.notes ?? ''
  lineItems.value = (draft?.lineItems ?? receipt?.lineItems ?? []).map(line => ({
    description: line.description,
    amount: centsToDollars(line.amountCents),
  }))
  draftReceiptId.value = receipt?.id ?? null
  actionError.value = ''
})

watch(view, () => { selectedId.value = visibleReceipts.value[0]?.id ?? null })

function selectImage(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  input.value = ''
  preparingImage.value = false
  releaseUrl(imagePreviewUrl.value)
  imagePreviewUrl.value = ''
  imageFile.value = null
  imageGeometry.value = null
  uploadOverrides.value = { merchant: '', date: '', total: '' }
  try {
    const types: Record<string, RegExp> = {
      'image/jpeg': /\.jpe?g$/i, 'image/png': /\.png$/i, 'image/webp': /\.webp$/i,
    }
    if (!types[file.type]?.test(file.name)) throw new Error('Choose a JPEG, PNG, or WebP image. PDFs are not supported for upload.')
    if (file.size === 0 || file.size > 15 * 1024 * 1024) throw new Error('Receipt images must be between 1 byte and 15 MB.')
    imageFile.value = file
    imagePreviewUrl.value = URL.createObjectURL(imageFile.value)
  } catch (error: unknown) {
    snackbar.enqueueSnackbar(error instanceof Error ? error.message : 'Could not read the receipt file', { variant: 'error' })
  }
}

async function uploadReceipt() {
  if (!imageFile.value || preparingImage.value || uploading.value) return
  uploading.value = true
  actionError.value = ''
  try {
    const formData = new FormData()
    formData.append('image', imageFile.value)
    if (imageGeometry.value) formData.append('imageGeometry', JSON.stringify(imageGeometry.value))
    const receipt = await apiClient.upload<ReceiptDto>('/api/receipts', formData)
    const overrides = uploadOverrides.value
    if (overrides.merchant || overrides.date || overrides.total) {
      drafts.set(receipt.id, {
        merchantName: overrides.merchant || receipt.merchantName,
        transactionDate: overrides.date || receipt.transactionDate,
        totalAmountCents: overrides.total ? dollarsToCents(overrides.total) : receipt.totalAmountCents,
        notes: receipt.notes,
        lineItems: receipt.lineItems,
      })
    }
    view.value = 'pending'
    receipts.value = [receipt, ...receipts.value.filter(existing => existing.id !== receipt.id)]
    selectedId.value = receipt.id
    imageFile.value = null
    imageGeometry.value = null
    releaseUrl(imagePreviewUrl.value)
    imagePreviewUrl.value = ''
    uploadOverrides.value = { merchant: '', date: '', total: '' }
    snackbar.enqueueSnackbar(
      receipt.processingMessage ?? 'Receipt uploaded. Review the extracted details.',
      { variant: receipt.processingMessage ? 'warning' : 'success' },
    )
  } catch (error: unknown) {
    actionError.value = error instanceof Error ? error.message : 'Failed to upload receipt'
    snackbar.enqueueSnackbar(error instanceof Error ? error.message : 'Failed to upload receipt', { variant: 'error' })
  } finally {
    uploading.value = false
  }
}

function addLineItem() {
  lineItems.value.push({ description: '', amount: '' })
}

function removeLineItem(index: number) {
  lineItems.value.splice(index, 1)
}

async function saveReceipt() {
  if (!selectedReceipt.value) return
  const id = selectedReceipt.value.id
  actionError.value = ''
  if ((total.value.trim() && (!Number.isFinite(Number(total.value)) || dollarsToCents(total.value) <= 0))
    || lineItems.value.some(line => !line.description.trim() || dollarsToCents(line.amount) <= 0)) {
    actionError.value = 'Enter a positive total and a description and positive amount for each line, or remove unused lines.'
    return
  }
  saving.value = true
  const submittedDraft = JSON.stringify(snapshot())
  try {
    const request: ReceiptUpdateRequest = {
      merchantName: merchantName.value.trim() || null,
      transactionDate: transactionDate.value || null,
      totalAmountCents: total.value.trim() ? dollarsToCents(total.value) : null,
      notes: notes.value.trim() || null,
      lineItems: lineItems.value
        .map(line => ({ description: line.description.trim(), amountCents: dollarsToCents(line.amount) }))
    }
    const updated = await apiClient.put<ReceiptDto>(`/api/receipts/${id}`, request)
    if (selectedId.value === id && JSON.stringify(snapshot()) !== submittedDraft)
      drafts.set(id, snapshot())
    else drafts.delete(id)
    if (selectedId.value === id) draftReceiptId.value = null
    const index = receipts.value.findIndex(receipt => receipt.id === updated.id)
    if (index >= 0) receipts.value[index] = updated
    snackbar.enqueueSnackbar('Receipt saved', { variant: 'success' })
  } catch (error: unknown) {
    actionError.value = error instanceof Error ? error.message : 'Failed to save receipt'
    snackbar.enqueueSnackbar(error instanceof Error ? error.message : 'Failed to save receipt', { variant: 'error' })
  } finally {
    saving.value = false
  }
}

async function deleteReceipt() {
  const receipt = selectedReceipt.value
  if (!receipt) return
  deleting.value = true
  actionError.value = ''
  try {
    await apiClient.delete(`/api/receipts/${receipt.id}`)
    drafts.delete(receipt.id)
    draftReceiptId.value = null
    receipts.value = receipts.value.filter(item => item.id !== receipt.id)
    selectedId.value = visibleReceipts.value[0]?.id ?? null
    deleteOpen.value = false
    snackbar.enqueueSnackbar('Receipt deleted', { variant: 'success' })
  } catch (error: unknown) {
    actionError.value = error instanceof Error ? error.message : 'Failed to delete receipt'
  } finally {
    deleting.value = false
  }
}

onMounted(() => void loadReceipts())
onBeforeUnmount(() => {
  releaseUrl(imagePreviewUrl.value)
})
</script>

<template>
  <div>
    <h5 class="text-h5 mb-4">Receipts</h5>

    <v-card class="pa-4 mb-4">
      <div class="text-subtitle-1 font-weight-medium mb-1">Add a receipt</div>
      <div class="text-body-2 text-medium-emphasis mb-3">
        Take a photo or choose a file. AI suggests the merchant, total, and date; you can always correct them.
        JPEG, PNG, or WebP · up to 15 MB. Originals are saved unchanged.
      </div>
      <div class="d-flex flex-wrap align-center ga-3">
        <v-btn
          prepend-icon="mdi-camera-plus"
          variant="outlined"
          :disabled="uploading"
          @click="takePhoto"
        >
          Take a photo
        </v-btn>
        <v-btn
          prepend-icon="mdi-image-multiple"
          variant="outlined"
          :disabled="uploading"
          @click="chooseImage"
        >
          Choose file
        </v-btn>
        <input
          ref="cameraInput"
          type="file"
          accept="image/jpeg,image/png,image/webp"
          capture="environment"
          class="d-none"
          @change="selectImage"
        >
        <input
          ref="receiptInput"
          type="file"
          accept="image/jpeg,image/png,image/webp"
          class="d-none"
          @change="selectImage"
        >
        <v-btn
          color="primary"
          prepend-icon="mdi-cloud-upload"
          :disabled="!imageFile || preparingImage || uploading"
          :loading="uploading"
          @click="uploadReceipt"
        >
          Upload receipt
        </v-btn>
      </div>
      <div v-if="imageFile || uploading" class="mt-4">
        <div class="text-body-2 mb-2" role="status">
          {{ uploading ? 'Uploading and analyzing… You can enter corrections below while AI works. Save them after upload finishes.' : 'Optional: enter known details now. These take priority over AI suggestions.' }}
        </div>
        <v-text-field v-model="uploadOverrides.merchant" label="Merchant override (optional)" maxlength="300" />
        <v-text-field v-model="uploadOverrides.date" label="Date override (optional)" type="date" />
        <v-text-field v-model="uploadOverrides.total" label="Total override (optional)" prefix="$" type="number" min="0.01" step="0.01" />
      </div>
      <ReceiptImageAdjustment
        v-if="imageFile && imagePreviewUrl"
        :file="imageFile"
        :original-url="imagePreviewUrl"
        :disabled="uploading"
        @geometry="imageGeometry = $event"
        @busy="preparingImage = $event"
      />
    </v-card>

    <v-alert v-if="loadError" type="error" variant="tonal" class="mb-4">
      {{ loadError }}
      <v-btn variant="text" @click="loadReceipts">Retry loading</v-btn>
    </v-alert>
    <v-alert v-if="actionError && !selectedReceipt && !deleteOpen" type="error" variant="tonal" class="mb-4">{{ actionError }}</v-alert>
    <div class="d-flex flex-wrap ga-2 mb-3" aria-label="Receipt views">
      <v-btn :variant="view === 'pending' ? 'tonal' : 'text'" :aria-pressed="view === 'pending'" @click="view = 'pending'">Pending ({{ pendingCount }})</v-btn>
      <v-btn :variant="view === 'categorized' ? 'tonal' : 'text'" :aria-pressed="view === 'categorized'" @click="view = 'categorized'">Categorized ({{ receipts.length - pendingCount }})</v-btn>
      <v-btn variant="text" icon="mdi-refresh" aria-label="Refresh receipts" :disabled="loadingReceipts || saving || deleting" @click="loadReceipts" />
    </div>
    <p class="text-body-2 text-medium-emphasis mb-4">
      {{ view === 'pending' ? 'Unattached receipts, ready to match when you process pending expenses.' : 'Receipts attached to expenses. You can also review them from Expenses.' }}
    </p>
    <div class="receipt-layout">
      <v-card class="pa-2">
        <div class="text-subtitle-2 px-3 pt-2">{{ view === 'pending' ? 'Pending receipts' : 'Categorized receipts' }}</div>
        <div v-if="loadingReceipts" class="d-flex justify-center pa-6">
          <v-progress-circular indeterminate aria-label="Loading receipts" />
        </div>
        <v-list v-else-if="visibleReceipts.length > 0" class="receipt-list">
          <v-list-item
            v-for="receipt in visibleReceipts"
            :key="receipt.id"
            :active="receipt.id === selectedId"
            :disabled="saving || deleting"
            :title="receipt.merchantName || receipt.fileName"
            :subtitle="`${receipt.transactionDate ? new Date(receipt.transactionDate).toLocaleDateString() : 'Date not set'} · ${receipt.totalAmountCents == null ? 'Total not set' : formatCents(receipt.totalAmountCents)}`"
            prepend-icon="mdi-receipt-text"
            @click="selectedId = receipt.id"
          />
        </v-list>
        <div v-else-if="!loadError" class="text-body-2 text-medium-emphasis pa-4">
          {{ view === 'pending' ? 'No pending receipts. Upload a receipt to get started, or find attached receipts in Categorized.' : 'No categorized receipts yet. Attach a receipt when processing a pending expense.' }}
        </div>
      </v-card>

      <v-card v-if="selectedReceipt" class="pa-4">
        <div class="d-flex flex-wrap justify-space-between align-center ga-2 mb-2">
          <div class="text-h6">Review receipt</div>
          <v-chip size="small" variant="outlined">{{ selectedReceipt.isLinked ? 'Attached to expense' : selectedReceipt.processingStatus === 'reviewed' ? 'Reviewed · ready to attach' : selectedReceipt.processingStatus === 'processing' ? 'AI analyzing' : 'Check AI details' }}</v-chip>
        </div>
        <v-alert
          v-if="selectedReceipt.processingMessage"
          type="warning"
          variant="tonal"
          density="compact"
          class="mb-3"
        >
          {{ selectedReceipt.processingMessage }}
        </v-alert>
        <ReceiptPreview :receipt-id="selectedReceipt.id" :image-geometry="selectedReceipt.imageGeometry" :title="`Receipt from ${selectedReceipt.merchantName || selectedReceipt.fileName}`" class="mb-4" />
        <v-alert v-if="selectedReceipt.processingStatus === 'processing'" type="info" variant="tonal" class="mb-3">
          AI is still analyzing. You can enter and save corrections now; saved corrections take priority over AI.
        </v-alert>
        <div class="text-body-2 mb-3">Review AI suggestions below. Corrections are saved only when you select Save corrections. Drafts stay here while switching receipts.</div>
        <v-alert v-if="actionError && !deleteOpen" type="error" variant="tonal" class="mb-3">{{ actionError }}</v-alert>
        <div class="d-flex flex-column ga-2">
          <v-text-field v-model="merchantName" label="Location / merchant" maxlength="300" hide-details />
          <v-text-field v-model="transactionDate" label="Date" type="date" hide-details />
          <v-text-field v-model="total" label="Total amount" prefix="$" type="number" min="0" step="0.01" hide-details />
          <div class="d-flex align-center justify-space-between mt-2">
            <span class="text-subtitle-2">Line items (optional)</span>
            <v-btn size="small" variant="text" prepend-icon="mdi-plus" @click="addLineItem">Add line</v-btn>
          </div>
          <div v-for="(line, index) in lineItems" :key="index" class="receipt-line">
            <v-text-field v-model="line.description" label="Description" maxlength="500" density="compact" hide-details />
            <v-text-field
              v-model="line.amount"
              label="Amount"
              prefix="$"
              type="number"
              min="0"
              step="0.01"
              density="compact"
              hide-details
              style="max-width: 150px;"
            />
            <v-btn
              icon="mdi-delete"
              size="small"
              variant="text"
              color="error"
              :aria-label="`Remove line ${index + 1}`"
              @click="removeLineItem(index)"
            />
          </div>
          <v-textarea v-model="notes" label="Custom note" maxlength="10000" rows="2" auto-grow />
          <div class="d-flex flex-wrap justify-space-between ga-2">
            <v-btn color="error" variant="text" :disabled="saving || deleting" @click="actionError = ''; deleteOpen = true">Delete receipt</v-btn>
            <v-btn color="primary" :loading="saving" @click="saveReceipt">Save corrections</v-btn>
          </div>
          <v-dialog v-model="deleteOpen" max-width="460" :persistent="deleting">
            <v-card>
              <v-card-title class="text-wrap">Delete receipt?</v-card-title>
              <v-card-text>
                <p v-if="selectedReceipt?.isLinked">Permanently delete {{ selectedReceipt.merchantName || selectedReceipt.fileName }} and its original image? Expense data, including all amounts, categories, and descriptions, will be kept. Only the receipt and its attachment links will be removed. This cannot be undone.</p>
                <p v-else>Delete {{ selectedReceipt?.merchantName || selectedReceipt?.fileName }} and its original image permanently? This cannot be undone.</p>
                <v-alert v-if="actionError" type="error" variant="tonal" class="mt-3">{{ actionError }}</v-alert>
              </v-card-text>
              <v-card-actions class="flex-wrap">
                <v-spacer />
                <v-btn :disabled="deleting" @click="deleteOpen = false">Cancel</v-btn>
                <v-btn color="error" :loading="deleting" @click="deleteReceipt">Delete receipt</v-btn>
              </v-card-actions>
            </v-card>
          </v-dialog>
        </div>
      </v-card>
    </div>
  </div>
</template>

<style scoped>
.receipt-layout {
  display: grid;
  grid-template-columns: minmax(240px, 0.8fr) minmax(0, 1.5fr);
  gap: 16px;
}
.receipt-layout > * { min-width: 0; }
.receipt-list { max-height: 50vh; overflow-y: auto; }
.receipt-line { display: grid; grid-template-columns: minmax(0, 1fr) minmax(85px, 110px) 44px; gap: 8px; align-items: center; }
.receipt-line :deep(.v-btn) { min-width: 44px; min-height: 44px; }
@media (max-width: 450px) {
  .receipt-line { grid-template-columns: minmax(0, 1fr) 44px; }
  .receipt-line > :first-child { grid-column: 1 / -1; }
}

@media (max-width: 800px) {
  .receipt-layout {
    grid-template-columns: 1fr;
  }
}
</style>
