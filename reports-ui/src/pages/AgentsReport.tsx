import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  useReactTable,
} from '@tanstack/react-table';
import { reportsApi, AgentRow, CustomerRow } from '../api/reports';

const agentColumnHelper = createColumnHelper<AgentRow>();
const agentColumns = [
  agentColumnHelper.accessor('id', { header: 'ID', size: 70 }),
  agentColumnHelper.accessor('loginName', { header: 'Login' }),
  agentColumnHelper.accessor('fullName', { header: 'Full Name' }),
  agentColumnHelper.accessor('email', { header: 'Email' }),
  agentColumnHelper.accessor('isActive', {
    header: 'Active',
    cell: (info) => (
      <span className={info.getValue() ? 'text-green-600' : 'text-red-500'}>
        {info.getValue() ? 'Yes' : 'No'}
      </span>
    ),
    size: 80,
  }),
];

const customerColumnHelper = createColumnHelper<CustomerRow>();
const customerColumns = [
  customerColumnHelper.accessor('id', { header: 'ID', size: 70 }),
  customerColumnHelper.accessor('loginName', { header: 'Login' }),
  customerColumnHelper.accessor('fullName', { header: 'Full Name' }),
  customerColumnHelper.accessor('balance', {
    header: 'Balance',
    cell: (info) => `$${info.getValue().toFixed(2)}`,
    size: 110,
  }),
];

export default function AgentsReport() {
  const [agentId, setAgentId] = useState('');
  const [search, setSearch] = useState('');
  const [mode, setMode] = useState<'agents' | 'customers'>('agents');
  const [submitted, setSubmitted] = useState(false);

  const { data: agentData = [], isLoading: agentsLoading } = useQuery({
    queryKey: ['agents', agentId, search],
    queryFn: () => reportsApi.searchAgents(Number(agentId), search),
    enabled: submitted && !!agentId && mode === 'agents',
  });

  const { data: customerData = [], isLoading: customersLoading } = useQuery({
    queryKey: ['customers', agentId, search],
    queryFn: () => reportsApi.searchCustomers(Number(agentId), search),
    enabled: submitted && !!agentId && mode === 'customers',
  });

  const agentTable = useReactTable({
    data: agentData,
    columns: agentColumns,
    getCoreRowModel: getCoreRowModel(),
  });

  const customerTable = useReactTable({
    data: customerData,
    columns: customerColumns,
    getCoreRowModel: getCoreRowModel(),
  });

  const isLoading = mode === 'agents' ? agentsLoading : customersLoading;
  const table = mode === 'agents' ? agentTable : customerTable;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold text-gray-800">Agents &amp; Customers</h2>
        <p className="text-sm text-gray-500 mt-1">Search within agent hierarchy</p>
      </div>

      <div className="bg-white rounded-lg border border-gray-200 p-5">
        <div className="flex gap-4 items-end flex-wrap">
          <div>
            <label htmlFor="agents-agent-id" className="block text-sm font-medium text-gray-700 mb-1">Agent ID</label>
            <input
              id="agents-agent-id"
              type="number"
              value={agentId}
              onChange={(e) => setAgentId(e.target.value)}
              className="border border-gray-300 rounded-md px-3 py-2 text-sm w-32"
            />
          </div>
          <div>
            <label htmlFor="agents-search" className="block text-sm font-medium text-gray-700 mb-1">Search</label>
            <input
              id="agents-search"
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Login prefix..."
              className="border border-gray-300 rounded-md px-3 py-2 text-sm w-44"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Search for</label>
            <div className="flex gap-3">
              <label className="flex items-center gap-1 cursor-pointer text-sm">
                <input type="radio" value="agents" checked={mode === 'agents'}
                  onChange={() => setMode('agents')} className="text-slate-600" />
                Agents
              </label>
              <label className="flex items-center gap-1 cursor-pointer text-sm">
                <input type="radio" value="customers" checked={mode === 'customers'}
                  onChange={() => setMode('customers')} className="text-slate-600" />
                Customers
              </label>
            </div>
          </div>
          <button
            onClick={() => setSubmitted(true)}
            disabled={!agentId || isLoading}
            className="bg-slate-700 text-white px-5 py-2 rounded-md text-sm font-medium hover:bg-slate-800 disabled:opacity-50 transition-colors"
          >
            {isLoading ? 'Searching...' : 'Search'}
          </button>
        </div>
      </div>

      {submitted && (agentData.length > 0 || customerData.length > 0) && (
        <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-100">
            <span className="text-sm font-medium text-gray-700">
              {mode === 'agents' ? agentData.length : customerData.length} results
            </span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 text-gray-500 text-xs uppercase">
                {table.getHeaderGroups().map((hg) => (
                  <tr key={hg.id}>
                    {hg.headers.map((h) => (
                      <th key={h.id} className="px-4 py-3 text-left">
                        {flexRender(h.column.columnDef.header, h.getContext())}
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

      {submitted && !isLoading &&
        (mode === 'agents' ? agentData : customerData).length === 0 && (
        <div className="text-center py-12 text-gray-500">No results found.</div>
      )}
    </div>
  );
}
