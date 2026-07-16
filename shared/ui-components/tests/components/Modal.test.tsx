'use strict'

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import React from 'react'
import { Modal } from '../../src/components/Modal'

describe('Modal', () => {
  it('renders nothing when open=false', () => {
    render(<Modal open={false} onClose={vi.fn()}><p>Content</p></Modal>)
    expect(screen.queryByText('Content')).not.toBeInTheDocument()
  })

  it('renders children when open=true', () => {
    render(<Modal open onClose={vi.fn()}><p>Modal body</p></Modal>)
    expect(screen.getByText('Modal body')).toBeInTheDocument()
  })

  it('renders title when provided', () => {
    render(<Modal open onClose={vi.fn()} title="Confirm Action"><p>body</p></Modal>)
    expect(screen.getByText('Confirm Action')).toBeInTheDocument()
  })

  it('calls onClose when close button is clicked', () => {
    const onClose = vi.fn()
    render(<Modal open onClose={onClose} title="Test"><p>x</p></Modal>)
    fireEvent.click(screen.getByRole('button', { name: /close modal/i }))
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('calls onClose when Escape is pressed', () => {
    const onClose = vi.fn()
    render(<Modal open onClose={onClose}><p>content</p></Modal>)
    fireEvent.keyDown(window, { key: 'Escape' })
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('calls onClose when backdrop is clicked', () => {
    const onClose = vi.fn()
    render(<Modal open onClose={onClose}><p>content</p></Modal>)
    const dialog = screen.getByRole('dialog')
    fireEvent.click(dialog)
    expect(onClose).toHaveBeenCalledOnce()
  })
})
