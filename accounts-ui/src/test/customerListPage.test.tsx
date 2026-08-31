import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import CustomerListPage from '../pages/CustomerListPage';
import * as accountsApi from '../api/accountsApi';
import { useAuthStore } from '../stores/authStore';
import type { Customer, PagedResult } from '../types/accounts';

function makeCustomer(overrides: Partial<Customer> = {}): Customer {
  return {
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
    ...overrides,
  };
}

function makePage(items: Customer[], overrides: Partial<PagedResult<Customer>> = {}): PagedResult<Customer> {
  return {
    items,
    totalCount: items.length,
    page: 1,
    pageSize: 25,
    totalPages: 1,
    hasPrevious: false,
    hasNext: false,
    ...overrides,
  };
}

function renderWithProviders() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <CustomerListPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

const originalState = useAuthStore.getState();

describe('CustomerListPage', () => {
  afterEach(() => {
    useAuthStore.setState(originalState, true);
  });

  describe('as a scoped Agent (not Admin/MasterAgent)', () => {
    beforeEach(() => {
      useAuthStore.setState({ agentId: 42, roles: ['Agent'] });
    });

    it('calls getCustomersByAgent with the logged-in agent id, not getCustomers', async () => {
      const byAgentSpy = vi.spyOn(accountsApi, 'getCustomersByAgent').mockResolvedValue(makePage([makeCustomer()]));
      const allSpy = vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(makePage([]));

      renderWithProviders();

      await waitFor(() => expect(byAgentSpy).toHaveBeenCalledWith(42, 1, 25));
      expect(allSpy).not.toHaveBeenCalled();
    });

    it('does not render the search box', async () => {
      vi.spyOn(accountsApi, 'getCustomersByAgent').mockResolvedValue(makePage([makeCustomer()]));
      renderWithProviders();

      await screen.findByText('player01');
      expect(screen.queryByPlaceholderText('Search by login name...')).not.toBeInTheDocument();
    });
  });

  describe('as Admin', () => {
    beforeEach(() => {
      useAuthStore.setState({ agentId: null, roles: ['Admin'] });
    });

    it('calls getCustomers (list-all), not getCustomersByAgent', async () => {
      const allSpy = vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(makePage([makeCustomer()]));
      const byAgentSpy = vi.spyOn(accountsApi, 'getCustomersByAgent').mockResolvedValue(makePage([]));

      renderWithProviders();

      await waitFor(() => expect(allSpy).toHaveBeenCalledWith('', 1, 25));
      expect(byAgentSpy).not.toHaveBeenCalled();
    });

    it('renders a search box that requeries on input and resets to page 1', async () => {
      const allSpy = vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(makePage([makeCustomer()]));
      const user = userEvent.setup();
      renderWithProviders();

      await screen.findByText('player01');
      const search = screen.getByPlaceholderText('Search by login name...');
      await user.type(search, 'play');

      await waitFor(() => expect(allSpy).toHaveBeenLastCalledWith('play', 1, 25));
    });

    it('shows a loading state before data arrives', () => {
      vi.spyOn(accountsApi, 'getCustomers').mockReturnValue(new Promise(() => {}));
      renderWithProviders();
      expect(screen.getByText('Loading...')).toBeInTheDocument();
    });

    it('shows an error state when the query fails', async () => {
      vi.spyOn(accountsApi, 'getCustomers').mockRejectedValue(new Error('network error'));
      renderWithProviders();
      expect(await screen.findByText('Failed to load customers.')).toBeInTheDocument();
    });

    it('shows an empty state when there are no customers', async () => {
      vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(makePage([]));
      renderWithProviders();
      expect(await screen.findByText('No customers found.')).toBeInTheDocument();
    });

    it('renders a row per customer with formatted balance columns and a status badge', async () => {
      vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(makePage([makeCustomer({ status: 'Suspended' })]));
      renderWithProviders();

      await screen.findByText('player01');
      expect(screen.getByText('$1000.00')).toBeInTheDocument(); // credit limit
      expect(screen.getByText('$200.00')).toBeInTheDocument();  // current balance
      expect(screen.getByText('$800.00')).toBeInTheDocument();  // available
      expect(screen.getByText('Suspended')).toBeInTheDocument();
    });

    it('renders an em dash when alternateLoginName is absent', async () => {
      vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(makePage([makeCustomer({ alternateLoginName: undefined })]));
      renderWithProviders();

      await screen.findByText('player01');
      expect(screen.getByText('—')).toBeInTheDocument();
    });

    it('View link points at the customer detail route', async () => {
      vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(makePage([makeCustomer({ id: 7 })]));
      renderWithProviders();

      const link = await screen.findByRole('link', { name: 'View' });
      expect(link).toHaveAttribute('href', '/customers/7');
    });

    it('New Customer link points at the create route', async () => {
      vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(makePage([makeCustomer()]));
      renderWithProviders();

      const link = await screen.findByRole('link', { name: 'New Customer' });
      expect(link).toHaveAttribute('href', '/customers/new');
    });

    it('disables Previous on the first page and Next when there is no next page', async () => {
      vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(
        makePage([makeCustomer()], { hasPrevious: false, hasNext: false, page: 1, totalPages: 1 })
      );
      renderWithProviders();

      await screen.findByText('player01');
      expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
      expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
    });

    it('enables Next and advances the page when there is a next page', async () => {
      const allSpy = vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(
        makePage([makeCustomer()], { hasPrevious: false, hasNext: true, page: 1, totalPages: 2, totalCount: 30 })
      );
      const user = userEvent.setup();
      renderWithProviders();

      await screen.findByText('player01');
      expect(screen.getByRole('button', { name: 'Next' })).toBeEnabled();
      await user.click(screen.getByRole('button', { name: 'Next' }));

      await waitFor(() => expect(allSpy).toHaveBeenLastCalledWith('', 2, 25));
    });

    it('shows the total customer count, singular vs plural', async () => {
      vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(
        makePage([makeCustomer()], { totalCount: 1 })
      );
      renderWithProviders();
      expect(await screen.findByText('1 customer total')).toBeInTheDocument();
    });
  });

  describe('as MasterAgent', () => {
    it('is also treated as able to see all customers', async () => {
      useAuthStore.setState({ agentId: 5, roles: ['MasterAgent'] });
      const allSpy = vi.spyOn(accountsApi, 'getCustomers').mockResolvedValue(makePage([makeCustomer()]));

      renderWithProviders();

      await waitFor(() => expect(allSpy).toHaveBeenCalled());
    });
  });
});
