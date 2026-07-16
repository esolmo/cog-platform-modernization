'use strict'

import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import React from 'react'
import { DataTable } from '../../src/components/DataTable'
import type { ColumnDef } from '../../src/components/DataTable'

interface Row {
  id: number
  name: string
  status: string
}

const columns: ColumnDef<Row>[] = [
  { key: 'id', header: 'ID' },
  { key: 'name', header: 'Name' },
  { key: 'status', header: 'Status' },
]

const rows: Row[] = [
  { id: 1, name: 'Alice', status: 'active' },
  { id: 2, name: 'Bob', status: 'inactive' },
]

describe('DataTable', () => {
  it('renders column headers', () => {
    render(<DataTable columns={columns} rows={rows} keyField="id" />)
    expect(screen.getByText('ID')).toBeInTheDocument()
    expect(screen.getByText('Name')).toBeInTheDocument()
    expect(screen.getByText('Status')).toBeInTheDocument()
  })

  it('renders row data', () => {
    render(<DataTable columns={columns} rows={rows} keyField="id" />)
    expect(screen.getByText('Alice')).toBeInTheDocument()
    expect(screen.getByText('Bob')).toBeInTheDocument()
  })

  it('renders empty message when rows is empty', () => {
    render(<DataTable columns={columns} rows={[]} keyField="id" emptyMessage="No data" />)
    expect(screen.getByText('No data')).toBeInTheDocument()
  })

  it('renders loading state', () => {
    render(<DataTable columns={columns} rows={[]} keyField="id" loading />)
    expect(screen.getByText('Loading…')).toBeInTheDocument()
  })

  it('uses custom render function for cells', () => {
    const customColumns: ColumnDef<Row>[] = [
      { key: 'id', header: 'ID' },
      { key: 'status', header: 'Status', render: (row) => <span data-testid="badge">{row.status.toUpperCase()}</span> },
    ]
    render(<DataTable columns={customColumns} rows={[rows[0]]} keyField="id" />)
    expect(screen.getByTestId('badge').textContent).toBe('ACTIVE')
  })
})
