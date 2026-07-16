'use strict';
import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  listAgents,
  getAgentMakeup,
  calculateDistribution,
  confirmDistribution,
} from '../api/accountsApi';
import { useAuthStore } from '../stores/authStore';
import type { Agent, AgentDistribution } from '../types/accounts';

// ─── Helpers ──────────────────────────────────────────────────────────────────

function nextSundayIso(): string {
  const d = new Date();
  const diff = d.getDay() === 0 ? 0 : 7 - d.getDay();
  d.setDate(d.getDate() + diff);
  return d.toISOString().split('T')[0];
}

const fmtMoney = (n: number) =>
  n.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 2 });

const fmtDate = (s: string) =>
  new Date(s).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

const typeColour = (t: string) =>
  ({ Master: 'text-purple-700', Agent: 'text-blue-700', SubAgent: 'text-gray-500' }[t] ?? 'text-gray-500');

// ─── Agent row ────────────────────────────────────────────────────────────────

function AgentRow({
  agent,
  weekEnding,
  loginName,
}: {
  agent: Agent;
  weekEnding: string;
  loginName: string;
}) {
  const qc = useQueryClient();
  const [result, setResult] = useState<AgentDistribution | null>(null);

  const { data: makeup } = useQuery({
    queryKey: ['agent-makeup', agent.id],
    queryFn:  () => getAgentMakeup(agent.id),
    staleTime: 60_000,
  });

  const calcMutation = useMutation({
    mutationFn: () => calculateDistribution(agent.id, weekEnding, loginName),
    onSuccess:  (dist) => {
      setResult(dist);
      qc.invalidateQueries({ queryKey: ['agent-makeup', agent.id] });
    },
  });

  const confirmMutation = useMutation({
    mutationFn: () => confirmDistribution(agent.id, weekEnding, loginName),
    onSuccess:  (dist) => {
      setResult(dist);
      qc.invalidateQueries({ queryKey: ['agent-makeup', agent.id] });
    },
  });

  const isConfirmed  = result?.isConfirmed ?? false;
  const isCalculated = result != null;
  const anyPending   = calcMutation.isPending || confirmMutation.isPending;

  return (
    <tr className="hover:bg-gray-50">
      <td className="px-4 py-3">
        <Link
          to={`/agents/${agent.id}`}
          className={`text-sm font-medium hover:underline ${typeColour(agent.agentType)}`}
        >
          {agent.loginName}
        </Link>
        {agent.name && <span className="text-xs text-gray-400 ml-1">{agent.name}</span>}
      </td>
      <td className="px-4 py-3 text-xs text-gray-500">{agent.agentType}</td>
      <td className="px-4 py-3 text-xs text-gray-500">
        {agent.commissionType.replace(/([A-Z])/g, ' $1').trim()} {agent.commissionRate}%
      </td>
      <td className={`px-4 py-3 text-sm text-right font-medium tabular-nums ${
        (makeup?.currentMakeup ?? 0) < 0 ? 'text-red-600' : 'text-gray-700'
      }`}>
        {makeup ? fmtMoney(makeup.currentMakeup) : '\u2014'}
      </td>
      <td className={`px-4 py-3 text-sm text-right font-medium tabular-nums ${
        isCalculated
          ? result!.netAmount < 0 ? 'text-red-600' : 'text-green-700'
          : 'text-gray-400'
      }`}>
        {isCalculated ? fmtMoney(result!.netAmount) : '\u2014'}
      </td>
      <td className="px-4 py-3 text-sm text-right tabular-nums text-gray-700">
        {isCalculated ? fmtMoney(result!.commissionAmount) : '\u2014'}
      </td>
      <td className="px-4 py-3 text-center">
        {isConfirmed ? (
          <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">Confirmed</span>
        ) : isCalculated ? (
          <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-700">Calculated</span>
        ) : (
          <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-400">Pending</span>
        )}
      </td>
      <td className="px-4 py-3">
        <div className="flex justify-end gap-2">
          {!isConfirmed && (
            <button
              onClick={() => calcMutation.mutate()}
              disabled={anyPending}
              className="px-3 py-1 text-xs bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
            >
              {calcMutation.isPending ? '\u2026' : 'Calculate'}
            </button>
          )}
          {isCalculated && !isConfirmed && (
            <button
              onClick={() => confirmMutation.mutate()}
              disabled={anyPending}
              className="px-3 py-1 text-xs bg-green-600 text-white rounded hover:bg-green-700 disabled:opacity-50"
            >
              {confirmMutation.isPending ? '\u2026' : 'Confirm'}
            </button>
          )}
          {(calcMutation.isError || confirmMutation.isError) && (
            <span className="text-xs text-red-500">Error</span>
          )}
        </div>
      </td>
    </tr>
  );
}

// ─── Settlement page ──────────────────────────────────────────────────────────

export default function SettlementPage() {
  const { loginName } = useAuthStore();
  const [weekEnding, setWeekEnding] = useState(nextSundayIso());
  const [calculating, setCalculating] = useState(false);
  const qc = useQueryClient();

  const { data: agents = [], isLoading } = useQuery({
    queryKey: ['agents'],
    queryFn:  listAgents,
  });

  const activeAgents = agents.filter((a) => a.isActive);

  const handleCalculateAll = async () => {
    setCalculating(true);
    for (const agent of activeAgents) {
      try {
        await calculateDistribution(agent.id, weekEnding, loginName ?? 'system');
        qc.invalidateQueries({ queryKey: ['agent-makeup', agent.id] });
      } catch {
        // continue with remaining agents on failure
      }
    }
    setCalculating(false);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between flex-wrap gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Settlement</h1>
          <p className="text-sm text-gray-500 mt-0.5">{activeAgents.length} active agents</p>
        </div>
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
            onClick={handleCalculateAll}
            disabled={calculating || isLoading}
            className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            {calculating ? 'Calculating all\u2026' : 'Calculate All'}
          </button>
        </div>
      </div>

      {weekEnding && (
        <p className="text-sm text-gray-500">
          Week ending <strong>{fmtDate(weekEnding)}</strong> — click Calculate on each row, then Confirm to finalise.
        </p>
      )}

      {isLoading && <p className="text-sm text-gray-500">Loading agents\u2026</p>}
      {!isLoading && activeAgents.length === 0 && (
        <p className="text-sm text-gray-500 text-center py-12">No active agents found.</p>
      )}

      {!isLoading && activeAgents.length > 0 && (
        <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Agent</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Type</th>
                <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Commission</th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Current Makeup</th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Week Net</th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Commission</th>
                <th className="px-4 py-3 text-center text-xs font-semibold text-gray-500 uppercase tracking-wide">Status</th>
                <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {activeAgents.map((agent) => (
                <AgentRow
                  key={agent.id}
                  agent={agent}
                  weekEnding={weekEnding}
                  loginName={loginName ?? 'system'}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}

      <p className="text-xs text-gray-400">
        After calculating, open each agent&apos;s detail page to review the full breakdown before confirming.{' '}
        <Link to="/agents" className="text-blue-500 hover:underline">View all agents</Link>
      </p>
    </div>
  );
}
