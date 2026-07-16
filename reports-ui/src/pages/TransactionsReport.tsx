import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  getSortedRowModel,
  SortingState,
  useReactTable,
} from '@tanstack/react-table';
import { reportsApi, ChangedTransactionRow } from '../api/reports';
import { format, subDays } from 'date-fns';

const columnHelper = createColumnHelper<ChangedTransactionRow>();

const columns = [
  columnHelper.accessor('loginName', { header: 'Player', size: 120 }),
  columnHelper.accessor('updatedDateTime', {
    header: 'Updated',
    cell: (info) => format(new Date(info.getValue()), 'MMM d, yyyy HH:mm'),
  }),
  columnHelper.accessor('transactionType', { header: 'Type', size: 100 }),
  columnHelper.accessor('description', { header: 'Description' }),
  columnHelper.accessor('amount', {
    header: 'Amount',
    cell: (info) => {
      const v = info.getValue();
      return (
        <span className={v < 0 ? 'text-red-600' : 'text-green-600'}>
          {v < 0 ? '-' : '+'}${Math.abs(v).toFixed(2)}
        </span>
      );
    },
    size: 110,
  }),
  columnHelper.accessor('reference', { header: 'Reference', size: 130 }),
];

export default function TransactionsReport() {
  const [agentId, setAgentId] = useState('');
  const [customerId, setCustomerId] = useState('0');
  const [from, setFrom] = useState(format(subDays(new Date(), 7), 'yyyy-MM-dd'));
  const [to, setTo] = useState(format(new Date(), 'yyyy-MM-dd'));
  const [includeAgent, setIncludeAgent] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [sorting, setSorting] = useState<SortingState>([]);

  const { data = [], isLoading, error } = useQuery({
    queryKey: ['changed-transactions', agentId, customerId, from, to, includeAgent],
    queryFn: () =>
      reportsApi.getChangedTransactions(
        Number(agentId), Number(customerId), from, to, includeAgent
      ),
    enabled: submitted && !!agentId,
  });

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    state: { sorting },
    onSortingChange: setSorting,
  });

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold text-gray-800">Changed Transactions</h2>
        <p className="text-sm text-gray-500 mt-1">Adjusted / corrected customer transactions</p>
      </div>

      <div className="bg-white rounded-lg border border-gray-200 p-5">
        <div className="grid grid-cols-3 gap-4 mb-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Agent ID</label>
            <input
              type="number"
              value={agentId}
              onChange={(e) => setAgentId(e.target.value)}
              className="border border-gray-300 rounded-md px-3 py-2 text-sm w-full"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Customer ID (0 = all)
            </label>
            <input
              type="number"
              value={customerId}
              onChange={(e) => setCustomerId(e.target.value)}
              className="border border-gray-300 rounded-md px-3 py-2 text-sm w-full"
            />
          </div>
          <div className="flex items-end">
            <label className="flex items-center gap-2 cursor-pointer pb-2">
              <input
                type="checkbox"
                checked={includeAgent}
                onChange={(e) => setIncludeAgent(e.target.checked)}
                className="h-4 w-4 text-slate-600"
              />
              <span className="text-sm text-gray-700">Include agent transactions</span>
            </label>
          </div>
        </div>
        <div className="flex gap-4 items-end">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">From</label>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)}
              className="border border-gray-300 rounded-md px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">To</label>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)}
              className="border border-gray-300 rounded-md px-3 py-2 text-sm" />
          </div>
          <button
            onClick={() => setSubmitted(true)}
            disabled={!agentId || isLoading}
            className="bg-slate-700 text-white px-5 py-2 rounded-md text-sm font-medium hover:bg-slate-800 disabled:opacity-50 transition-colors"
          >
            {isLoading ? 'Loading...' : 'Run Report'}
          </button>
        </div>
      </div>

      {error && (
        <div className="p-4 bg-red-100 text-red-700 rounded-lg border border-red-200 text-sm">
          Failed to load transactions.
        </div>
      )}

      {submitted && data.length > 0 && (
        <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-100 flex items-center justify-between">
            <span className="text-sm font-medium text-gray-700">{data.length} records</span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 text-gray-500 text-xs uppercase">
                {table.getHeaderGroups().map((hg) => (
                  <tr key={hg.id}>
                    {hg.headers.map((h) => (
                      <th
                        key={h.id}
                        className="px-4 py-3 text-left cursor-pointer hover:bg-gray-100"
                        onClick={h.column.getToggleSortingHandler()}
                      >
                        {flexRender(h.column.columnDef.header, h.getContext())}
                        {h.column.getIsSorted() === 'asc' ? ' ↑' : h.column.getIsSorted() === 'desc' ? ' ↓' : ''}
                      </th>
                    ))}
                  </tr>
                ))}
              </thead>
              <tbody>
                {table.getRowModel().rows.map((row) => (
                  <tr key={row.id} className="border-t border-gray-50 hover:bg-gray-50">
                    {row.getVisibleCells().map((cell) => (
                      <td key={cell.id} className="px-4 py-3">
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </td>
                    ))}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {submitted && !isLoading && data.length === 0 && (
        <div className="text-center py-12 text-gray-500">No changed transactions found.</div>
      )}
    </div>
  );
}
