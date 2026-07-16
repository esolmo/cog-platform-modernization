'use strict'

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import axios from 'axios'
import { LoginPage } from '../LoginPage'
import { useAuthStore } from '@/stores/authStore'

vi.mock('axios')
const mockedAxios = vi.mocked(axios, true)

const mockNavigate = vi.fn()
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')
  return { ...actual, useNavigate: () => mockNavigate }
})

function renderPage() {
  return render(
    <MemoryRouter>
      <LoginPage />
    </MemoryRouter>
  )
}

describe('LoginPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    useAuthStore.setState({ accessToken: null, loginName: null, customerId: null })
  })

  it('renders username and password fields', () => {
    renderPage()
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
  })

  it('navigates to /casino on successful login', async () => {
    mockedAxios.post = vi.fn().mockResolvedValueOnce({
      data: { accessToken: 'tok', loginName: 'alice', customerId: 'C1' },
    })

    renderPage()
    fireEvent.change(screen.getByLabelText(/username/i), { target: { value: 'alice' } })
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'pw' } })
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => expect(mockNavigate).toHaveBeenCalledWith('/casino', { replace: true }))
    expect(useAuthStore.getState().loginName).toBe('alice')
  })

  it('shows error on failed login', async () => {
    const err = Object.assign(new Error('Unauthorized'), {
      isAxiosError: true,
      response: { data: { error: 'Invalid credentials' }, status: 401 },
    })
    mockedAxios.post = vi.fn().mockRejectedValueOnce(err)
    mockedAxios.isAxiosError = vi.fn().mockReturnValue(true)

    renderPage()
    fireEvent.change(screen.getByLabelText(/username/i), { target: { value: 'bad' } })
    fireEvent.change(screen.getByLabelText(/password/i), { target: { value: 'wrong' } })
    fireEvent.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => expect(screen.getByText('Invalid credentials')).toBeInTheDocument())
  })
})
