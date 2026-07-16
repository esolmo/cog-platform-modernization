'use strict'

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import React from 'react'
import { Alert } from '../../src/components/Alert'

describe('Alert', () => {
  it('renders children', () => {
    render(<Alert>Something went wrong</Alert>)
    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
  })

  it('renders title when provided', () => {
    render(<Alert title="Error">Details here</Alert>)
    expect(screen.getByText('Error')).toBeInTheDocument()
    expect(screen.getByText('Details here')).toBeInTheDocument()
  })

  it('shows dismiss button when onDismiss is provided', () => {
    const onDismiss = vi.fn()
    render(<Alert onDismiss={onDismiss}>Alert text</Alert>)
    const btn = screen.getByRole('button', { name: /dismiss/i })
    expect(btn).toBeInTheDocument()
    fireEvent.click(btn)
    expect(onDismiss).toHaveBeenCalledOnce()
  })

  it('does not render dismiss button without onDismiss', () => {
    render(<Alert>No dismiss</Alert>)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
  })

  it('applies error variant classes', () => {
    render(<Alert variant="error">Oops</Alert>)
    expect(screen.getByRole('alert').className).toContain('bg-red-900')
  })

  it('applies success variant classes', () => {
    render(<Alert variant="success">Done</Alert>)
    expect(screen.getByRole('alert').className).toContain('bg-green-900')
  })
})
