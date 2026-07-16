import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import CustomerDashboardPage from '../pages/CustomerDashboardPage';
import * as accountsApi from '../api/accountsApi';
import type { Customer } from '../types/accounts';

const mockCustomer: Customer = {
  id: 1,
  loginName: 'player01',
  agentId: 1,
  agentLoginName: 'agent01',
  status: 'Active',
  oddsFormat: 'American',
  instantActionEnabled: true,
  createdAt: '2024-01-01T00:00:00Z',
  balance: {
    creditLimit: 1000,
    wagerLimit: 500,
    currentBalance: 200,
    availableCredit: 800,
    pendingWagerBalance: 0,
    pendingWagerCount: 0,
    freePlayBalance: 0,
  },
};

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/customers/1']}>
        <Routes>
          <Route path="/customers/:id" element={ui} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('CustomerDashboardPage', () => {
  beforeEach(() => {
    vi.spyOn(accountsApi, 'getCustomer').mockResolvedValue(mockCustomer);
    vi.spyOn(accountsApi, 'getTransactionsByCustomer').mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 25,
      totalPages: 0,
      hasPrevious: false,
      hasNext: false,
    });
  });

  it('renders customer login name', async () => {
    renderWithProviders(<CustomerDashboardPage />);
    expect(await screen.findByRole('heading', { name: /player01/i })).toBeInTheDocument();
  });

  it('shows Active status badge', async () => {
    renderWithProviders(<CustomerDashboardPage />);
    expect(await screen.findByText('Active')).toBeInTheDocument();
  });

  it('shows credit limit in balance summary', async () => {
    renderWithProviders(<CustomerDashboardPage />);
    expect(await screen.findByText('$1000.00')).toBeInTheDocument();
  });

  it('switches to Transactions tab on click', async () => {
    const user = userEvent.setup();
    renderWithProviders(<CustomerDashboardPage />);

    await screen.findByRole('heading', { name: /player01/i }); // wait for load
    await user.click(screen.getByRole('button', { name: 'Transactions' }));

    expect(screen.getByText('No transactions found.')).toBeInTheDocument();
  });
});
