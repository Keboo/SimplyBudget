<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { apiClient } from '@/services/apiClient'
import type { ReceiptImageGeometry } from '@/types'
import { flattenReceiptImage } from '@/utils/receiptImage'
import ReceiptImageAdjustment from './ReceiptImageAdjustment.vue'

const props = withDefaults(defineProps<{ receiptId: number; title?: string; imageGeometry?: ReceiptImageGeometry | null; controls?: boolean; editable?: boolean; disabled?: boolean }>(), { controls: true })
const emit = defineEmits<{
  geometry: [value: ReceiptImageGeometry | null]
  busy: [value: boolean]
}>()
const imageFile = ref<File | null>(null)
const adjusting = ref(false)
const originalUrl = ref('')
const flatUrl = ref('')
const showOriginal = ref(false)
const url = computed(() => showOriginal.value || !flatUrl.value ? originalUrl.value : flatUrl.value)
const isPdf = ref(false)
const loading = ref(false)
const error = ref('')
const correctionError = ref('')
let request = 0

function release() {
  if (originalUrl.value) URL.revokeObjectURL(originalUrl.value)
  if (flatUrl.value) URL.revokeObjectURL(flatUrl.value)
  originalUrl.value = flatUrl.value = ''
}

async function load() {
  const current = ++request
  const geometry = props.imageGeometry ? JSON.parse(JSON.stringify(props.imageGeometry)) as ReceiptImageGeometry : null
  release()
  adjusting.value = false
  imageFile.value = null
  emit('busy', false)
  showOriginal.value = false
  loading.value = true
  error.value = ''
  correctionError.value = ''
  try {
    const { blob, fileName } = await apiClient.download(`/api/receipts/${props.receiptId}/image`)
    if (current !== request) return
    isPdf.value = blob.type === 'application/pdf'
    if (!isPdf.value) imageFile.value = new File([blob], fileName || 'receipt.jpg', { type: blob.type })
    originalUrl.value = URL.createObjectURL(blob)
    if (!isPdf.value && geometry) {
      try {
        const flat = await flattenReceiptImage(blob, geometry)
        if (current !== request) return
        flatUrl.value = URL.createObjectURL(flat)
      } catch {
        if (current === request) correctionError.value = 'The flat preview could not be built. Showing the original receipt instead.'
      }
    }
  } catch (e: unknown) {
    if (current === request)
      error.value = e instanceof Error ? e.message : 'Could not load receipt.'
  } finally {
    if (current === request) loading.value = false
  }
}

watch(() => [props.receiptId, props.imageGeometry], () => void load(), { immediate: true, deep: true })
onBeforeUnmount(() => {
  request++
  release()
})
</script>

<template>
  <div class="receipt-preview">
    <div v-if="loading" class="pa-4 text-center" role="status">
      <v-progress-circular indeterminate size="24" aria-label="Loading receipt" />
      <div class="text-body-2 mt-2">Loading receipt…</div>
    </div>
    <v-alert v-else-if="error" type="error" variant="tonal" class="mb-2">
      {{ error }}
      <v-btn variant="text" @click="load">Retry preview</v-btn>
    </v-alert>
    <template v-else-if="url">
      <v-alert v-if="correctionError" type="warning" variant="tonal" class="mb-2">{{ correctionError }}</v-alert>
      <ReceiptImageAdjustment v-if="adjusting && editable && imageFile" :file="imageFile" :original-url="originalUrl" :initial-geometry="imageGeometry" :automatic="false" :disabled="disabled" @geometry="emit('geometry', $event)" @busy="emit('busy', $event)" />
      <iframe v-else-if="isPdf" :src="url" :title="title || 'Receipt PDF'" class="receipt-pdf" />
      <v-img v-else :src="url" :alt="`${showOriginal || !flatUrl ? 'Original' : 'Flattened'}: ${title || 'receipt'}`" max-height="55vh" contain />
      <v-btn v-if="editable && imageFile && !adjusting" variant="outlined" class="mt-2 mr-2" :disabled="disabled" @click="adjusting = true">Adjust image</v-btn>
      <v-btn v-if="controls && flatUrl && !adjusting" variant="outlined" class="mt-2 mr-2" :aria-pressed="showOriginal" @click="showOriginal = !showOriginal">
        {{ showOriginal ? 'Show flat view' : 'Show original' }}
      </v-btn>
      <v-btn v-if="controls" :href="originalUrl" target="_blank" rel="noopener noreferrer" variant="text" prepend-icon="mdi-open-in-new" class="mt-2">
        Open original receipt
      </v-btn>
    </template>
  </div>
</template>

<style scoped>
.receipt-preview { min-width: 0; }
.receipt-pdf { width: 100%; height: 55vh; border: 0; }
</style>
