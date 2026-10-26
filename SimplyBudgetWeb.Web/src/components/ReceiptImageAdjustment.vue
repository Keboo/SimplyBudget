<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import type { ReceiptImageGeometry, ReceiptImagePoint } from '@/types'
import { detectReceiptCorners } from '@/services/receiptCornerDetection'
import { flattenReceiptImage, imageBoundary, validGeometry } from '@/utils/receiptImage'

const props = defineProps<{ file: File; originalUrl: string; disabled?: boolean }>()
const emit = defineEmits<{
  geometry: [value: ReceiptImageGeometry | null]
  busy: [value: boolean]
}>()
const corners = ref(imageBoundary())
const editing = ref(false)
const processing = ref(false)
const detecting = ref(false)
const autoCropped = ref(false)
const detectionMessage = ref('')
const flatUrl = ref('')
const error = ref('')
const stage = ref<HTMLElement | null>(null)
const labels = ['Top-left', 'Top-right', 'Bottom-right', 'Bottom-left']
const polygon = computed(() => corners.value.corners.map(p => `${p.x * 100},${p.y * 100}`).join(' '))
const valid = computed(() => validGeometry(corners.value))
let generation = 0
let dragging: { index: number; pointer: number } | null = null

function release() {
  if (flatUrl.value) URL.revokeObjectURL(flatUrl.value)
  flatUrl.value = ''
}

function edit() {
  generation++
  processing.value = detecting.value = autoCropped.value = false
  detectionMessage.value = ''
  editing.value = true
  error.value = ''
  emit('busy', true)
}

function reset() {
  edit()
  corners.value = imageBoundary()
  release()
  emit('geometry', null)
}

function rotate() {
  const [a, b, c, d] = corners.value.corners
  corners.value.corners = [d, a, b, c]
}

function skip() {
  generation++
  dragging = null
  editing.value = processing.value = detecting.value = autoCropped.value = false
  release()
  error.value = ''
  emit('geometry', null)
  emit('busy', false)
}

function move(index: number, point: ReceiptImagePoint) {
  if (props.disabled || processing.value) return
  corners.value.corners[index] = {
    x: Math.max(0, Math.min(1, point.x)),
    y: Math.max(0, Math.min(1, point.y)),
  }
}

function pointerDown(event: PointerEvent, index: number) {
  if (props.disabled || processing.value || event.button !== 0) return
  dragging = { index, pointer: event.pointerId }
  ;(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId)
}

function pointerMove(event: PointerEvent) {
  if (!dragging || dragging.pointer !== event.pointerId || !stage.value) return
  const bounds = stage.value.getBoundingClientRect()
  if (bounds.width && bounds.height)
    move(dragging.index, { x: (event.clientX - bounds.left) / bounds.width, y: (event.clientY - bounds.top) / bounds.height })
}

function keyMove(event: KeyboardEvent, index: number) {
  const direction: Record<string, [number, number]> = { ArrowLeft: [-1, 0], ArrowRight: [1, 0], ArrowUp: [0, -1], ArrowDown: [0, 1] }
  const delta = direction[event.key]
  if (!delta) return
  event.preventDefault()
  const point = corners.value.corners[index]!
  const step = event.shiftKey ? 0.02 : 0.005
  move(index, { x: point.x + delta[0] * step, y: point.y + delta[1] * step })
}

function numericMove(event: Event, index: number, axis: 'x' | 'y') {
  const value = (event.target as HTMLInputElement).valueAsNumber
  if (Number.isFinite(value)) move(index, { ...corners.value.corners[index]!, [axis]: value / 100 })
}

async function flatten(current: number): Promise<boolean> {
  const geometry: ReceiptImageGeometry = {
    version: 1,
    corners: corners.value.corners.map(point => ({ ...point })) as ReceiptImageGeometry['corners'],
  }
  processing.value = true
  error.value = ''
  try {
    const blob = await flattenReceiptImage(props.file, geometry)
    if (current !== generation) return false
    release()
    flatUrl.value = URL.createObjectURL(blob)
    editing.value = false
    emit('geometry', geometry)
    emit('busy', false)
    return true
  } catch (e: unknown) {
    if (current === generation) error.value = e instanceof Error ? e.message : 'Could not flatten this image. You can still use the original.'
    return false
  } finally {
    if (current === generation) processing.value = false
  }
}

async function apply() {
  if (!valid.value || processing.value || props.disabled) return
  await flatten(++generation)
}

// Ask the Foundry vision model for the receipt's paper corners, then crop locally.
async function autoDetect() {
  const current = ++generation
  detecting.value = true
  detectionMessage.value = ''
  emit('busy', true)
  try {
    const detected = await detectReceiptCorners(props.file)
    if (current !== generation) return
    detecting.value = false
    if (!detected) {
      detectionMessage.value = 'The receipt edges were not found automatically. Adjust the corners manually if needed.'
      emit('busy', false)
      return
    }
    corners.value = detected
    autoCropped.value = await flatten(current)
    if (!autoCropped.value && current === generation) emit('busy', false)
  } catch (e: unknown) {
    if (current !== generation) return
    detecting.value = false
    detectionMessage.value = e instanceof Error && e.message ? e.message : 'Automatic receipt cropping is unavailable.'
    emit('busy', false)
  }
}

