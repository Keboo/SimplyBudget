export function tryParseSearchAmountInCents(value: string): number | null {
  const trimmedValue = value.trim()
  if (!trimmedValue) return null

  const hasParens = trimmedValue.startsWith('(') && trimmedValue.endsWith(')')
  const valueWithoutParens = hasParens ? trimmedValue.slice(1, -1).trim() : trimmedValue
  const normalizedValue = valueWithoutParens.replace(/[$,\s]/g, '')
  if (!normalizedValue || normalizedValue === '-' || normalizedValue === '+') return null

  const parsedAmount = Number(normalizedValue)
  if (!Number.isFinite(parsedAmount)) return null

  const signedAmount = hasParens ? -parsedAmount : parsedAmount
  return Math.round(signedAmount * 100)
}

export function includesSearchText(value: string | null | undefined, normalizedSearchText: string): boolean {
  if (!value || !normalizedSearchText) return false
  return value.toLocaleLowerCase().includes(normalizedSearchText)
}
