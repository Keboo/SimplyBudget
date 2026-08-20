import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { formatMonth, parseMonth } from '@/utils/currency'

/**
 * Returns a reactive `currentMonth` ref that is kept in sync with the `month`
 * query parameter (`?month=YYYY-MM`). The URL is updated immediately on setup
 * so the address bar always reflects the active month, enabling deep linking
 * and full-page refresh.
 */
export function useMonthQueryParam() {
  const route = useRoute()
  const router = useRouter()

  function resolveMonth(): Date {
    const q = route.query.month
    if (typeof q === 'string' && /^\d{4}-\d{2}$/.test(q)) return parseMonth(q)
    const now = new Date()
    return new Date(now.getFullYear(), now.getMonth(), 1)
  }

  const currentMonth = ref(resolveMonth())

  // Write the resolved month to the URL immediately so the address bar is in
  // sync even on first load when no query param was present.
  void router.replace({ query: { ...route.query, month: formatMonth(currentMonth.value) } })

  watch(currentMonth, (month) => {
    void router.replace({ query: { ...route.query, month: formatMonth(month) } })
  })

  return { currentMonth }
}
