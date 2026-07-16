'use strict';
import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getAgentPosition, getAgentFigures } from '../../api/accountsApi';
import type { Agent } from '../../types/accounts';

const fmtMoney = (n: number) =>
  n.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 2 });

const fmtDate = (s: string) =>
  new Date(s).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

function mondayOfCurrentWeek(): string {
  const d = new Date();
  const day = d.getDay();
  d.setDate(d.getDate() - (day === 0 ? 6 : day - 1));
  return d.toISOString().split('T')[0];
}

function todayIso(): string {
  return new Date().toISOString().split('T')[0];
}

export default function FiguresTab({ agent }: { agent: Agent }) {
  const [from, setFrom] = useState(mondayOfCurrentWeek());
  const [to,   setTo]   = useState(todayIso());

  const { data: position, isLoading: loadingPos } = useQuery({
    queryKey: ['agent-position', agent.id],
    queryFn:  () => getAgentPosition(agent.id),
    staleTime: 60_000,
  });

  const { data: figures, isLoading: loadingFig, refetch } = useQuery({
    queryKey: ['agent-figures', agent.id, from, to],
    queryFn:  () => getAgentFigures(agent.id, from, to),
  });

  return (
    <div className="space-y-6">
      {/* Position cards */}
      <div>
        <h2 className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-3">Current Position</h2>
        {loadingPos ? (
          <p className="text-sm text-gray-400">Loading…</p>
        ) : position ? (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {[
              { label: 'Outstanding Balance',  value: position.totalCurrentBalance,   red: position.totalCurrentBalance < 0 },
              { label: 'Pending Wagers',        value: position.totalPendingWager,      red: false },
              { label: 'Free Play Outstanding', value: position.totalFreePlay,          red: false },
              { label: 'Available Credit',      value: position.totalAvailableCredit,   red: position.totalAvailableCredit < 0 },
            ].map(({ label, value, red }) => (
              <div key={label} className="bg-white rounded-lg shadow p-4">
                <p className="text-xs text-gray-500 mb-1">{label}</p>
                <p className={`text-lg font-semibold ${red ? 'text-red-600' : 'text-gray-800'}`}>
                  {fmtMoney(value)}
                </p>
              </div>
            ))}
          </div>
        ) : null}
        {position && (
          <p className="text-xs text-gray-400 mt-2">
            {position.activeCustomers} active / {position.totalCustomers} total customers
            &nbsp;&middot;&nbsp;
            {position.totalPendingWagerCount} pending wager{position.totalPendingWagerCount !== 1 ? 's' : ''}
            &nbsp;&middot;&nbsp;
            As of {fmtDate(position.asOf)}
          </p>
        )}
      </div>

      {/* Figures date range */}
      <div className="bg-white rounded-lg shadow p-5">
        <div className="flex items-end gap-3 flex-wrap mb-4">
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">From</label>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)}
              className="border border-gray-300 rounded px-3 py-1.5 text-sm" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">To</label>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)}
              className="border border-gray-300 rounded px-3 py-1.5 text-sm" />
          </div>
          <button onClick={() => refetch()}
            className="px-4 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700">
            Refresh
          </button>
        </div>

        {/* Period summary */}
        {loadingFig && <p className="text-sm text-gray-400">Loading figures…</p>}
        {figures && (
          <>
            <div className="grid grid-cols-2 md:grid-cols-3 gap-4 mb-5">
              {[
                { label: 'Total Credits',      value: figures.totalCredits,      green: true },
                { label: 'Total Debits',       value: figures.totalDebits,       green: false },
                { label: 'Net',                value: figures.netTransactions,   green: figures.netTransactions >= 0 },
                { label: 'Casino Adjustments', value: figures.casinoAdjustments, green: false },
                { label: 'Free Play Issued',   value: figures.freePlayIssued,    green: false },
              ].map(({ label, value, green }) => (
                <div key={label}>
                  <p className="text-xs text-gray-500 mb-0.5">{label}</p>
                  <p className={`text-base font-semibold ${green ? 'text-green-700' : 'text-gray-800'}`}>
                    {fmtMoney(value)}
                  </p>
                </div>
              ))}
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Transactions</p>
                <p className="text-base font-semibold text-gray-800">{figures.transactionCount}</p>
              </div>
            </div>

            {/* Per-customer breakdown */}
            {figures.customers.length > 0 && (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-100 text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Customer</th>
                      <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Credits</th>
                      <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Debits</th>
                      <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Net</th>
                      <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Casino</th>
                      <th className="px-3 py-2 text-right text-xs font-medium text-gray-500 uppercase">Balance</th>
                      <th className="px-3 py-2 text-center text-xs font-medium text-gray-500 uppercase">Txns</th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-50">
                    {figures.customers.map((c) => (
                      <tr key={c.customerId} className="hover:bg-gray-50">
                        <td className="px-3 py-2 text-blue-700 font-medium">{c.loginName}</td>
                        <td className="px-3 py-2 text-right text-green-700 tabular-nums">{fmtMoney(c.credits)}</td>
                        <td className="px-3 py-2 text-right text-gray-700 tabular-nums">{fmtMoney(c.debits)}</td>
                        <td className={`px-3 py-2 text-right font-medium tabular-nums ${c.net >= 0 ? 'text-green-700' : 'text-red-600'}`}>
                          {fmtMoney(c.net)}
                        </td>
                        <td className="px-3 py-2 text-right text-gray-600 tabular-nums">{fmtMoney(c.casinoAdj)}</td>
                        <td className={`px-3 py-2 text-right font-medium tabular-nums ${c.currentBalance < 0 ? 'text-red-600' : 'text-gray-700'}`}>
                          {fmtMoney(c.currentBalance)}
                        </td>
                        <td className="px-3 py-2 text-center text-gray-500">{c.txCount}</td>
                      </tr>
                    ))}
                    {/* Totals row */}
                    <tr className="bg-gray-50 font-semibold">
                      <td className="px-3 py-2 text-xs text-gray-500 uppercase">Total</td>
                      <td className="px-3 py-2 text-right text-green-700 tabular-nums">{fmtMoney(figures.totalCredits)}</td>
                      <td className="px-3 py-2 text-right text-gray-700 tabular-nums">{fmtMoney(figures.totalDebits)}</td>
                      <td className={`px-3 py-2 text-right tabular-nums ${figures.netTransactions >= 0 ? 'text-green-700' : 'text-red-600'}`}>
                        {fmtMoney(figures.netTransactions)}
                      </td>
                      <td className="px-3 py-2 text-right text-gray-600 tabular-nums">{fmtMoney(figures.casinoAdjustments)}</td>
                      <td className="px-3 py-2"></td>
                      <td className="px-3 py-2 text-center text-gray-500">{figures.transactionCount}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            )}

            {figures.customers.length === 0 && (
              <p className="text-sm text-gray-500 text-center py-6">No transactions in this period.</p>
            )}
          </>
        )}
      </div>
    </div>
  );
}
