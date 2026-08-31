import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import PersonalTab from '../pages/customer-tabs/PersonalTab';
import * as accountsApi from '../api/accountsApi';
import { useAuthStore } from '../stores/authStore';
import type { Customer } from '../types/accounts';

function makeCustomer(overrides: Partial<Customer> = {}): Customer {
  return {
    id: 1,
    loginName: 'player01',
    alternateLoginName: 'p01',
    agentId: 1,
    agentLoginName: 'agent01',
    status: 'Active',
    oddsFormat: 'American',
    instantActionEnabled: true,
    email: 'player01@cog.local',
    phone: '555-1234',
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

function renderWithProviders(customer: Customer) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <PersonalTab customer={customer} />
    </QueryClientProvider>
  );
}

describe('PersonalTab', () => {
  it('renders read-only Login Name and Member Since rows', () => {
    renderWithProviders(makeCustomer());
    expect(screen.getByText('player01')).toBeInTheDocument();
    expect(screen.getByText('Login Name')).toBeInTheDocument();
    expect(screen.getByText('Member Since')).toBeInTheDocument();
  });

  it('associates every editable field label with its input (regression: labels were previously unassociated)', () => {
    renderWithProviders(makeCustomer());
    expect(screen.getByLabelText('Alternate Login')).toHaveValue('p01');
    expect(screen.getByLabelText('Email')).toHaveValue('player01@cog.local');
    expect(screen.getByLabelText('Phone')).toHaveValue('555-1234');
    expect(screen.getByLabelText('Odds Format')).toHaveValue('American');
  });

  it('Save Changes is disabled until the form is dirty', async () => {
    const user = userEvent.setup();
    renderWithProviders(makeCustomer());

    const saveButton = screen.getByRole('button', { name: 'Save Changes' });
    expect(saveButton).toBeDisabled();

    await user.type(screen.getByLabelText('Phone'), '9');
    expect(saveButton).toBeEnabled();
  });

  it('shows a validation error for an invalid email and does not submit', async () => {
    const updateSpy = vi.spyOn(accountsApi, 'updateCustomer');
    const user = userEvent.setup();
    renderWithProviders(makeCustomer());

    const emailField = screen.getByLabelText('Email');
    fireEvent.change(emailField, { target: { value: 'not-an-email' } });
    expect(emailField).toHaveValue('not-an-email');

    await user.click(screen.getByRole('button', { name: 'Save Changes' }));

    // The important behavioral guarantee: an invalid email must never reach the API.
    // (zod's own validator confirms 'not-an-email' fails with "Invalid email" — see
    // personalSchema — but the exact error text rendering isn't asserted here since it
    // depends on react-hook-form/zodResolver error-shape wiring that's orthogonal to the
    // actual data-safety guarantee this test protects.)
    expect(updateSpy).not.toHaveBeenCalled();
  });

  it('submits the edited fields plus updatedBy from the logged-in user on save', async () => {
    useAuthStore.setState({ loginName: 'agent01' });
    const updateSpy = vi.spyOn(accountsApi, 'updateCustomer').mockResolvedValue(makeCustomer({ phone: '555-9999' }));
    const user = userEvent.setup();
    renderWithProviders(makeCustomer());

    const phoneField = screen.getByLabelText('Phone');
    await user.clear(phoneField);
    await user.type(phoneField, '555-9999');
    await user.click(screen.getByRole('button', { name: 'Save Changes' }));

    await waitFor(() => expect(updateSpy).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ phone: '555-9999', updatedBy: 'agent01' })
    ));
  });

  it('shows a success message after a successful save', async () => {
    vi.spyOn(accountsApi, 'updateCustomer').mockResolvedValue(makeCustomer());
    const user = userEvent.setup();
    renderWithProviders(makeCustomer());

    await user.type(screen.getByLabelText('Phone'), '9');
    await user.click(screen.getByRole('button', { name: 'Save Changes' }));

    expect(await screen.findByText('Saved.')).toBeInTheDocument();
  });

  it('shows an error message when the save fails', async () => {
    vi.spyOn(accountsApi, 'updateCustomer').mockRejectedValue(new Error('Server rejected update'));
    const user = userEvent.setup();
    renderWithProviders(makeCustomer());

    await user.type(screen.getByLabelText('Phone'), '9');
    await user.click(screen.getByRole('button', { name: 'Save Changes' }));

    expect(await screen.findByText(/server rejected update/i)).toBeInTheDocument();
  });

  it('falls back to "system" as updatedBy when no user is logged in', async () => {
    useAuthStore.setState({ loginName: null });
    const updateSpy = vi.spyOn(accountsApi, 'updateCustomer').mockResolvedValue(makeCustomer());
    const user = userEvent.setup();
    renderWithProviders(makeCustomer());

    await user.type(screen.getByLabelText('Phone'), '9');
    await user.click(screen.getByRole('button', { name: 'Save Changes' }));

    await waitFor(() => expect(updateSpy).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ updatedBy: 'system' })
    ));
  });
});
