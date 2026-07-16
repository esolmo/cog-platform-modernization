'use strict';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { getCustomersByAgent } from '../../api/accountsApi';
import type { Agent } from '../../types/accounts';

const PAGE_SIZE = 25;

export default function CustomersTab({ agent }: { agent: Agent }) {
  const [page, setPage] = useState(1);
  const navigate = useNavigate();

  const { data, isLoading } = useQuery({
    queryKey: ['customers-by-agent', agent.id, page],
    queryFn:  () => getCustomersByAgent(agent.id, page, PAGE_SIZE),
  });

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-sm font-semibold text-gray-700">
          Customers <span className="text-gray-400 font-normal">({data?.totalCount ?? 0})</span>
        </h2>
      </div>

      {isLoading && <p className="text-sm text-gray-500">Loading…</p>}

      {data && data.items.length === 0 && (
        <p className="text-sm text-gray-500 text-center py-8">No customers assigned to this agent.</p>
      )}

      {data && data.items.length > 0 && (
        <>
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Login</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Alt Login</th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Status</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Credit Limit</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Balance</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Available</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Odds Format</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-100">
                {data.items.map((c) => (
                  <tr
                    key={c.id}
                    onClick={() => navigate(`/customers/${c.id}`)}
                    className="hover:bg-blue-50 cursor-pointer"
                  >
                    <td className="px-4 py-3 text-sm font-medium text-blue-700">{c.loginName}</td>
                    <td className="px-4 py-3 text-sm text-gray-500">{c.alternateLoginName ?? '—'}</td>
                    <td className="px-4 py-3 text-center">
                      <span
                        className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                          c.status === 'Active'
                            ? 'bg-green-100 text-green-700'
                            : c.status === 'Suspended'
                            ? 'bg-yellow-100 text-yellow-700'
                            : 'bg-red-100 text-red-700'
                        }`}
                      >
                        {c.status}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-700 text-right">
                      {c.balance.creditLimit.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-700 text-right">
                      {c.balance.currentBalance.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 2 })}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-700 text-right">
                      {c.balance.availableCredit.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 2 })}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600">{c.oddsFormat}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>
              Page {data.page} of {data.totalPages} &middot; {data.totalCount} total
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage((p) => p - 1)}
                disabled={!data.hasPrevious}
                className="px-3 py-1.5 rounded border border-gray-300 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
              >
                &larr; Prev
              </button>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={!data.hasNext}
                className="px-3 py-1.5 rounded border border-gray-300 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
              >
                Next &rarr;
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
