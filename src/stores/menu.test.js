import { describe, it, expect, vi, beforeEach } from 'vitest'

// Mock the menu API the store depends on.
vi.mock('../services/menuApi.js', () => ({
  isMenuApiConfigured: vi.fn(),
  fetchMenu: vi.fn(),
}))
// Keep the fallback import resolvable.
vi.mock('../data/menu.js', () => ({ MENU: [{ id: 'sample', name: 'Sample' }] }))

import { fetchMenu, isMenuApiConfigured } from '../services/menuApi.js'
import { useMenu } from './menu.js'

describe('menu store', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('shows an error/empty state (not the sample menu) when configured but the API fails', async () => {
    isMenuApiConfigured.mockReturnValue(true)
    fetchMenu.mockRejectedValue(new Error('down'))

    const { state, load } = useMenu()
    await load(true)

    expect(state.source).toBe('error')
    expect(state.items).toEqual([])
    expect(state.error).toBeTruthy()
  })

  it('uses API data when configured and the request succeeds', async () => {
    isMenuApiConfigured.mockReturnValue(true)
    fetchMenu.mockResolvedValue([{ id: 1, name: 'Real Burger' }])

    const { state, load } = useMenu()
    await load(true)

    expect(state.source).toBe('api')
    expect(state.items).toEqual([{ id: 1, name: 'Real Burger' }])
  })
})
