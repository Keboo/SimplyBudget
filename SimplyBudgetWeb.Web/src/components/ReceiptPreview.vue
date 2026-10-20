<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'
import { apiClient } from '@/services/apiClient'

const props = defineProps<{ receiptId: number; title?: string }>()
const url = ref('')
const isPdf = ref(false)
const loading = ref(false)
const error = ref('')
let request = 0

function release() {
  if (url.value) URL.revokeObjectURL(url.value)
  url.value = ''
}

async function load() {
  const current = ++request
  release()
  loading.value = true
  error.value = ''
  try {
    const { blob } = await apiClient.download(`/api/receipts/${props.receiptId}/image`)
    if (current !== request) return
    isPdf.value = blob.type === 'application/pdf'
    url.value = URL.createObjectURL(blob)
  } catch (e: unknown) {
    if (current === request)
      error.value = e instanceof Error ? e.message : 'Could not load receipt.'
  } finally {
    if (current === request) loading.value = false
  }
}

watch(() => props.receiptId, () => void load(), { immediate: true })
onBeforeUnmount(() => {
  request++
  release()
})
</script>

<template>
  <div class="receipt-preview">
    <div v-if="loading" class="pa-4 text-center" role="status">
      <v-progress-circular indeterminate size="24" aria-label="Loading receipt" />
      <div class="text-body-2 mt-2">Loading original receipt…</div>
    </div>
    <v-alert v-else-if="error" type="error" variant="tonal" class="mb-2">
      {{ error }}
      <v-btn variant="text" @click="load">Retry preview</v-btn>
    </v-alert>
    <template v-else-if="url">
      <iframe v-if="isPdf" :src="url" :title="title || 'Receipt PDF'" class="receipt-pdf" />
      <v-img v-else :src="url" :alt="title || 'Original receipt'" max-height="55vh" contain />
      <v-btn :href="url" target="_blank" rel="noopener noreferrer" variant="text" prepend-icon="mdi-open-in-new" class="mt-2">
        Open original receipt
      </v-btn>
    </template>
  </div>
</template>

<style scoped>
.receipt-preview { min-width: 0; }
.receipt-pdf { width: 100%; height: 55vh; border: 0; }
</style>
