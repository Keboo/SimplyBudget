import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { formatMonth, parseMonth } from '@/utils/currency'

const MONTH_STORAGE_KEY = 'SimplyBudgetWebMonth'

function isValidYearMonth(value: unknown): value is string {
  return typeof value === 'string' && /^\d{4}-\d{2}$/.test(value)
}

/**
 * Returns a reactive `currentMonth` ref that is kept in sync with the `month`
 * query parameter (`?month=YYYY-MM`). The URL is updated immediately on setup
 * so the address bar always reflects the active month, enabling deep linking
 * and full-page refresh. The selected month is also persisted and shared across
 * every page that uses this composable.
 */
export function useMonthQueryParam() {
  const route = useRoute()
  const router = useRouter()

  function getStoredMonth(): Date | null {
    const stored = localStorage.getItem(MONTH_STORAGE_KEY)
    if (!isValidYearMonth(stored)) return null
    return parseMonth(stored)
  }

  function storeMonth(month: Date) {
    localStorage.setItem(MONTH_STORAGE_KEY, formatMonth(month))
  }

  function resolveMonth(): Date {
    const q = route.query.month
    if (isValidYearMonth(q)) return parseMonth(q)

    const storedMonth = getStoredMonth()
    if (storedMonth) return storedMonth

    const now = new Date()
    return new Date(now.getFullYear(), now.getMonth(), 1)
  }

  const currentMonth = ref(resolveMonth())

  // Write the resolved month to the URL immediately so the address bar is in
  // sync even on first load when no query param was present.
  void router.replace({ query: { ...route.query, month: formatMonth(currentMonth.value) } })
  storeMonth(currentMonth.value)

  watch(currentMonth, (month) => {
    storeMonth(month)
    void router.replace({ query: { ...route.query, month: formatMonth(month) } })
  })

  return { currentMonth }
}
