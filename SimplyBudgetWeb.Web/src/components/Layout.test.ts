import { afterEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Layout from './Layout.vue'
import appStyles from '@/index.css?raw'

vi.mock('@/stores/auth', () => ({
  useAuthStore: () => ({ isAuthenticated: true, displayName: 'Household reviewer', logout: vi.fn() }),
}))
enableAutoUnmount(afterEach)
afterEach(() => { document.body.innerHTML = '' })

async function mountLayout(path: string, width = 390, theme = 'light') {
  vi.stubGlobal('innerWidth', width)
  vi.stubGlobal('visualViewport', { addEventListener: vi.fn(), removeEventListener: vi.fn(), width, height: 844, offsetTop: 0, offsetLeft: 0 })
  const router = createRouter({
    history: createMemoryHistory(),
    routes: ['/budget', '/history', '/pending-expenses', '/receipts', '/settings', '/import']
      .map(path => ({ path, component: { template: '<div>Page content</div>' } })),
  })
  await router.push(path)
  await router.isReady()
  const wrapper = mount({ components: { Layout }, template: '<v-app><Layout /></v-app>' }, {
    attachTo: document.body,
    global: { plugins: [router, createVuetify({ components, directives, theme: { defaultTheme: theme } })] },
  })
  await flushPromises()
  return { wrapper, router }
}

describe('phone navigation', () => {
  it.each([
    ['/budget', 'Budget'],
    ['/history', 'Activity'],
    ['/pending-expenses', 'Activity'],
    ['/receipts', 'Receipts'],
    ['/settings', 'More'],
    ['/import', 'More'],
  ])('keeps only the current destination active on %s, even while More is open', async (path, label) => {
    const { wrapper } = await mountLayout(path)
    const activeLabels = () => wrapper.findAll('.phone-navigation .v-btn--active')
      .map(button => button.attributes('aria-label'))
    expect(activeLabels()).toEqual([label])

    await wrapper.get('[aria-label="More"]').trigger('click')
    await flushPromises()
    expect(activeLabels()).toEqual([label])
  })

  it('keeps both activity routes and the selected month reachable, with account controls in More', async () => {
    const { wrapper, router } = await mountLayout('/pending-expenses?month=2026-02')
    expect(wrapper.get('header').text()).toBe('Pending Expenses')
    expect(wrapper.get('[aria-label="Activity"]').classes()).toContain('v-btn--active')
    await wrapper.get('[aria-label="Activity"]').trigger('click')
    await flushPromises()
    const expenses = document.querySelector<HTMLAnchorElement>('a[href="/history?month=2026-02"]')
    expect(expenses?.textContent).toContain('Expenses')
    expect(document.querySelector('a[href="/pending-expenses?month=2026-02"]')).not.toBeNull()
    expenses!.click()
    await flushPromises()
    await vi.waitFor(() => expect(router.currentRoute.value.fullPath).toBe('/history?month=2026-02'))
    await wrapper.get('[aria-label="More"]').trigger('click')
    await flushPromises()
    expect(document.querySelector('[role="dialog"]')?.textContent).toContain('Household reviewer')
    expect(document.querySelector('[role="dialog"]')?.textContent).toContain('Sign out')
    document.querySelector<HTMLAnchorElement>('a[href="/settings"]')!.click()
    await flushPromises()
    await vi.waitFor(() => expect(router.currentRoute.value.path).toBe('/settings'))
    expect(wrapper.get('header').text()).toBe('Settings')
  })
})

describe('dark navigation link colors', () => {
  it.each([390, 1280])('does not give inactive navigation links the text-link color at width %i', async (width) => {
    const { wrapper } = await mountLayout('/receipts', width, 'dark')
    const style = document.createElement('style')
    style.textContent = appStyles
    document.body.appendChild(style)
    const textLink = document.createElement('a')
    textLink.href = '/help'
    textLink.textContent = 'Help'
    wrapper.element.appendChild(textLink)
    const textLinkRules = Array.from(style.sheet!.cssRules)
      .filter((rule): rule is CSSStyleRule => rule instanceof CSSStyleRule
        && rule.style.color !== '' && textLink.matches(rule.selectorText))
    expect(textLinkRules.length).toBeGreaterThan(0)

    if (width === 390) {
      await wrapper.get('[aria-label="More"]').trigger('click')
      await flushPromises()
    }

    const inactiveLinks = document.querySelectorAll('a.v-btn:not(.v-btn--active), a.v-list-item:not(.v-list-item--active)')
    expect(inactiveLinks.length).toBeGreaterThan(0)
    for (const link of inactiveLinks) {
      for (const rule of textLinkRules) {
        expect(link.matches(rule.selectorText), link.textContent ?? '').toBe(false)
      }
    }
  })
})
