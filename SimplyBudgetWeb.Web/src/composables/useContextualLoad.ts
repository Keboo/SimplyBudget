import { ref, type Ref } from 'vue'
import { useSnackbarStore } from '@/stores/snackbar'

export function useContextualLoad<T>(initialValue: T, failureMessage: string) {
  const data = ref(initialValue) as Ref<T>
  const loading = ref(false)
  const error = ref('')
  const hasLoaded = ref(false)
  const snackbar = useSnackbarStore()
  let activeContext: string | null = null
  let requestId = 0

  async function load(context: string, request: () => Promise<T>) {
    const id = ++requestId
    if (activeContext !== context) {
      activeContext = context
      data.value = initialValue
      hasLoaded.value = false
      error.value = ''
    }
    loading.value = true
    try {
      const result = await request()
      if (id !== requestId) return
      data.value = result
      hasLoaded.value = true
      error.value = ''
    } catch (cause: unknown) {
      if (id !== requestId) return
      error.value = cause instanceof Error ? cause.message : failureMessage
      snackbar.enqueueSnackbar(error.value, { variant: 'error' })
    } finally {
      if (id === requestId) loading.value = false
    }
  }

  return { data, loading, error, hasLoaded, load }
}
