import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { getCustomers, getCustomersByAgent } from '../api/accountsApi';
import { useAuthStore } from '../stores/authStore';
import type { Customer } from '../types/accounts';

function StatusBadge({ status }: { status: Customer['status'] }) {
  const colours = {
    Active:    'bg-green-100 text-green-800',
    Inactive:  'bg-gray-100 text-gray-600',
    Suspended: 'bg-red-100 text-red-700',
  };
  return (
    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${colours[status]}`}>
      {status}
    </span>
  );
}

export default function CustomerListPage() {
  const agentId = useAuthStore((s) => s.agentId) ?? 0;
  const roles   = useAuthStore((s) => s.roles);
  const canSeeAllCustomers = roles.includes('Admin') || roles.includes('MasterAgent');

  const [page, setPage]     = useState(1);
  const [search, setSearch] = useState('');
  const pageSize            = 25;

  const { data, isLoading, isError } = useQuery({
    queryKey: canSeeAllCustomers ? ['customers', 'all', search, page] : ['customers', agentId, page],
    queryFn:  () =>
      canSeeAllCustomers
        ? getCustomers(search, page, pageSize)
        : getCustomersByAgent(agentId, page, pageSize),
    enabled:  canSeeAllCustomers || agentId > 0,
  });

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-xl font-semibold text-gray-800">Customers</h1>
        <Link
          to="/customers/new"
          className="bg-blue-600 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-blue-700"
        >
          New Customer
        </Link>
      </div>

      {canSeeAllCustomers && (
        <input
          type="text"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          placeholder="Search by login name..."
          className="w-full max-w-sm mb-4 border border-gray-300 rounded-md px-3 py-2 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none"
        />
      )}

      {isLoading && (
        <p className="text-sm text-gray-500">Loading...</p>
      )}

      {isError && (
        <p className="text-sm text-red-500">Failed to load customers.</p>
      )}

      {data && data.items.length === 0 && (
        <p className="text-sm text-gray-500">No customers found.</p>
      )}

      {data && data.items.length > 0 && (
        <>
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">Login</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">Alternate</th>
                  <th className="px-4 py-3 text-right font-medium text-gray-600">Credit Limit</th>
                  <th className="px-4 py-3 text-right font-medium text-gray-600">Balance</th>
                  <th className="px-4 py-3 text-right font-medium text-gray-600">Available</th>
                  <th className="px-4 py-3 text-center font-medium text-gray-600">Status</th>
                  <th className="px-4 py-3"></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {data.items.map((customer) => (
                  <tr key={customer.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium text-gray-900">{customer.loginName}</td>
                    <td className="px-4 py-3 text-gray-500">{customer.alternateLoginName ?? '—'}</td>
                    <td className="px-4 py-3 text-right text-gray-700">
                      ${customer.balance.creditLimit.toFixed(2)}
                    </td>
                    <td className="px-4 py-3 text-right text-gray-700">
                      ${customer.balance.currentBalance.toFixed(2)}
                    </td>
                    <td className="px-4 py-3 text-right text-gray-700">
                      ${customer.balance.availableCredit.toFixed(2)}
                    </td>
                    <td className="px-4 py-3 text-center">
                      <StatusBadge status={customer.status} />
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Link
                        to={`/customers/${customer.id}`}
                        className="text-blue-600 hover:text-blue-800 text-xs font-medium"
                      >
                        View
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>
              {data.totalCount} customer{data.totalCount !== 1 ? 's' : ''} total
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={!data.hasPrevious}
                className="px-3 py-1 rounded border border-gray-300 disabled:opacity-40"
              >
                Previous
              </button>
              <span className="px-3 py-1">
                Page {data.page} of {data.totalPages}
              </span>
              <button
                onClick={() => setPage((p) => p + 1)}
                disabled={!data.hasNext}
                className="px-3 py-1 rounded border border-gray-300 disabled:opacity-40"
              >
                Next
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
