'use strict'

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { SetupPage } from '../SetupPage'
import * as casinoApiModule from '@/api/casinoApi'

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')
  return { ...actual, useNavigate: () => mockNavigate }
})

function renderPage() {
  return render(
    <MemoryRouter>
      <SetupPage />
    </MemoryRouter>
  )
}

describe('SetupPage', () => {
  beforeEach(() => vi.clearAllMocks())

  it('renders nickname field', () => {
    renderPage()
    expect(screen.getByLabelText(/nickname/i)).toBeInTheDocument()
  })

  it('disables submit when nickname is too short', () => {
    renderPage()
    const btn = screen.getByRole('button', { name: /enter the casino/i })
    expect(btn).toBeDisabled()
    fireEvent.change(screen.getByLabelText(/nickname/i), { target: { value: 'A' } })
    expect(btn).toBeDisabled()
    fireEvent.change(screen.getByLabelText(/nickname/i), { target: { value: 'AB' } })
    expect(btn).not.toBeDisabled()
  })

  it('navigates to /casino after successful registration', async () => {
    vi.spyOn(casinoApiModule.casinoApi, 'register').mockResolvedValueOnce({
      customerId: 'C1',
      nickname: 'TestUser',
      externalPlayerId: 'ext1',
      registeredAt: '2026-01-01T00:00:00Z',
    })

    renderPage()
    fireEvent.change(screen.getByLabelText(/nickname/i), { target: { value: 'TestUser' } })
    fireEvent.click(screen.getByRole('button', { name: /enter the casino/i }))

    await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith('/casino', { replace: true }))
  })

  it('shows error on registration failure', async () => {
    vi.spyOn(casinoApiModule.casinoApi, 'register').mockRejectedValueOnce(
      new Error('Nickname already taken')
    )

    renderPage()
    fireEvent.change(screen.getByLabelText(/nickname/i), { target: { value: 'TakenUser' } })
    fireEvent.click(screen.getByRole('button', { name: /enter the casino/i }))

    await waitFor(() =>
      expect(screen.getByText('Nickname already taken')).toBeInTheDocument()
    )
  })
})
