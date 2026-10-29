import { describe, expect, it } from 'vitest'
import { calculateReceiptSplits } from './receiptSplit'

describe('calculateReceiptSplits', () => {
  it('groups amounts by category and applies tax only to taxable amounts', () => {
    const totals = calculateReceiptSplits([
      { categoryId: 1, amountCents: 999, taxable: true, excluded: false },
      { categoryId: 1, amountCents: 1000, taxable: false, excluded: false },
      { categoryId: 2, amountCents: 500, taxable: true, excluded: false },
    ], 10)

    expect(totals.items).toEqual([
      { expenseCategoryId: 1, subtotalCents: 1999, taxCents: 100, amount: 2099 },
      { expenseCategoryId: 2, subtotalCents: 500, taxCents: 50, amount: 550 },
    ])
    expect(totals.taxCents).toBe(150)
    expect(totals.totalCents).toBe(2649)
  })

  it('excludes marked lines and reports unassigned included lines', () => {
    const totals = calculateReceiptSplits([
      { categoryId: null, amountCents: 125, taxable: true, excluded: false },
      { categoryId: 1, amountCents: 250, taxable: true, excluded: true },
      { categoryId: 2, amountCents: 300, taxable: false, excluded: false },
    ], null)

    expect(totals.items).toEqual([
      { expenseCategoryId: 2, subtotalCents: 300, taxCents: 0, amount: 300 },
    ])
    expect(totals.unassignedCount).toBe(1)
    expect(totals.totalCents).toBe(300)
  })

  it('rounds tax once per category so output amounts are whole cents', () => {
    const totals = calculateReceiptSplits([
      { categoryId: 1, amountCents: 3, taxable: true, excluded: false },
      { categoryId: 1, amountCents: 3, taxable: true, excluded: false },
      { categoryId: 2, amountCents: 4, taxable: true, excluded: false },
    ], 10)

    expect(totals.items.map(item => item.taxCents)).toEqual([1, 0])
    expect(totals.totalCents).toBe(11)
  })
})
