import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { lotteryApi, TicketDto } from '../api/lottery';

export default function HistoryPage() {
  const { data: tickets, isLoading, error } = useQuery({
    queryKey: ['my-tickets'],
    queryFn: () => lotteryApi.getMyTickets(),
  });

  if (isLoading) return <div className="p-8 text-gray-500">Loading tickets...</div>;
  if (error) return <div className="p-8 text-red-600">Failed to load ticket history.</div>;

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-indigo-700 text-white px-8 py-4 flex items-center gap-4">
        <Link to="/games" className="text-indigo-200 hover:text-white">&larr; Games</Link>
        <h1 className="text-2xl font-bold">My Tickets</h1>
      </header>

      <main className="max-w-3xl mx-auto p-8">
        {!tickets?.length ? (
          <div className="text-center text-gray-500 py-16">
            <p className="text-lg">No tickets yet.</p>
            <Link to="/games" className="mt-4 inline-block text-indigo-600 hover:underline">
              Buy your first ticket
            </Link>
          </div>
        ) : (
          <div className="space-y-4">
            {tickets.map((ticket: TicketDto) => (
              <Link
                key={ticket.id}
                to={`/tickets/${ticket.id}`}
                className="block bg-white rounded-lg border border-gray-200 p-5 hover:border-indigo-400 hover:shadow-sm transition-all"
              >
                <div className="flex items-center justify-between">
                  <div>
                    <p className="font-semibold text-gray-900">
                      {ticket.drawingName}
                    </p>
                    <p className="text-sm text-gray-500 mt-0.5">
                      Play date: {new Date(ticket.dateToPlay).toLocaleDateString()} ·
                      Purchased: {new Date(ticket.purchasedAt).toLocaleString()}
                    </p>
                    <p className="text-xs text-gray-400 mt-1 font-mono truncate max-w-xs">
                      {ticket.description}
                    </p>
                  </div>
                  <span className="text-lg font-bold text-indigo-700">
                    ${ticket.total.toFixed(2)}
                  </span>
                </div>
              </Link>
            ))}
          </div>
        )}
      </main>
    </div>
  );
}
