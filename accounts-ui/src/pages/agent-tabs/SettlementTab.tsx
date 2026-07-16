'use strict';
import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  calculateDistribution,
  confirmDistribution,
  getAgentMakeup,
  getDistributionHistory,
} from '../../api/accountsApi';
import type { Agent, AgentDistribution } from '../../types/accounts';

// Next Sunday (week ending)
function nextSundayIso(): string {
  const d = new Date();
  const day = d.getDay(); // 0=Sun
  const diff = day === 0 ? 0 : 7 - day;
  d.setDate(d.getDate() + diff);
  return d.toISOString().split('T')[0];
}

const fmtMoney = (n: number) =>
  n.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 2 });

const fmtDate = (s: string) => new Date(s).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

// ─── Distribution row ─────────────────────────────────────────────────────────

function DistributionRow({
  dist,
  agentId,
  loginName,
}: {
  dist: AgentDistribution;
  agentId: number;
  loginName: string;
}) {
  const qc = useQueryClient();
  const [expanded, setExpanded] = useState(false);

  const { mutate: doConfirm, isPending: confirming } = useMutation({
    mutationFn: () => confirmDistribution(agentId, dist.weekEnding, loginName),
    onSuccess:  () => {
      qc.invalidateQueries({ queryKey: ['distribution-history', agentId] });
      qc.invalidateQueries({ queryKey: ['agent-makeup', agentId] });
    },
  });

  return (
    <>
      <tr
        className="hover:bg-gray-50 cursor-pointer"
        onClick={() => setExpanded((v) => !v)}
      >
        <td className="px-4 py-3 text-sm text-gray-700">{fmtDate(dist.weekEnding)}</td>
        <td className={`px-4 py-3 text-sm text-right font-medium ${dist.netAmount >= 0 ? 'text-green-700' : 'text-red-700'}`}>
          {fmtMoney(dist.netAmount)}
        </td>
        <td className="px-4 py-3 text-sm text-right text-gray-700">{fmtMoney(dist.commissionAmount)}</td>
        <td className="px-4 py-3 text-sm text-right text-gray-700">{dist.activePlayerCount}</td>
        <td className="px-4 py-3 text-sm text-right text-gray-700">{fmtMoney(dist.newBalance)}</td>
        <td className="px-4 py-3 text-center">
          {dist.isConfirmed ? (
            <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">Confirmed</span>
          ) : (
            <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-700">Pending</span>
          )}
        </td>
        <td className="px-4 py-3 text-center">
          {!dist.isConfirmed && (
            <button
              onClick={(e) => { e.stopPropagation(); doConfirm(); }}
              disabled={confirming}
              className="px-3 py-1 text-xs bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50"
            >
              {confirming ? 'Confirming…' : 'Confirm'}
            </button>
          )}
        </td>
      </tr>
      {expanded && (
        <tr className="bg-gray-50">
          <td colSpan={7} className="px-6 py-4">
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Sports Win</p>
                <p className="font-medium text-gray-800">{fmtMoney(dist.winAmount)}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Sports Loss</p>
                <p className="font-medium text-gray-800">{fmtMoney(dist.lossAmount)}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Casino Win</p>
                <p className="font-medium text-gray-800">{fmtMoney(dist.casinoWinAmount)}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Casino Loss</p>
                <p className="font-medium text-gray-800">{fmtMoney(dist.casinoLossAmount)}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Live Dealer Win</p>
                <p className="font-medium text-gray-800">{fmtMoney(dist.liveDealerWin)}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Live Dealer Loss</p>
                <p className="font-medium text-gray-800">{fmtMoney(dist.liveDealerLoss)}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Credit Adj.</p>
                <p className="font-medium text-gray-800">{fmtMoney(dist.creditAdjustments)}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Debit Adj.</p>
                <p className="font-medium text-gray-800">{fmtMoney(dist.debitAdjustments)}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Commission Type</p>
                <p className="font-medium text-gray-800">{dist.commissionType} @ {dist.commissionRate}%</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Prev. Makeup</p>
                <p className="font-medium text-gray-800">{fmtMoney(dist.previousMakeup)}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-0.5">New Makeup</p>
                <p className="font-medium text-gray-800">{fmtMoney(dist.newMakeup)}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500 mb-0.5">Head Count Fee</p>
                <p className="font-medium text-gray-800">{fmtMoney(dist.headCountFee)}</p>
              </div>
            </div>
            {dist.confirmedAt && (
              <p className="text-xs text-gray-400 mt-3">Confirmed at {fmtDate(dist.confirmedAt)}</p>
            )}
          </td>
        </tr>
      )}
    </>
  );
}

