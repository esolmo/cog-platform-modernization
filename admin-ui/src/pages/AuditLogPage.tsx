import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { apiClient } from '../api/client';

interface AuditLogDto {
  id: number;
  userId: number;
  username: string;
  action: string;
  entityType: string;
  entityId: string;
  oldValues: string | null;
  newValues: string | null;
  ipAddress: string;
  occurredAt: string;
}

interface AuditLogPagedResult {
  items: AuditLogDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export default function AuditLogPage() {
  const [page, setPage] = useState(1);
  const [actionFilter, setActionFilter] = useState('');
  const [expandedId, setExpandedId] = useState<number | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['audit-logs', page, actionFilter],
    queryFn: () =>
      apiClient.get<AuditLogPagedResult>('/config/audit', {
        params: { page, pageSize: 50, action: actionFilter || undefined },
      }).then((r) => r.data),
  });

  const actionBadgeColor = (action: string) => {
    if (action.endsWith('.create')) return 'bg-green-100 text-green-700';
    if (action.endsWith('.delete')) return 'bg-red-100 text-red-700';
    if (action.endsWith('.update') || action.endsWith('.upsert')) return 'bg-blue-100 text-blue-700';
    return 'bg-gray-100 text-gray-700';
  };

  return (
    <div>
      <div className="mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Audit Log</h2>
        <p className="text-sm text-gray-500 mt-1">Complete record of administrative actions</p>
      </div>

      <div className="mb-4">
        <input
          type="search"
          placeholder="Filter by action (e.g. user.create)..."
          value={actionFilter}
          onChange={(e) => { setActionFilter(e.target.value); setPage(1); }}
          className="w-full max-w-sm border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
        />
      </div>

      {isLoading ? (
        <p className="text-gray-500">Loading...</p>
      ) : (
        <>
          <div className="bg-white rounded-xl shadow overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">When</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">User</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">Action</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">Entity</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">IP</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">Details</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {data?.items.map((log) => (
                  <>
                    <tr key={log.id} className="hover:bg-gray-50">
                      <td className="px-4 py-3 text-gray-500 whitespace-nowrap">
                        {new Date(log.occurredAt).toLocaleString()}
                      </td>
                      <td className="px-4 py-3 font-medium text-gray-900">{log.username}</td>
                      <td className="px-4 py-3">
                        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${actionBadgeColor(log.action)}`}>
                          {log.action}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-gray-600">{log.entityType} #{log.entityId}</td>
                      <td className="px-4 py-3 text-gray-500 font-mono text-xs">{log.ipAddress}</td>
                      <td className="px-4 py-3">
                        {(log.oldValues || log.newValues) && (
                          <button
                            onClick={() => setExpandedId(expandedId === log.id ? null : log.id)}
                            className="text-indigo-600 hover:underline text-xs"
                          >
                            {expandedId === log.id ? 'Hide' : 'Show diff'}
                          </button>
                        )}
                      </td>
                    </tr>
                    {expandedId === log.id && (
                      <tr key={`${log.id}-detail`} className="bg-gray-50">
                        <td colSpan={6} className="px-4 py-3">
                          <div className="grid grid-cols-2 gap-4 text-xs font-mono">
                            <div>
                              <p className="font-semibold text-gray-600 mb-1">Before</p>
                              <pre className="bg-white border rounded p-2 overflow-auto max-h-32">
                                {log.oldValues ? JSON.stringify(JSON.parse(log.oldValues), null, 2) : '(none)'}
                              </pre>
                            </div>
                            <div>
                              <p className="font-semibold text-gray-600 mb-1">After</p>
                              <pre className="bg-white border rounded p-2 overflow-auto max-h-32">
                                {log.newValues ? JSON.stringify(JSON.parse(log.newValues), null, 2) : '(none)'}
                              </pre>
                            </div>
                          </div>
                        </td>
                      </tr>
                    )}
                  </>
                ))}
              </tbody>
            </table>
          </div>

          {data && data.totalPages > 1 && (
            <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
              <span>{data.totalCount} entries total</span>
              <div className="flex gap-2">
                <button onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1} className="px-3 py-1 border rounded disabled:opacity-40">
                  Previous
                </button>
                <span className="px-3 py-1">Page {data.page} of {data.totalPages}</span>
                <button onClick={() => setPage((p) => p + 1)} disabled={page >= data.totalPages} className="px-3 py-1 border rounded disabled:opacity-40">
                  Next
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
