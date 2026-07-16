'use strict';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { createAgent, getSubAgents } from '../../api/accountsApi';
import type { Agent, CommissionType } from '../../types/accounts';

const AGENT_TYPES = ['Agent', 'SubAgent'] as const;
const COMMISSION_TYPES: CommissionType[] = [
  'WeeklyProfit', 'Split', 'SplitVariant', 'AffiliateWeekly', 'RedFigure', 'RedFigureVariant',
];

const createSchema = z.object({
  loginName:      z.string().min(1, 'Required').max(10, 'Max 10 chars'),
  name:           z.string().optional(),
  agentType:      z.enum(AGENT_TYPES),
  creditLimitMax: z.coerce.number().min(0),
  wagerLimitMax:  z.coerce.number().min(0),
  commissionType: z.enum(['WeeklyProfit', 'Split', 'SplitVariant', 'AffiliateWeekly', 'RedFigure', 'RedFigureVariant'] as const),
  commissionRate: z.coerce.number().min(0).max(100),
});
type CreateFields = z.infer<typeof createSchema>;

const fmt = (n: number) =>
  n.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 });

const typeColour = (t: string) =>
  ({ Master: 'bg-purple-100 text-purple-700', Agent: 'bg-blue-100 text-blue-700', SubAgent: 'bg-gray-100 text-gray-700' }[t] ?? 'bg-gray-100 text-gray-700');

// ─── Create sub-agent modal ───────────────────────────────────────────────────

function CreateSubAgentModal({
  parentAgentId,
  loginName,
  onClose,
}: {
  parentAgentId: number;
  loginName: string;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const { register, handleSubmit, formState: { errors } } = useForm<CreateFields>({
    resolver: zodResolver(createSchema),
    defaultValues: {
      agentType:      'SubAgent',
      commissionType: 'WeeklyProfit',
      commissionRate: 50,
      creditLimitMax: 0,
      wagerLimitMax:  0,
    },
  });

  const { mutate, isPending, error } = useMutation({
    mutationFn: (data: CreateFields) =>
      createAgent({ ...data, parentAgentId, createdBy: loginName }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['sub-agents', parentAgentId] });
      qc.invalidateQueries({ queryKey: ['agent', parentAgentId] });
      onClose();
    },
  });

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 className="text-lg font-semibold text-gray-800 mb-4">Create Sub-Agent</h2>
        <form onSubmit={handleSubmit((d) => mutate(d))} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Login Name *</label>
              <input {...register('loginName')} className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
              {errors.loginName && <p className="text-xs text-red-500 mt-0.5">{errors.loginName.message}</p>}
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Display Name</label>
              <input {...register('name')} className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-600 mb-1">Agent Type</label>
            <select {...register('agentType')} className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm">
              {AGENT_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Credit Limit Max</label>
              <input {...register('creditLimitMax')} type="number" className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Wager Limit Max</label>
              <input {...register('wagerLimitMax')} type="number" className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Commission Type</label>
              <select {...register('commissionType')} className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm">
                {COMMISSION_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-600 mb-1">Commission Rate (%)</label>
              <input {...register('commissionRate')} type="number" step="0.5" className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
            </div>
          </div>
          {error && <p className="text-xs text-red-500">{String(error)}</p>}
          <div className="flex justify-end gap-2 pt-2">
            <button type="button" onClick={onClose} className="px-4 py-2 text-sm bg-gray-100 text-gray-700 rounded hover:bg-gray-200">
              Cancel
            </button>
            <button type="submit" disabled={isPending} className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
              {isPending ? 'Creating…' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ─── Sub-agents tab ───────────────────────────────────────────────────────────

export default function SubAgentsTab({ agent, loginName }: { agent: Agent; loginName: string }) {
  const [showCreate, setShowCreate] = useState(false);
  const navigate = useNavigate();

  const { data: subAgents, isLoading } = useQuery({
    queryKey: ['sub-agents', agent.id],
    queryFn:  () => getSubAgents(agent.id),
  });

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-sm font-semibold text-gray-700">
          Direct Sub-Agents <span className="text-gray-400 font-normal">({subAgents?.length ?? 0})</span>
        </h2>
        <button
          onClick={() => setShowCreate(true)}
          className="px-3 py-1.5 text-xs bg-blue-600 text-white rounded hover:bg-blue-700"
        >
          + Add Sub-Agent
        </button>
      </div>

      {isLoading && <p className="text-sm text-gray-500">Loading…</p>}

      {subAgents && subAgents.length === 0 && (
        <p className="text-sm text-gray-500 text-center py-8">No sub-agents under this agent.</p>
      )}

      {subAgents && subAgents.length > 0 && (
        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Login</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Name</th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Type</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Credit Max</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Commission</th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase">Customers</th>
                <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Status</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-100">
              {subAgents.map((sa) => (
                <tr
                  key={sa.id}
                  onClick={() => navigate(`/agents/${sa.id}`)}
                  className="hover:bg-blue-50 cursor-pointer"
                >
                  <td className="px-4 py-3 text-sm font-medium text-blue-700">{sa.loginName}</td>
                  <td className="px-4 py-3 text-sm text-gray-600">{sa.name ?? '—'}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${typeColour(sa.agentType)}`}>
                      {sa.agentType}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-sm text-gray-700 text-right">{fmt(sa.creditLimitMax)}</td>
                  <td className="px-4 py-3 text-sm text-gray-600 text-right">{sa.commissionType} {sa.commissionRate}%</td>
                  <td className="px-4 py-3 text-sm text-gray-700 text-right">{sa.customerCount}</td>
                  <td className="px-4 py-3 text-center">
                    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${sa.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                      {sa.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showCreate && (
        <CreateSubAgentModal
          parentAgentId={agent.id}
          loginName={loginName}
          onClose={() => setShowCreate(false)}
        />
      )}
    </div>
  );
}
