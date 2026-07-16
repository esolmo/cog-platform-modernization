import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import WagersReport from '../pages/WagersReport';

vi.mock('../api/reports', () => ({
  reportsApi: {
    getWagerActivity: vi.fn().mockResolvedValue([]),
    getChangedTransactions: vi.fn().mockResolvedValue([]),
    searchAgents: vi.fn().mockResolvedValue([]),
    searchCustomers: vi.fn().mockResolvedValue([]),
    getPackageTracker: vi.fn().mockResolvedValue([]),
  },
}));

function renderWithProviders(ui: React.ReactElement) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <Routes>
          <Route path="/" element={ui} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('WagersReport', () => {
  it('renders the page heading', () => {
    renderWithProviders(<WagersReport />);
    expect(screen.getByText('Wager Activity')).toBeInTheDocument();
  });

  it('renders Login ID input', () => {
    renderWithProviders(<WagersReport />);
    expect(screen.getByPlaceholderText(/ticket writer login/i)).toBeInTheDocument();
  });

  it('renders date range filters', () => {
    renderWithProviders(<WagersReport />);
    expect(screen.getByText('From')).toBeInTheDocument();
    expect(screen.getByText('To')).toBeInTheDocument();
  });

  it('renders Run Report button', () => {
    renderWithProviders(<WagersReport />);
    expect(screen.getByRole('button', { name: /run report/i })).toBeInTheDocument();
  });

  it('disables Run Report button when no login ID is entered', () => {
    renderWithProviders(<WagersReport />);
    expect(screen.getByRole('button', { name: /run report/i })).toBeDisabled();
  });
});
