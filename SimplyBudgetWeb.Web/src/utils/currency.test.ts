import { describe, expect, it } from 'vitest'
import { centsToDollars, dollarsToCents, formatCents, parseLocalDate } from './currency'

describe('currency utils', () => {
  it('converts dollars to cents with rounding', () => {
    expect(dollarsToCents('12.34')).toBe(1234)
    expect(dollarsToCents('0.1')).toBe(10)
  })

  it('treats empty or invalid dollars as zero', () => {
    expect(dollarsToCents('')).toBe(0)
    expect(dollarsToCents('abc')).toBe(0)
  })

  it('converts cents to a two-decimal dollar string', () => {
    expect(centsToDollars(1250)).toBe('12.50')
    expect(centsToDollars(5)).toBe('0.05')
  })

  it('formats cents as USD', () => {
    expect(formatCents(123456)).toBe('$1,234.56')
  })

  it('parses date-only strings as local dates', () => {
    const date = parseLocalDate('2026-03-04')
    expect([date.getFullYear(), date.getMonth(), date.getDate()]).toEqual([2026, 2, 4])
  })
})
