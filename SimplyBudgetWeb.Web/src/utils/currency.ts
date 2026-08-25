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

export function dollarsToCents(s: string): number {
  return Math.round((parseFloat(s) || 0) * 100)
}

export function centsToDollars(cents: number): string {
  return (cents / 100).toFixed(2)
}
