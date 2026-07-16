'use strict'

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { CasinoLobbyPage } from '../CasinoLobbyPage'
import * as casinoApiModule from '@/api/casinoApi'
import type { CasinoSession, CasinoBalance } from '@/types/casino'

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')
  return { ...actual, useNavigate: () => mockNavigate }
})

function makeClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } })
}

function renderPage(client: QueryClient) {
  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter>
        <CasinoLobbyPage />
      </MemoryRouter>
    </QueryClientProvider>
  )
}

const mockSession: CasinoSession = {
  customerId: 'C1',
  nickname: 'Alice',
  lobbyUrl: 'https://casino.example.com/lobby?token=abc',
  casinoBalance: 250.0,
  bonusBalance: 50.0,
  playthrough: 0,
}

const mockBalance: CasinoBalance = {
  availableBalance: 1000.0,
  casinoBalance: 250.0,
  bonusBalance: 50.0,
}

describe('CasinoLobbyPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('renders balance cards when session and balance load', async () => {
    vi.spyOn(casinoApiModule.casinoApi, 'getSession').mockResolvedValueOnce(mockSession)
    vi.spyOn(casinoApiModule.casinoApi, 'getBalance').mockResolvedValueOnce(mockBalance)

    renderPage(makeClient())

    await waitFor(() => expect(screen.getByText('Welcome, Alice')).toBeInTheDocument())
    expect(screen.getByText('$250.00')).toBeInTheDocument()
    expect(screen.getByText('$1,000.00') || screen.getByText('$1000.00')).toBeTruthy()
  })

  it('renders Play Now link with lobby URL', async () => {
    vi.spyOn(casinoApiModule.casinoApi, 'getSession').mockResolvedValueOnce(mockSession)
    vi.spyOn(casinoApiModule.casinoApi, 'getBalance').mockResolvedValueOnce(mockBalance)

    renderPage(makeClient())

    await waitFor(() => expect(screen.getByRole('link', { name: /play now/i })).toBeInTheDocument())
    const link = screen.getByRole('link', { name: /play now/i }) as HTMLAnchorElement
    expect(link.href).toBe('https://casino.example.com/lobby?token=abc')
    expect(link.target).toBe('_blank')
  })

  it('redirects to /setup when session returns 404', async () => {
    const err = Object.assign(new Error('Not Found'), {
      isAxiosError: true,
      response: { status: 404 },
    })
    vi.spyOn(casinoApiModule.casinoApi, 'getSession').mockRejectedValueOnce(err)
    // Make axios.isAxiosError return true for this error
    const axiosModule = await import('axios')
    vi.spyOn(axiosModule.default, 'isAxiosError').mockReturnValue(true)

    renderPage(makeClient())

    await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith('/setup', { replace: true }))
  })

  it('shows deposit and withdraw buttons', async () => {
    vi.spyOn(casinoApiModule.casinoApi, 'getSession').mockResolvedValueOnce(mockSession)
    vi.spyOn(casinoApiModule.casinoApi, 'getBalance').mockResolvedValueOnce(mockBalance)

    renderPage(makeClient())

    await waitFor(() => expect(screen.getByText('Deposit to Casino')).toBeInTheDocument())
    expect(screen.getByText('Withdraw from Casino')).toBeInTheDocument()
  })
})
