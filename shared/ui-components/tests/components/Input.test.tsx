'use strict'

import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import React from 'react'
import { Input } from '../../src/components/Input'

describe('Input', () => {
  it('renders input element', () => {
    render(<Input placeholder="Enter value" />)
    expect(screen.getByPlaceholderText('Enter value')).toBeInTheDocument()
  })

  it('shows error message', () => {
    render(<Input error="Required field" />)
    expect(screen.getByText('Required field')).toBeInTheDocument()
  })

  it('applies red border on error', () => {
    render(<Input error="Bad input" />)
    expect(screen.getByRole('textbox').className).toContain('border-red-500')
  })

  it('renders prefix character', () => {
    render(<Input prefix="$" placeholder="Amount" />)
    expect(screen.getByText('$')).toBeInTheDocument()
  })

  it('passes through standard input attributes', () => {
    render(<Input type="email" required placeholder="Email" />)
    const input = screen.getByPlaceholderText('Email')
    expect(input).toHaveAttribute('type', 'email')
    expect(input).toBeRequired()
  })
})
