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
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { reportsApi, WagerActivityRow } from '../api/reports';
import { format, subDays } from 'date-fns';

const columnHelper = createColumnHelper<WagerActivityRow>();

const columns = [
  columnHelper.accessor('documentNumber', { header: 'Document #', size: 130 }),
  columnHelper.accessor('tranDateTime', {
    header: 'Date/Time',
    cell: (info) => format(new Date(info.getValue()), 'MMM d, yyyy HH:mm'),
  }),
  columnHelper.accessor('tranType', { header: 'Type', size: 100 }),
  columnHelper.accessor('description', { header: 'Description' }),
  columnHelper.accessor('amount', {
    header: 'Amount',
    cell: (info) => `$${info.getValue().toFixed(2)}`,
    size: 100,
  }),
  columnHelper.accessor('gradeNum', { header: 'Grade', size: 80 }),
];

export default function WagersReport() {
  const [loginId, setLoginId] = useState('');
  const [from, setFrom] = useState(format(subDays(new Date(), 7), 'yyyy-MM-dd'));
  const [to, setTo] = useState(format(new Date(), 'yyyy-MM-dd'));
  const [submitted, setSubmitted] = useState(false);
  const [sorting, setSorting] = useState<SortingState>([]);

  const { data = [], isLoading, error } = useQuery({
    queryKey: ['wager-activity', loginId, from, to],
    queryFn: () => reportsApi.getWagerActivity(loginId, from, to),
    enabled: submitted && !!loginId,
  });

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    state: { sorting },
    onSortingChange: setSorting,
  });

  // Aggregate daily totals for chart
  const chartData = Object.entries(
    data.reduce<Record<string, number>>((acc, row) => {
      const day = format(new Date(row.tranDateTime), 'MMM d');
      acc[day] = (acc[day] ?? 0) + row.amount;
      return acc;
    }, {})
  ).map(([date, total]) => ({ date, total }));

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold text-gray-800">Wager Activity</h2>
        <p className="text-sm text-gray-500 mt-1">Replaces BetMaker Activity report</p>
      </div>

      <div className="bg-white rounded-lg border border-gray-200 p-5">
        <div className="flex gap-4 items-end">
          <div>
            <label htmlFor="wagers-login-id" className="block text-sm font-medium text-gray-700 mb-1">Login ID</label>
            <input
              id="wagers-login-id"
              type="text"
              value={loginId}
              onChange={(e) => setLoginId(e.target.value)}
              placeholder="Ticket writer login"
              className="border border-gray-300 rounded-md px-3 py-2 text-sm w-48"
            />
          </div>
          <div>
            <label htmlFor="wagers-from" className="block text-sm font-medium text-gray-700 mb-1">From</label>
            <input
              id="wagers-from"
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
              className="border border-gray-300 rounded-md px-3 py-2 text-sm"
            />
          </div>
          <div>
            <label htmlFor="wagers-to" className="block text-sm font-medium text-gray-700 mb-1">To</label>
            <input
              id="wagers-to"
              type="date"
              value={to}
              onChange={(e) => setTo(e.target.value)}
              className="border border-gray-300 rounded-md px-3 py-2 text-sm"
            />
          </div>
          <button
            onClick={() => setSubmitted(true)}
            disabled={!loginId || isLoading}
            className="bg-slate-700 text-white px-5 py-2 rounded-md text-sm font-medium hover:bg-slate-800 disabled:opacity-50 transition-colors"
          >
            {isLoading ? 'Loading...' : 'Run Report'}
          </button>
        </div>
      </div>

      {error && (
        <div className="p-4 bg-red-100 text-red-700 rounded-lg border border-red-200 text-sm">
          Failed to load report.
        </div>
      )}

      {submitted && data.length > 0 && (
        <>
          <div className="bg-white rounded-lg border border-gray-200 p-5">
            <h3 className="text-sm font-semibold text-gray-600 mb-4">Daily Volume</h3>
            <ResponsiveContainer width="100%" height={200}>
              <BarChart data={chartData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip formatter={(v: number) => `$${v.toFixed(2)}`} />
                <Bar dataKey="total" fill="#475569" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
            <div className="px-5 py-3 border-b border-gray-100 flex items-center justify-between">
              <span className="text-sm font-medium text-gray-700">{data.length} records</span>
              <span className="text-sm font-bold text-gray-900">
                Total: ${data.reduce((s, r) => s + r.amount, 0).toFixed(2)}
              </span>
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
        </>
      )}

      {submitted && !isLoading && data.length === 0 && (
        <div className="text-center py-12 text-gray-500">No results found for this period.</div>
      )}
    </div>
  );
}
