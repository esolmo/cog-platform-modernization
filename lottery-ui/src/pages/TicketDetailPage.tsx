import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { lotteryApi, PickEntryDto } from '../api/lottery';

const PICK_TYPE_LABEL: Record<PickEntryDto['pickType'], string> = { Straight: 'Straight', Boxed: 'Boxed' };

export default function TicketDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { data: ticket, isLoading, error } = useQuery({
    queryKey: ['ticket', id],
    queryFn: () => lotteryApi.getTicket(Number(id)),
    enabled: !!id,
  });

  if (isLoading) return <div className="p-8 text-gray-500">Loading ticket...</div>;
  if (error || !ticket) return <div className="p-8 text-red-600">Ticket not found.</div>;

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-indigo-700 text-white px-8 py-4 flex items-center gap-4">
        <Link to="/history" className="text-indigo-200 hover:text-white">&larr; My Tickets</Link>
        <h1 className="text-2xl font-bold">Ticket #{ticket.id}</h1>
      </header>

      <main className="max-w-2xl mx-auto p-8 space-y-6">
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <dl className="grid grid-cols-2 gap-4 text-sm">
            <div>
              <dt className="text-gray-500">Drawing</dt>
              <dd className="font-semibold text-gray-900">{ticket.drawingName}</dd>
            </div>
            <div>
              <dt className="text-gray-500">Play Date</dt>
              <dd className="font-semibold text-gray-900">
                {new Date(ticket.dateToPlay).toLocaleDateString()}
              </dd>
            </div>
            <div>
              <dt className="text-gray-500">Event Date</dt>
              <dd className="font-semibold text-gray-900">
                {new Date(ticket.eventDate).toLocaleString()}
              </dd>
            </div>
            <div>
              <dt className="text-gray-500">Total Cost</dt>
              <dd className="font-bold text-indigo-700 text-lg">${ticket.total.toFixed(2)}</dd>
            </div>
            <div className="col-span-2">
              <dt className="text-gray-500">Description</dt>
              <dd className="font-mono text-xs text-gray-700 mt-1 break-all">{ticket.description}</dd>
            </div>
          </dl>
        </div>

        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h2 className="text-lg font-semibold text-gray-800 mb-4">
            Picks ({ticket.picks.length})
          </h2>
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-gray-500 border-b border-gray-100">
                <th className="pb-2">Numbers</th>
                <th className="pb-2">Type</th>
                <th className="pb-2 text-right">Cost</th>
                <th className="pb-2 text-right">Prize</th>
              </tr>
            </thead>
            <tbody>
              {ticket.picks.map((pick: PickEntryDto, idx: number) => (
                <tr key={idx} className="border-b border-gray-50 hover:bg-gray-50">
                  <td className="py-2 font-mono">
                    {pick.number4 > 0
                      ? `${pick.number1}-${pick.number2}-${pick.number3}-${pick.number4}`
                      : `${pick.number1}-${pick.number2}-${pick.number3}`}
                  </td>
                  <td className="py-2">
                    <span className={`px-2 py-0.5 rounded text-xs font-medium ${
                      pick.pickType === 'Straight'
                        ? 'bg-blue-100 text-blue-700'
                        : 'bg-purple-100 text-purple-700'
                    }`}>
                      {PICK_TYPE_LABEL[pick.pickType]}
                    </span>
                  </td>
                  <td className="py-2 text-right">${pick.cost.toFixed(2)}</td>
                  <td className="py-2 text-right text-green-600">${pick.prize.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </main>
    </div>
  );
}
