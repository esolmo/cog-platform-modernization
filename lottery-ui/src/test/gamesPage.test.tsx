import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import GamesPage from '../pages/GamesPage';
import * as lotteryApiModule from '../api/lottery';

vi.mock('../api/lottery', () => ({
  lotteryApi: {
    getGames: vi.fn(),
    getDrawings: vi.fn(),
    purchaseTicket: vi.fn(),
    getMyTickets: vi.fn(),
    getTicket: vi.fn(),
    previewPicks: vi.fn(),
  },
}));

function renderWithProviders(ui: React.ReactElement) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
}

describe('GamesPage', () => {
  it('renders COG Lottery heading', () => {
    vi.mocked(lotteryApiModule.lotteryApi.getGames).mockResolvedValue([]);
    renderWithProviders(<GamesPage />);
    expect(screen.getByText('COG Lottery')).toBeInTheDocument();
  });

  it('renders loading state initially', () => {
    vi.mocked(lotteryApiModule.lotteryApi.getGames).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<GamesPage />);
    expect(screen.getByText(/loading games/i)).toBeInTheDocument();
  });

  it('renders My Tickets navigation link', () => {
    vi.mocked(lotteryApiModule.lotteryApi.getGames).mockResolvedValue([]);
    renderWithProviders(<GamesPage />);
    expect(screen.getByText('My Tickets')).toBeInTheDocument();
  });
});
