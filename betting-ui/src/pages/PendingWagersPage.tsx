'use strict'

import { useQuery } from '@tanstack/react-query'
import { format } from 'date-fns'
import { bettingApi } from '@/api/bettingApi'
import { useAuthStore } from '@/stores/authStore'
import type { Wager } from '@/types/betting'

function WagerStatusBadge({ status }: { status: Wager['status'] }): React.ReactElement {
  const colours: Record<Wager['status'], string> = {
    Pending: 'bg-yellow-100 text-yellow-800',
    Won: 'bg-green-100 text-green-800',
    Lost: 'bg-red-100 text-red-800',
    Push: 'bg-gray-100 text-gray-800',
    Cancelled: 'bg-gray-100 text-gray-500',
    NoAction: 'bg-gray-100 text-gray-500',
  }
  return (
    <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${colours[status]}`}>
      {status}
    </span>
  )
}

export function PendingWagersPage(): React.ReactElement {
  const agentId = useAuthStore((s) => s.agentId)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['pending-wagers', agentId],
    queryFn: () => bettingApi.getPendingWagers(agentId!),
    enabled: !!agentId,
    refetchInterval: 30_000,
  })

  if (isLoading) return <div className="p-8 text-center">Loading wagers...</div>
  if (isError) return <div className="p-8 text-center text-red-600">Failed to load wagers.</div>

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Pending Wagers</h1>
      <div className="overflow-hidden rounded-lg border border-gray-200 bg-white shadow-sm">
        <table className="min-w-full divide-y divide-gray-200 text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left font-semibold text-gray-600">Ticket</th>
              <th className="px-4 py-3 text-left font-semibold text-gray-600">Customer</th>
              <th className="px-4 py-3 text-left font-semibold text-gray-600">Type</th>
              <th className="px-4 py-3 text-right font-semibold text-gray-600">Risk</th>
              <th className="px-4 py-3 text-right font-semibold text-gray-600">Win</th>
              <th className="px-4 py-3 text-left font-semibold text-gray-600">Status</th>
              <th className="px-4 py-3 text-left font-semibold text-gray-600">Date</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {data?.items.map((wager) => (
              <tr key={wager.id} className="hover:bg-gray-50">
                <td className="px-4 py-3 font-mono text-xs text-gray-500">#{wager.id}</td>
                <td className="px-4 py-3">{wager.customerLoginName}</td>
                <td className="px-4 py-3">{wager.wagerType}</td>
                <td className="px-4 py-3 text-right font-medium">${wager.riskAmount.toFixed(2)}</td>
                <td className="px-4 py-3 text-right text-green-700">${wager.winAmount.toFixed(2)}</td>
                <td className="px-4 py-3"><WagerStatusBadge status={wager.status} /></td>
                <td className="px-4 py-3 text-gray-500">{format(new Date(wager.createdAt), 'MM/dd HH:mm')}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {data?.items.length === 0 && (
          <div className="p-8 text-center text-gray-400">No pending wagers.</div>
        )}
      </div>
    </div>
  )
}
