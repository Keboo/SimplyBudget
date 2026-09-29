<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import { useSnackbarStore } from '@/stores/snackbar'
import type { ReceiptDto, ReceiptUpdateRequest } from '@/types'
import { centsToDollars, dollarsToCents, formatCents } from '@/utils/currency'
import { flattenReceiptImage } from '@/utils/receiptImage'

interface EditableLine {
  description: string
  amount: string
}

const snackbar = useSnackbarStore()
const receiptInput = ref<HTMLInputElement | null>(null)
const receipts = ref<ReceiptDto[]>([])
const selectedId = ref<number | null>(null)
const selectedReceipt = computed(() => receipts.value.find(receipt => receipt.id === selectedId.value) ?? null)
const imageFile = ref<File | null>(null)
const imagePreviewUrl = ref('')
const receiptImageUrl = ref('')
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

async function loadReceipts() {
  loadingReceipts.value = true
  try {
    receipts.value = await apiClient.get<ReceiptDto[]>('/api/receipts') ?? []
    if (selectedId.value === null && receipts.value.length > 0)
      selectedId.value = receipts.value[0]!.id
  } catch (error: unknown) {
    snackbar.enqueueSnackbar(error instanceof Error ? error.message : 'Failed to load receipts', { variant: 'error' })
  } finally {
    loadingReceipts.value = false
  }
}

async function loadReceiptImage(receiptId: number) {
  try {
    const { blob } = await apiClient.download(`/api/receipts/${receiptId}/image`)
    if (selectedId.value !== receiptId) return
    releaseUrl(receiptImageUrl.value)
    receiptImageUrl.value = URL.createObjectURL(blob)
  } catch (error: unknown) {
    snackbar.enqueueSnackbar(error instanceof Error ? error.message : 'Failed to load receipt image', { variant: 'error' })
  }
}

watch(selectedReceipt, receipt => {
  merchantName.value = receipt?.merchantName ?? ''
  transactionDate.value = receipt?.transactionDate?.split('T')[0] ?? ''
  total.value = receipt?.totalAmountCents == null ? '' : centsToDollars(receipt.totalAmountCents)
  notes.value = receipt?.notes ?? ''
  lineItems.value = (receipt?.lineItems ?? []).map(line => ({
    description: line.description,
    amount: centsToDollars(line.amountCents),
  }))
  releaseUrl(receiptImageUrl.value)
  receiptImageUrl.value = ''
  if (receipt) void loadReceiptImage(receipt.id)
})

async function selectImage(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file) return
  input.value = ''
  preparingImage.value = true
  releaseUrl(imagePreviewUrl.value)
  imagePreviewUrl.value = ''
  imageFile.value = null
  try {
    imageFile.value = await flattenReceiptImage(file)
    imagePreviewUrl.value = URL.createObjectURL(imageFile.value)
  } catch (error: unknown) {
    snackbar.enqueueSnackbar(error instanceof Error ? error.message : 'Could not crop the receipt image', { variant: 'error' })
  } finally {
    preparingImage.value = false
  }
}

