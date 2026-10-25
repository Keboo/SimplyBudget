import { afterEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import Layout from './Layout.vue'

vi.mock('@/stores/auth', () => ({
  useAuthStore: () => ({ isAuthenticated: true, displayName: 'Household reviewer', logout: vi.fn() }),
}))
enableAutoUnmount(afterEach)
afterEach(() => { document.body.innerHTML = '' })

describe('phone navigation', () => {
  it('keeps both activity routes and the selected month reachable, with account controls in More', async () => {
    vi.stubGlobal('innerWidth', 390)
    vi.stubGlobal('visualViewport', { addEventListener: vi.fn(), removeEventListener: vi.fn(), width: 390, height: 844, offsetTop: 0, offsetLeft: 0 })
    const router = createRouter({
      history: createMemoryHistory(),
      routes: ['/budget', '/history', '/pending-expenses', '/receipts', '/settings', '/import']
        .map(path => ({ path, component: { template: '<div>Page content</div>' } })),
    })
    await router.push('/pending-expenses?month=2026-02')
    await router.isReady()
    const wrapper = mount({ components: { Layout }, template: '<v-app><Layout /></v-app>' }, {
      attachTo: document.body,
      global: { plugins: [router, createVuetify({ components, directives })] },
    })
    await flushPromises()
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
