export function formatCents(cents: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(cents / 100)
}

export function formatMonth(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`
}

export function parseMonth(yearMonth: string): Date {
  const [year, month] = yearMonth.split('-').map(Number)
  return new Date(year, month - 1, 1)
}

/**
 * Parses a `yyyy-MM-dd` (or `yyyy-MM`) date-input string as a local date, avoiding the
 * UTC-midnight interpretation that `new Date(string)` uses for ISO date-only strings (which
 * shifts to the previous day/month in negative-UTC-offset timezones).
 */
export function parseLocalDate(dateString: string): Date {
  const [year, month, day] = dateString.split('-').map(Number)
  if (!year || !month) return new Date()
  return new Date(year, month - 1, day || 1)
}

export function dollarsToCents(s: string): number {
  return Math.round((parseFloat(s) || 0) * 100)
}

export function centsToDollars(cents: number): string {
  return (cents / 100).toFixed(2)
}
