const amazonWordRegex = /\bamazon\b/i

export const AMAZON_TRANSACTIONS_URL = 'https://www.amazon.com/cpe/yourpayments/transactions'

export function hasAmazonInDescription(description: string | null | undefined): boolean {
  if (!description) return false
  return amazonWordRegex.test(description)
}
