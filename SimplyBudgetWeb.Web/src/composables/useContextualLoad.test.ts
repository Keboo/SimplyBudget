import { beforeEach, describe, expect, it } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useContextualLoad } from './useContextualLoad'

beforeEach(() => setActivePinia(createPinia()))
describe('contextual load recovery', () => {
  it('retains same-context data on failure and clears the durable error only after successful retry', async () => {
    const state = useContextualLoad<string[]>([], 'Could not load')
    await state.load('October', async () => ['Groceries'])
    await state.load('October', async () => { throw new Error('Offline') })
    expect(state.data.value).toEqual(['Groceries'])
    expect(state.hasLoaded.value).toBe(true)
    expect(state.error.value).toBe('Offline')
    await state.load('October', async () => ['Updated groceries'])
    expect(state.data.value).toEqual(['Updated groceries'])
    expect(state.error.value).toBe('')
  })
  it('does not relabel the previous month or filter as current when another context fails', async () => {
    const state = useContextualLoad<number[]>([], 'Could not load')
    await state.load('October/all', async () => [10])
    await state.load('November/assigned', async () => { throw new Error('Offline') })
    expect(state.data.value).toEqual([])
    expect(state.hasLoaded.value).toBe(false)
    expect(state.error.value).toBe('Offline')
  })
  it('ignores late responses from an older request, including their errors and loading completion', async () => {
    const state = useContextualLoad<string[]>([], 'Could not load')
    let rejectOld!: (error: Error) => void
    const old = state.load('October', () => new Promise((_, reject) => { rejectOld = reject }))
    await state.load('November', async () => ['November data'])
    rejectOld(new Error('Old request failed'))
    await old
    expect(state.data.value).toEqual(['November data'])
    expect(state.error.value).toBe('')
    expect(state.loading.value).toBe(false)
  })
})