watch(() => props.file, () => {
  skip()
  corners.value = imageBoundary()
  detectionMessage.value = ''
  void autoDetect()
})
onMounted(() => void autoDetect())
onBeforeUnmount(() => {
  generation++
  dragging = null
  release()
})
</script>

<template>
  <section class="mt-4" aria-label="Receipt image adjustment">
    <p class="text-body-2 mb-3" role="status">
      {{ detecting ? 'Finding the receipt edges with AI… You can skip to upload the original or adjust the corners yourself.' : editing ? 'Place corners 1–4 around the receipt, clockwise from the flat view’s top-left. Drag, use arrow keys (Shift for larger steps), or enter coordinates below. Apply to see the flat result, or skip.' : flatUrl && autoCropped ? 'Receipt cropped automatically. Check the result, adjust the corners if needed, or skip. The original file will still be saved unchanged.' : flatUrl ? 'Flat view applied. The original file will still be saved unchanged.' : 'Original file selected. Optionally crop and straighten the receipt before uploading. The original is always saved.' }}
    </p>
    <p v-if="detectionMessage && !editing" class="text-body-2 text-medium-emphasis mb-3">{{ detectionMessage }}</p>
    <div class="stage-area">
      <div ref="stage" class="corner-stage">
        <img :src="editing ? originalUrl : flatUrl || originalUrl" :alt="flatUrl && !editing ? 'Flattened receipt preview' : 'Original receipt preview'" @error="error = 'Could not preview this image. Try another file or skip adjustment.'">
        <template v-if="editing">
          <svg viewBox="0 0 100 100" preserveAspectRatio="none" class="corner-overlay" aria-hidden="true">
            <polygon :points="polygon" />
          </svg>
          <button
            v-for="(point, index) in corners.corners"
            :key="index"
            type="button"
            class="corner-handle"
            :style="{ left: `${point.x * 100}%`, top: `${point.y * 100}%` }"
            :aria-label="`${labels[index]} corner ${index + 1}. Use arrow keys to move.`"
            :disabled="disabled || processing"
            @pointerdown="pointerDown($event, index)"
            @pointermove="pointerMove"
            @pointerup="dragging = null"
            @pointercancel="dragging = null"
            @lostpointercapture="dragging = null"
            @keydown="keyMove($event, index)"
          >{{ index + 1 }}</button>
        </template>
      </div>
    </div>
    <template v-if="editing">
      <details class="my-3">
        <summary>Enter corner coordinates (percent of image)</summary>
        <div v-for="(point, index) in corners.corners" :key="index" class="numeric-corner">
          <span>{{ index + 1 }}. {{ labels[index] }}</span>
          <label>X % <input type="number" min="0" max="100" step="0.5" :value="Number((point.x * 100).toFixed(2))" :aria-label="`${labels[index]} X percent`" :disabled="disabled || processing" @input="numericMove($event, index, 'x')"></label>
          <label>Y % <input type="number" min="0" max="100" step="0.5" :value="Number((point.y * 100).toFixed(2))" :aria-label="`${labels[index]} Y percent`" :disabled="disabled || processing" @input="numericMove($event, index, 'y')"></label>
        </div>
      </details>
      <p v-if="!valid" role="alert" class="text-error">Keep corners apart and clockwise without crossing. Select Reset corners to start again.</p>
    </template>
    <v-alert v-if="error" type="warning" variant="tonal" class="my-3">{{ error }}</v-alert>
    <div class="d-flex flex-wrap ga-2 mt-3">
      <v-btn v-if="!editing" variant="outlined" :disabled="disabled" @click="edit">Adjust corners</v-btn>
      <v-progress-circular v-if="detecting" indeterminate size="24" aria-label="Detecting receipt edges" class="align-self-center" />
      <template v-else>
        <v-btn color="primary" :disabled="disabled || !valid" :loading="processing" @click="apply">Apply flat view</v-btn>
        <v-btn variant="outlined" :disabled="disabled || processing" @click="reset">Reset corners</v-btn>
        <v-btn variant="outlined" :disabled="disabled || processing" @click="rotate">Rotate flat view 90° clockwise</v-btn>
      </template>
      <v-btn :disabled="disabled" variant="text" @click="skip">Skip — use original</v-btn>
    </div>
  </section>
</template>

<style scoped>
.stage-area { padding: 24px; text-align: center; }
.corner-stage { position: relative; display: inline-block; max-width: 100%; vertical-align: top; }
.corner-stage img { display: block; max-width: 100%; max-height: 55vh; object-fit: contain; image-orientation: from-image; }
.corner-overlay { position: absolute; inset: 0; width: 100%; height: 100%; pointer-events: none; }
.corner-overlay polygon { fill: #1976d222; stroke: #1976d2; stroke-width: 2; vector-effect: non-scaling-stroke; }
.corner-handle { position: absolute; transform: translate(-50%, -50%); width: 44px; height: 44px; border-radius: 50%; background: #0d47a1; color: white; border: 2px solid white; touch-action: none; font-weight: bold; }
.corner-handle:focus-visible { outline: 3px solid #ffab00; outline-offset: 2px; }
.numeric-corner { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; margin-top: 8px; }
.numeric-corner span { min-width: 140px; }
.numeric-corner input { width: 80px; min-height: 44px; border: 1px solid currentColor; border-radius: 4px; padding: 4px; }
summary { cursor: pointer; min-height: 44px; padding: 8px 0; }
</style>
