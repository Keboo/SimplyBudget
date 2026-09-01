import { ref } from 'vue'
import { apiClient } from '@/services/apiClient'
import type { ExternalLinkRuleDto } from '@/types'

export interface ExternalLinkMatch {
  name: string
  url: string
}

const compiledPatterns = new WeakMap<ExternalLinkRuleDto, RegExp | null>()

function getPattern(rule: ExternalLinkRuleDto): RegExp | null {
  if (compiledPatterns.has(rule)) return compiledPatterns.get(rule) ?? null

  let pattern: RegExp | null = null
  if (rule.ruleRegex) {
    try {
      pattern = new RegExp(rule.ruleRegex, 'i')
    } catch {
      // Ignore invalid patterns so a single bad rule cannot break the page.
      pattern = null
    }
  }
  compiledPatterns.set(rule, pattern)
  return pattern
}

export function getExternalLinks(
  rules: ExternalLinkRuleDto[],
  description: string | null | undefined,
): ExternalLinkMatch[] {
  if (!description) return []

  const matches: ExternalLinkMatch[] = []
  for (const rule of rules) {
    if (!rule.url) continue
    const pattern = getPattern(rule)
    if (pattern?.test(description)) {
      matches.push({ name: rule.name ?? rule.url, url: rule.url })
    }
  }
  return matches
}

const externalLinkRules = ref<ExternalLinkRuleDto[]>([])
let loadPromise: Promise<void> | null = null

/**
 * Shared, lazily loaded set of external link rules. The rules change rarely, so they are
 * fetched once per session and reused by every page that renders external links.
 */
export function useExternalLinkRules() {
  function loadExternalLinkRules(): Promise<void> {
    loadPromise ??= apiClient
      .get<ExternalLinkRuleDto[]>('/api/external-links')
      .then((result) => {
        externalLinkRules.value = result ?? []
      })
      .catch(() => {
        // External links are non-essential; fail silently rather than blocking the page.
        externalLinkRules.value = []
      })
    return loadPromise
  }

  function externalLinksFor(description: string | null | undefined): ExternalLinkMatch[] {
    return getExternalLinks(externalLinkRules.value, description)
  }

  return { externalLinkRules, loadExternalLinkRules, externalLinksFor }
}

export function resetExternalLinkRulesCache() {
  loadPromise = null
  externalLinkRules.value = []
}
