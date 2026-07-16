'use strict';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { getAgentPositionSummary } from '../api/accountsApi';
import type { AgentPositionSummary } from '../types/accounts';

const fmtMoney = (n: number) =>
  n.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 2 });

const typeColour = (t: string) =>
  ({ Master: 'text-purple-700', Agent: 'text-blue-700', SubAgent: 'text-gray-500' }[t] ?? 'text-gray-500');

export default function PositionPage() {
  const { data: agents = [], isLoading, refetch, isFetching } = useQuery({
    queryKey: ['agent-position-summary'],
    queryFn:  getAgentPositionSummary,
    staleTime: 60_000,
  });

  const totalBalance  = agents.reduce((s, a) => s + a.totalBalance,  0);
  const totalPending  = agents.reduce((s, a) => s + a.totalPending,  0);
  const totalCredit   = agents.reduce((s, a) => s + a.totalCreditLimit, 0);
  const totalAvail    = agents.reduce((s, a) => s + a.totalAvailableCredit, 0);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Position Report</h1>
          <p className="text-sm text-gray-500 mt-0.5">{agents.length} active agents</p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isFetching}
          className="px-4 py-2 text-sm bg-white border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 disabled:opacity-50"
        >
          {isFetching ? 'Refreshing…' : 'Refresh'}
        </button>
      </div>

      {/* Portfolio summary */}
      {!isLoading && agents.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          {[
            { label: 'Total Balance',     value: totalBalance,  red: totalBalance < 0 },
            { label: 'Pending Wagers',    value: totalPending,  red: false },
            { label: 'Total Credit Ext.', value: totalCredit,   red: false },
            { label: 'Total Available',   value: totalAvail,    red: totalAvail < 0 },
          ].map(({ label, value, red }) => (
            <div key={label} className="bg-white rounded-lg shadow p-4">
              <p className="text-xs text-gray-500 mb-1">{label}</p>
              <p className={`text-xl font-bold ${red ? 'text-red-600' : 'text-gray-800'}`}>
                {fmtMoney(value)}
              </p>
            </div>
          ))}
        </div>
      )}

      {/* Agents table */}
      {isLoading && <p className="text-sm text-gray-500">Loading…</p>}
      {!isLoading && agents.length === 0 && (
        <p className="text-sm text-gray-500 text-center py-12">No active agents found.</p>
      )}

      {!isLoading && agents.length > 0 && (
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Agent</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Type</th>
                <th className="px-4 py-3 text-center text-xs font-semibold text-gray-500 uppercase tracking-wide">Customers</th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Balance</th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Pending</th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Credit Limit</th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Available</th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Utilisation</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {agents.map((a) => {
                const utilPct = a.totalCreditLimit > 0
                  ? ((a.totalCreditLimit - a.totalAvailableCredit) / a.totalCreditLimit) * 100
                  : 0;
                return (
                  <AgentRow key={a.agentId} agent={a} utilPct={utilPct} />
                );
              })}
            </tbody>
            {/* Totals footer */}
            <tfoot className="bg-gray-50 border-t border-gray-200 font-semibold text-sm">
              <tr>
                <td className="px-4 py-3 text-xs text-gray-500 uppercase" colSpan={3}>Totals</td>
                <td className={`px-4 py-3 text-right tabular-nums ${totalBalance < 0 ? 'text-red-600' : 'text-gray-800'}`}>
                  {fmtMoney(totalBalance)}
                </td>
                <td className="px-4 py-3 text-right tabular-nums text-gray-800">{fmtMoney(totalPending)}</td>
                <td className="px-4 py-3 text-right tabular-nums text-gray-800">{fmtMoney(totalCredit)}</td>
                <td className={`px-4 py-3 text-right tabular-nums ${totalAvail < 0 ? 'text-red-600' : 'text-gray-800'}`}>
                  {fmtMoney(totalAvail)}
                </td>
                <td className="px-4 py-3"></td>
              </tr>
            </tfoot>
          </table>
        </div>
      )}
    </div>
  );
}

function AgentRow({ agent, utilPct }: { agent: AgentPositionSummary; utilPct: number }) {
  const utilColour =
    utilPct >= 90 ? 'text-red-600' :
    utilPct >= 70 ? 'text-yellow-600' :
    'text-green-700';

  return (
    <tr className="hover:bg-gray-50">
      <td className="px-4 py-3">
        <Link
          to={`/agents/${agent.agentId}?tab=figures`}
          className={`font-medium hover:underline ${typeColour(agent.agentType)}`}
        >
          {agent.loginName}
        </Link>
        {agent.name && <span className="text-xs text-gray-400 ml-1">{agent.name}</span>}
      </td>
      <td className="px-4 py-3 text-xs text-gray-500">{agent.agentType}</td>
      <td className="px-4 py-3 text-center text-gray-700">{agent.customerCount}</td>
      <td className={`px-4 py-3 text-right font-medium tabular-nums ${agent.totalBalance < 0 ? 'text-red-600' : 'text-gray-700'}`}>
        {fmtMoney(agent.totalBalance)}
      </td>
      <td className="px-4 py-3 text-right text-gray-700 tabular-nums">{fmtMoney(agent.totalPending)}</td>
      <td className="px-4 py-3 text-right text-gray-700 tabular-nums">{fmtMoney(agent.totalCreditLimit)}</td>
      <td className={`px-4 py-3 text-right font-medium tabular-nums ${agent.totalAvailableCredit < 0 ? 'text-red-600' : 'text-gray-700'}`}>
        {fmtMoney(agent.totalAvailableCredit)}
      </td>
      <td className={`px-4 py-3 text-right font-medium tabular-nums ${utilColour}`}>
        {utilPct.toFixed(1)}%
      </td>
    </tr>
  );
}
