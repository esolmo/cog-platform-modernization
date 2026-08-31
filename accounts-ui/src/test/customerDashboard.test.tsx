import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import CustomerDashboardPage from '../pages/CustomerDashboardPage';
import * as accountsApi from '../api/accountsApi';
import { useAuthStore } from '../stores/authStore';
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

const originalAuthState = useAuthStore.getState();

// suspendCustomer/activateCustomer resolve the raw AxiosResponse (unused by the component,
// which only cares about the promise settling) — a minimal stand-in satisfies the type.
function fakeAxiosResponse() {
  return { data: null, status: 204, statusText: 'No Content', headers: {}, config: {} } as never;
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

  afterEach(() => {
    useAuthStore.setState(originalAuthState, true);
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

  it('shows a loading state before the customer loads', () => {
    vi.spyOn(accountsApi, 'getCustomer').mockReturnValue(new Promise(() => {}));
    renderWithProviders(<CustomerDashboardPage />);
    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('shows a not-found state when the customer query fails', async () => {
    vi.spyOn(accountsApi, 'getCustomer').mockRejectedValue(new Error('404'));
    renderWithProviders(<CustomerDashboardPage />);
    expect(await screen.findByText('Customer not found.')).toBeInTheDocument();
  });

  it('the Personal tab is active by default', async () => {
    renderWithProviders(<CustomerDashboardPage />);
    await screen.findByRole('heading', { name: /player01/i });

    const personalTab = screen.getByRole('button', { name: 'Personal' });
    expect(personalTab.className).toMatch(/border-blue-600/);
    // Personal tab's own content (a read-only Login Name row) should be visible.
    expect(screen.getByText('Login Name')).toBeInTheDocument();
  });

  it('switches to the Limits tab on click', async () => {
    vi.spyOn(accountsApi, 'getCustomerWagerLimits').mockResolvedValue({
      customerId: 1, maxStraightWager: 200, maxParlayWager: 200, maxParlayPayout: 2000,
      maxTeaserWager: 100, maxIfBetWager: 100, maxLotteryPick3: 50, maxLotteryPick4: 50,
      maxParlayLegs: 10, minimumWager: 5,
    });
    vi.spyOn(accountsApi, 'getCustomerCasinoLimits').mockResolvedValue({
      customerId: 1, casinoWagerLimit: 100, casinoCreditLimit: 500,
    });

    const user = userEvent.setup();
    renderWithProviders(<CustomerDashboardPage />);

    await screen.findByRole('heading', { name: /player01/i });
    await user.click(screen.getByRole('button', { name: 'Limits' }));

    // "Wager Limit" (exact) — LimitsTab also has "Casino Wager Limit" and a transient
    // "Loading wager limits…" string, both of which also match a loose /wager limit/i regex.
    expect(await screen.findByText('Wager Limit', { exact: true })).toBeInTheDocument();
  });

  describe('Suspend / Activate', () => {
    it('shows a Suspend button for an Active customer, and calls suspendCustomer on click', async () => {
      useAuthStore.setState({ loginName: 'agent01' });
      const suspendSpy = vi.spyOn(accountsApi, 'suspendCustomer').mockResolvedValue(fakeAxiosResponse());
      const user = userEvent.setup();
      renderWithProviders(<CustomerDashboardPage />);

      await screen.findByRole('heading', { name: /player01/i });
      expect(screen.queryByRole('button', { name: 'Activate Customer' })).not.toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Suspend Customer' }));

      await waitFor(() => expect(suspendSpy).toHaveBeenCalledWith(1, 'agent01'));
    });

    it('shows an Activate button for a Suspended customer, and calls activateCustomer on click', async () => {
      useAuthStore.setState({ loginName: 'agent01' });
      vi.spyOn(accountsApi, 'getCustomer').mockResolvedValue({ ...mockCustomer, status: 'Suspended' });
      const activateSpy = vi.spyOn(accountsApi, 'activateCustomer').mockResolvedValue(fakeAxiosResponse());
      const user = userEvent.setup();
      renderWithProviders(<CustomerDashboardPage />);

      await screen.findByRole('heading', { name: /player01/i });
      expect(screen.queryByRole('button', { name: 'Suspend Customer' })).not.toBeInTheDocument();

      await user.click(screen.getByRole('button', { name: 'Activate Customer' }));

      await waitFor(() => expect(activateSpy).toHaveBeenCalledWith(1, 'agent01'));
    });

    it('disables the Suspend button and shows pending text while the mutation is in flight', async () => {
      let resolveSuspend!: () => void;
      vi.spyOn(accountsApi, 'suspendCustomer').mockReturnValue(
        new Promise((resolve) => { resolveSuspend = () => resolve(fakeAxiosResponse()); })
      );
      const user = userEvent.setup();
      renderWithProviders(<CustomerDashboardPage />);

      await screen.findByRole('heading', { name: /player01/i });
      await user.click(screen.getByRole('button', { name: 'Suspend Customer' }));

      expect(await screen.findByRole('button', { name: 'Suspending…' })).toBeDisabled();
      resolveSuspend();
    });
  });
});
