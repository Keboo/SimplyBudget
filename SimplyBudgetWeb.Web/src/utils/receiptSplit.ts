export interface ReceiptSplitLine {
  categoryId: number | null
  amountCents: number
  taxable: boolean
  excluded: boolean
}

export interface ReceiptCategorySplit {
  expenseCategoryId: number
  amount: number
  subtotalCents: number
  taxCents: number
}

export interface ReceiptSplitTotals {
  items: ReceiptCategorySplit[]
  unassignedCount: number
  taxCents: number
  totalCents: number
}

export function calculateReceiptSplits(
  lines: ReceiptSplitLine[],
  taxPercentage: number | null,
): ReceiptSplitTotals {
  const grouped = new Map<number, { subtotalCents: number; taxableCents: number }>()
  let unassignedCount = 0

  for (const line of lines) {
    if (line.excluded) continue
    if (line.categoryId === null) {
      unassignedCount++
      continue
    }

    const group = grouped.get(line.categoryId) ?? { subtotalCents: 0, taxableCents: 0 }
    group.subtotalCents += line.amountCents
    if (line.taxable) group.taxableCents += line.amountCents
    grouped.set(line.categoryId, group)
  }

  const items = [...grouped.entries()]
    .map(([expenseCategoryId, group]) => {
      const taxCents = taxPercentage === null
        ? 0
        : Math.round(group.taxableCents * taxPercentage / 100)
      return {
        expenseCategoryId,
        amount: group.subtotalCents + taxCents,
        subtotalCents: group.subtotalCents,
        taxCents,
      }
    })
    .filter(item => item.amount > 0)

  const taxCents = items.reduce((sum, item) => sum + item.taxCents, 0)
  return {
    items,
    unassignedCount,
    taxCents,
    totalCents: items.reduce((sum, item) => sum + item.amount, 0),
  }
}
