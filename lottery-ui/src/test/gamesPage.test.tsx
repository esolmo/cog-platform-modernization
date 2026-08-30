import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
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
  it('renders COG Lottery heading', async () => {
    vi.mocked(lotteryApiModule.lotteryApi.getGames).mockResolvedValue([]);
    renderWithProviders(<GamesPage />);
    await waitFor(() => expect(screen.getByText('COG Lottery')).toBeInTheDocument());
  });

  it('renders loading state initially', () => {
    vi.mocked(lotteryApiModule.lotteryApi.getGames).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<GamesPage />);
    expect(screen.getByText(/loading games/i)).toBeInTheDocument();
  });

  it('renders My Tickets navigation link', async () => {
    vi.mocked(lotteryApiModule.lotteryApi.getGames).mockResolvedValue([]);
    renderWithProviders(<GamesPage />);
    await waitFor(() => expect(screen.getByText('My Tickets')).toBeInTheDocument());
  });
});