async function uploadReceipt() {
  if (!imageFile.value) return
  uploading.value = true
  try {
    const formData = new FormData()
    formData.append('image', imageFile.value)
    const receipt = await apiClient.upload<ReceiptDto>('/api/receipts', formData)
    receipts.value = [receipt, ...receipts.value.filter(existing => existing.id !== receipt.id)]
    selectedId.value = receipt.id
    imageFile.value = null
    releaseUrl(imagePreviewUrl.value)
    imagePreviewUrl.value = ''
    snackbar.enqueueSnackbar(
      receipt.processingMessage ?? 'Receipt uploaded. Review the extracted details.',
      { variant: receipt.processingMessage ? 'warning' : 'success' },
    )
  } catch (error: unknown) {
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
  saving.value = true
  try {
    const request: ReceiptUpdateRequest = {
      merchantName: merchantName.value.trim() || null,
      transactionDate: transactionDate.value || null,
      totalAmountCents: total.value.trim() ? dollarsToCents(total.value) : null,
      notes: notes.value.trim() || null,
      lineItems: lineItems.value
        .map(line => ({ description: line.description.trim(), amountCents: dollarsToCents(line.amount) }))
        .filter(line => line.description.length > 0 && line.amountCents > 0),
    }
    const updated = await apiClient.put<ReceiptDto>(`/api/receipts/${selectedReceipt.value.id}`, request)
    const index = receipts.value.findIndex(receipt => receipt.id === updated.id)
    if (index >= 0) receipts.value[index] = updated
    snackbar.enqueueSnackbar('Receipt saved', { variant: 'success' })
  } catch (error: unknown) {
    snackbar.enqueueSnackbar(error instanceof Error ? error.message : 'Failed to save receipt', { variant: 'error' })
  } finally {
    saving.value = false
  }
}

onMounted(() => void loadReceipts())
onBeforeUnmount(() => {
  releaseUrl(imagePreviewUrl.value)
  releaseUrl(receiptImageUrl.value)
})
</script>

<template>
  <div>
    <h5 class="text-h5 mb-4">Receipts</h5>

    <v-card class="pa-4 mb-4">
      <div class="text-subtitle-1 font-weight-medium mb-1">Add a receipt</div>
      <div class="text-body-2 text-medium-emphasis mb-3">
        Choose a receipt photo. Its edges are detected and perspective-corrected before upload.
      </div>
      <div class="d-flex flex-wrap align-center ga-3">
        <v-btn
          prepend-icon="mdi-camera-plus"
          variant="outlined"
          :loading="preparingImage"
          @click="chooseImage"
        >
          Choose or take a photo
        </v-btn>
        <input
          ref="receiptInput"
          type="file"
          accept="image/jpeg,image/png,image/webp"
          capture="environment"
          class="d-none"
          @change="selectImage"
        >
        <v-btn
          color="primary"
          prepend-icon="mdi-cloud-upload"
          :disabled="!imageFile || preparingImage"
          :loading="uploading"
          @click="uploadReceipt"
        >
          Upload receipt
        </v-btn>
      </div>
      <v-img
        v-if="imagePreviewUrl"
        :src="imagePreviewUrl"
        alt="Automatically cropped receipt preview"
        max-height="360"
        class="mt-4 rounded"
        contain
      />
    </v-card>

    <div class="receipt-layout">
      <v-card class="pa-2">
        <div class="text-subtitle-2 px-3 pt-2">Saved receipts</div>
        <div v-if="loadingReceipts" class="d-flex justify-center pa-6">
          <v-progress-circular indeterminate aria-label="Loading receipts" />
        </div>
        <v-list v-else-if="receipts.length > 0">
          <v-list-item
            v-for="receipt in receipts"
            :key="receipt.id"
            :active="receipt.id === selectedId"
            :title="receipt.merchantName || receipt.fileName"
            :subtitle="`${receipt.transactionDate ? new Date(receipt.transactionDate).toLocaleDateString() : 'Date not set'} · ${receipt.totalAmountCents == null ? 'Total not set' : formatCents(receipt.totalAmountCents)}`"
            prepend-icon="mdi-receipt-text"
            @click="selectedId = receipt.id"
          />
        </v-list>
        <div v-else class="text-body-2 text-medium-emphasis pa-4">No receipts yet.</div>
      </v-card>

      <v-card v-if="selectedReceipt" class="pa-4">
        <div class="d-flex flex-wrap justify-space-between align-center ga-2 mb-2">
          <div class="text-h6">Review receipt</div>
          <v-chip size="small" variant="outlined">{{ selectedReceipt.processingStatus }}</v-chip>
        </div>
        <v-alert
          v-if="selectedReceipt.processingMessage"
          type="info"
          variant="tonal"
          density="compact"
          class="mb-3"
        >
          {{ selectedReceipt.processingMessage }}
        </v-alert>
        <v-img
          v-if="receiptImageUrl"
          :src="receiptImageUrl"
          :alt="`Receipt from ${selectedReceipt.merchantName || selectedReceipt.fileName}`"
          max-height="420"
          class="mb-4 rounded"
          contain
        />
        <div class="d-flex flex-column ga-2">
          <v-text-field v-model="merchantName" label="Location / merchant" hide-details />
          <v-text-field v-model="transactionDate" label="Date" type="date" hide-details />
          <v-text-field v-model="total" label="Total amount" prefix="$" type="number" min="0" step="0.01" hide-details />
          <div class="d-flex align-center justify-space-between mt-2">
            <span class="text-subtitle-2">Line items (optional)</span>
            <v-btn size="small" variant="text" prepend-icon="mdi-plus" @click="addLineItem">Add line</v-btn>
          </div>
          <div v-for="(line, index) in lineItems" :key="index" class="d-flex align-center ga-2">
            <v-text-field v-model="line.description" label="Description" density="compact" hide-details />
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
          <v-textarea v-model="notes" label="Custom note" rows="2" auto-grow />
          <div class="d-flex justify-end">
            <v-btn color="primary" :loading="saving" @click="saveReceipt">Save corrections</v-btn>
          </div>
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

@media (max-width: 800px) {
  .receipt-layout {
    grid-template-columns: 1fr;
  }
}
</style>