// ─── Settlement tab ───────────────────────────────────────────────────────────

export default function SettlementTab({ agent, loginName }: { agent: Agent; loginName: string }) {
  const [weekEnding, setWeekEnding] = useState(nextSundayIso());
  const [weeksBack,  setWeeksBack]  = useState(12);
  const qc = useQueryClient();

  const { data: makeup, isLoading: loadingMakeup } = useQuery({
    queryKey: ['agent-makeup', agent.id],
    queryFn:  () => getAgentMakeup(agent.id),
  });

  const { data: history, isLoading: loadingHistory } = useQuery({
    queryKey: ['distribution-history', agent.id, weeksBack],
    queryFn:  () => getDistributionHistory(agent.id, weeksBack),
  });

  const { mutate: doCalculate, isPending: calculating, error: calcError } = useMutation({
    mutationFn: () => calculateDistribution(agent.id, weekEnding, loginName),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['distribution-history', agent.id] });
      qc.invalidateQueries({ queryKey: ['agent-makeup', agent.id] });
    },
  });

  return (
    <div className="space-y-6">
      {/* Makeup summary */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-lg shadow p-5">
          <p className="text-xs text-gray-500 uppercase tracking-wide mb-1">Current Makeup</p>
          {loadingMakeup ? (
            <p className="text-sm text-gray-400">Loading…</p>
          ) : (
            <>
              <p className={`text-2xl font-bold ${(makeup?.currentMakeup ?? 0) < 0 ? 'text-red-600' : 'text-gray-800'}`}>
                {fmtMoney(makeup?.currentMakeup ?? 0)}
              </p>
              {makeup?.asOfWeekEnding && (
                <p className="text-xs text-gray-400 mt-1">As of week ending {fmtDate(makeup.asOfWeekEnding)}</p>
              )}
            </>
          )}
        </div>

        <div className="bg-white rounded-lg shadow p-5 md:col-span-2">
            <p className="text-xs text-gray-500 uppercase tracking-wide mb-3">Calculate Distribution</p>
            <div className="flex items-end gap-3 flex-wrap">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Week Ending (Sunday)</label>
                <input
                  type="date"
                  value={weekEnding}
                  onChange={(e) => setWeekEnding(e.target.value)}
                  className="border border-gray-300 rounded px-3 py-1.5 text-sm"
                />
              </div>
              <button
                onClick={() => doCalculate()}
                disabled={calculating || !weekEnding}
                className="px-4 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
              >
                {calculating ? 'Calculating…' : 'Calculate'}
              </button>
            </div>
            {calcError && <p className="text-xs text-red-500 mt-2">{String(calcError)}</p>}
          </div>
      </div>

      {/* Distribution history */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <h2 className="text-sm font-semibold text-gray-700">Distribution History</h2>
          <select
            value={weeksBack}
            onChange={(e) => setWeeksBack(parseInt(e.target.value, 10))}
            className="text-xs border border-gray-300 rounded px-2 py-1"
          >
            <option value={4}>Last 4 weeks</option>
            <option value={8}>Last 8 weeks</option>
            <option value={12}>Last 12 weeks</option>
            <option value={26}>Last 26 weeks</option>
            <option value={52}>Last 52 weeks</option>
          </select>
        </div>

        {loadingHistory && <p className="text-sm text-gray-500">Loading…</p>}

        {history && history.length === 0 && (
          <p className="text-sm text-gray-500 text-center py-8">No distribution history found.</p>
        )}

        {history && history.length > 0 && (
          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Week Ending</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Net</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Commission</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Players</th>
                  <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">New Balance</th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Status</th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Action</th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-100">
                {history.map((dist) => (
                  <DistributionRow
                    key={dist.id}
                    dist={dist}
                    agentId={agent.id}
                    loginName={loginName}
                  />
                ))}
              </tbody>
            </table>
            <p className="text-xs text-gray-400 px-4 py-2">Click a row to expand details.</p>
          </div>
        )}
      </div>
    </div>
  );
}
