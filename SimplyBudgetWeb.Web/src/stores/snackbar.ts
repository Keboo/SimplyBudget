import { defineStore } from 'pinia'
import { ref } from 'vue'

export type SnackbarVariant = 'success' | 'error' | 'info' | 'warning'

interface SnackbarMessage {
  id: number
  text: string
  variant: SnackbarVariant
}

let nextId = 0

export const useSnackbarStore = defineStore('snackbar', () => {
  const messages = ref<SnackbarMessage[]>([])

  function enqueueSnackbar(text: string, options?: { variant?: SnackbarVariant }) {
    const id = nextId++
    messages.value.push({ id, text, variant: options?.variant ?? 'info' })
  }

  function dismiss(id: number) {
    messages.value = messages.value.filter(m => m.id !== id)
  }

  return { messages, enqueueSnackbar, dismiss }
})
