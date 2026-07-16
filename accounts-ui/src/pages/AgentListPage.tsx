'use strict';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  createAgent,
  deactivateAgent,
  listAgents,
  moveAgent,
  updateAgent,
} from '../api/accountsApi';
import { useAuthStore } from '../stores/authStore';
import type { Agent } from '../types/accounts';

// ─── Zod schemas ──────────────────────────────────────────────────────────────

const AGENT_TYPES = ['Master', 'Agent', 'SubAgent'] as const;
const COMMISSION_TYPES = [
  'WeeklyProfit', 'Split', 'SplitVariant', 'AffiliateWeekly', 'RedFigure', 'RedFigureVariant',
] as const;

const createSchema = z.object({
  loginName:      z.string().min(1).max(10),
  name:           z.string().optional(),
  parentAgentId:  z.coerce.number().optional(),
  agentType:      z.enum(AGENT_TYPES),
  creditLimitMax: z.coerce.number().min(0),
  wagerLimitMax:  z.coerce.number().min(0),
  commissionType: z.enum(COMMISSION_TYPES),
  commissionRate: z.coerce.number().min(0).max(100),
});
type CreateFields = z.infer<typeof createSchema>;

const editSchema = z.object({
  name:           z.string().optional(),
  creditLimitMax: z.coerce.number().min(0).optional(),
  wagerLimitMax:  z.coerce.number().min(0).optional(),
  commissionType: z.enum(COMMISSION_TYPES).optional(),
  commissionRate: z.coerce.number().min(0).max(100).optional(),
});
type EditFields = z.infer<typeof editSchema>;

const moveSchema = z.object({
  newParentAgentId: z.coerce.number().min(1, 'Select a parent agent'),
});
type MoveFields = z.infer<typeof moveSchema>;

// ─── Helpers ──────────────────────────────────────────────────────────────────

const typeColour = (t: string) =>
  ({ Master: 'bg-purple-100 text-purple-700', Agent: 'bg-blue-100 text-blue-700', SubAgent: 'bg-gray-100 text-gray-700' }[t] ?? 'bg-gray-100 text-gray-700');

const fmt = (n: number) =>
  n.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 });

// ─── Create Modal ─────────────────────────────────────────────────────────────

function CreateAgentModal({
  agents,
  loginName,
  onClose,
}: {
  agents: Agent[];
  loginName: string;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const { register, handleSubmit, formState: { errors } } = useForm<CreateFields>({
    resolver: zodResolver(createSchema),
    defaultValues: {
      agentType:      'Agent',
      commissionType: 'WeeklyProfit',
      commissionRate: 50,
      creditLimitMax: 0,
      wagerLimitMax:  0,
    },
  });

  const mutation = useMutation({
    mutationFn: (data: CreateFields) =>
      createAgent({ ...data, parentAgentId: data.parentAgentId || undefined, createdBy: loginName }),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['agents'] }); onClose(); },
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-lg p-6">
        <h2 className="text-lg font-semibold text-gray-800 mb-4">Create Agent</h2>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Login Name *</label>
              <input {...register('loginName')} autoComplete="off"
                className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none" />
              {errors.loginName && <p className="text-xs text-red-500 mt-0.5">{errors.loginName.message}</p>}
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Display Name</label>
              <input {...register('name')} autoComplete="off"
                className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none" />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Agent Type *</label>
              <select {...register('agentType')} className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none">
                {AGENT_TYPES.map((t) => <option key={t}>{t}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Parent Agent</label>
              <select {...register('parentAgentId')} className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none">
                <option value="">— None (top-level) —</option>
                {agents.map((a) => (
                  <option key={a.id} value={a.id}>{a.loginName}</option>
                ))}
              </select>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Credit Limit Max *</label>
              <input {...register('creditLimitMax')} type="number" min="0"
                className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none" />
              {errors.creditLimitMax && <p className="text-xs text-red-500 mt-0.5">{errors.creditLimitMax.message}</p>}
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Wager Limit Max *</label>
              <input {...register('wagerLimitMax')} type="number" min="0"
                className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none" />
              {errors.wagerLimitMax && <p className="text-xs text-red-500 mt-0.5">{errors.wagerLimitMax.message}</p>}
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Commission Type *</label>
              <select {...register('commissionType')} className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none">
                {COMMISSION_TYPES.map((t) => <option key={t}>{t}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Commission Rate % *</label>
              <input {...register('commissionRate')} type="number" min="0" max="100" step="0.5"
                className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none" />
              {errors.commissionRate && <p className="text-xs text-red-500 mt-0.5">{errors.commissionRate.message}</p>}
            </div>
          </div>

          {mutation.isError && (
            <p className="text-sm text-red-600 bg-red-50 rounded px-3 py-2">
              {(mutation.error as Error).message}
            </p>
          )}

          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800">Cancel</button>
            <button type="submit" disabled={mutation.isPending}
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
              {mutation.isPending ? 'Creating...' : 'Create Agent'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ─── Edit Modal ───────────────────────────────────────────────────────────────

function EditAgentModal({
  agent,
  loginName,
  onClose,
}: {
  agent: Agent;
  loginName: string;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const { register, handleSubmit, formState: { errors } } = useForm<EditFields>({
    resolver: zodResolver(editSchema),
    defaultValues: {
      name:           agent.name ?? '',
      creditLimitMax: agent.creditLimitMax,
      wagerLimitMax:  agent.wagerLimitMax,
      commissionType: agent.commissionType,
      commissionRate: agent.commissionRate,
    },
  });

  const mutation = useMutation({
    mutationFn: (data: EditFields) =>
      updateAgent(agent.id, { ...data, updatedBy: loginName }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['agents'] });
      qc.invalidateQueries({ queryKey: ['agent', agent.id] });
      onClose();
    },
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-md p-6">
        <h2 className="text-lg font-semibold text-gray-800 mb-1">Edit Agent</h2>
        <p className="text-sm text-gray-500 mb-4">{agent.loginName}</p>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Display Name</label>
            <input {...register('name')}
              className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none" />
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Credit Limit Max</label>
              <input {...register('creditLimitMax')} type="number" min="0"
                className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none" />
              {errors.creditLimitMax && <p className="text-xs text-red-500 mt-0.5">{errors.creditLimitMax.message}</p>}
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Wager Limit Max</label>
              <input {...register('wagerLimitMax')} type="number" min="0"
                className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none" />
              {errors.wagerLimitMax && <p className="text-xs text-red-500 mt-0.5">{errors.wagerLimitMax.message}</p>}
            </div>
          </div>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Commission Type</label>
              <select {...register('commissionType')} className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none">
                {COMMISSION_TYPES.map((t) => <option key={t}>{t}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Commission Rate %</label>
              <input {...register('commissionRate')} type="number" min="0" max="100" step="0.5"
                className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none" />
              {errors.commissionRate && <p className="text-xs text-red-500 mt-0.5">{errors.commissionRate.message}</p>}
            </div>
          </div>

          {mutation.isError && (
            <p className="text-sm text-red-600 bg-red-50 rounded px-3 py-2">
              {(mutation.error as Error).message}
            </p>
          )}

          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800">Cancel</button>
            <button type="submit" disabled={mutation.isPending}
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
              {mutation.isPending ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ─── Move Modal ───────────────────────────────────────────────────────────────

function MoveAgentModal({
  agent,
  agents,
  loginName,
  onClose,
}: {
  agent: Agent;
  agents: Agent[];
  loginName: string;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const { register, handleSubmit, formState: { errors } } = useForm<MoveFields>({
    resolver: zodResolver(moveSchema),
    defaultValues: { newParentAgentId: 0 },
  });

  const mutation = useMutation({
    mutationFn: (data: MoveFields) =>
      moveAgent(agent.id, { newParentAgentId: data.newParentAgentId, updatedBy: loginName }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['agents'] });
      qc.invalidateQueries({ queryKey: ['agent', agent.id] });
      onClose();
    },
  });

  const eligible = agents.filter((a) => a.id !== agent.id && a.isActive);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-sm p-6">
        <h2 className="text-lg font-semibold text-gray-800 mb-1">Move Agent</h2>
        <p className="text-sm text-gray-500 mb-4">Move <strong>{agent.loginName}</strong> under a new parent</p>
        <form onSubmit={handleSubmit((d) => mutation.mutate(d))} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">New Parent Agent *</label>
            <select {...register('newParentAgentId')} className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm focus:ring-1 focus:ring-blue-500 focus:outline-none">
              <option value={0}>— Select —</option>
              {eligible.map((a) => (
                <option key={a.id} value={a.id}>{a.loginName}{a.name ? ` — ${a.name}` : ''}</option>
              ))}
            </select>
            {errors.newParentAgentId && <p className="text-xs text-red-500 mt-0.5">{errors.newParentAgentId.message}</p>}
          </div>

          {mutation.isError && (
            <p className="text-sm text-red-600 bg-red-50 rounded px-3 py-2">
              {(mutation.error as Error).message}
            </p>
          )}

          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800">Cancel</button>
            <button type="submit" disabled={mutation.isPending}
              className="px-4 py-2 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
              {mutation.isPending ? 'Moving...' : 'Move Agent'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export default function AgentListPage() {
  const { loginName } = useAuthStore();
  const navigate = useNavigate();
  const qc = useQueryClient();

  const [showCreate, setShowCreate] = useState(false);
  const [editAgent,  setEditAgent]  = useState<Agent | null>(null);
  const [moveAgent_,  setMoveAgent]  = useState<Agent | null>(null);
  const [showInactive, setShowInactive] = useState(false);

  const { data: agents = [], isLoading, isError } = useQuery({
    queryKey: ['agents'],
    queryFn:  listAgents,
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: number) => deactivateAgent(id, loginName ?? 'system'),
    onSuccess:  () => qc.invalidateQueries({ queryKey: ['agents'] }),
  });

  const visible = showInactive ? agents : agents.filter((a) => a.isActive);

  if (isLoading) return <p className="text-sm text-gray-500 py-8">Loading agents...</p>;
  if (isError)   return <p className="text-sm text-red-500 py-8">Failed to load agents.</p>;

  return (
    <div>
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Agents</h1>
          <p className="text-sm text-gray-500 mt-0.5">{agents.filter((a) => a.isActive).length} active agents</p>
        </div>
        <div className="flex items-center gap-3">
          <label className="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
            <input type="checkbox" checked={showInactive} onChange={(e) => setShowInactive(e.target.checked)}
              className="rounded border-gray-300" />
            Show inactive
          </label>
          <button
            onClick={() => setShowCreate(true)}
            className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
          >
            + New Agent
          </button>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Login</th>
              <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Name</th>
              <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Type</th>
              <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Parent</th>
              <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Credit Max</th>
              <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Wager Max</th>
              <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">Commission</th>
              <th className="px-4 py-3 text-center text-xs font-semibold text-gray-500 uppercase tracking-wide">Customers</th>
              <th className="px-4 py-3 text-center text-xs font-semibold text-gray-500 uppercase tracking-wide">Sub-Agents</th>
              <th className="px-4 py-3 text-center text-xs font-semibold text-gray-500 uppercase tracking-wide">Status</th>
              <th className="px-4 py-3 text-right text-xs font-semibold text-gray-500 uppercase tracking-wide">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {visible.map((agent) => (
              <tr
                key={agent.id}
                onClick={() => navigate(`/agents/${agent.id}`)}
                className="hover:bg-blue-50 cursor-pointer transition-colors"
              >
                <td className="px-4 py-3 font-medium text-blue-700">{agent.loginName}</td>
                <td className="px-4 py-3 text-gray-600">{agent.name ?? '—'}</td>
                <td className="px-4 py-3">
                  <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${typeColour(agent.agentType)}`}>
                    {agent.agentType}
                  </span>
                </td>
                <td className="px-4 py-3 text-gray-500">{agent.parentLoginName ?? '—'}</td>
                <td className="px-4 py-3 text-right text-gray-700 tabular-nums">{fmt(agent.creditLimitMax)}</td>
                <td className="px-4 py-3 text-right text-gray-700 tabular-nums">{fmt(agent.wagerLimitMax)}</td>
                <td className="px-4 py-3 text-gray-600">
                  {agent.commissionType.replace(/([A-Z])/g, ' $1').trim()} ({agent.commissionRate}%)
                </td>
                <td className="px-4 py-3 text-center text-gray-700">{agent.customerCount}</td>
                <td className="px-4 py-3 text-center text-gray-700">{agent.subAgentCount}</td>
                <td className="px-4 py-3 text-center">
                  <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${agent.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                    {agent.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="px-4 py-3 text-right" onClick={(e) => e.stopPropagation()}>
                  <div className="flex justify-end gap-2">
                    <button
                      onClick={() => setEditAgent(agent)}
                      className="text-xs text-blue-600 hover:text-blue-800 font-medium px-2 py-1 rounded hover:bg-blue-50"
                    >
                      Edit
                    </button>
                    <button
                      onClick={() => setMoveAgent(agent)}
                      className="text-xs text-gray-600 hover:text-gray-800 font-medium px-2 py-1 rounded hover:bg-gray-50"
                    >
                      Move
                    </button>
                    {agent.isActive && (
                      <button
                        onClick={() => {
                          if (confirm(`Deactivate ${agent.loginName}?`))
                            deactivateMutation.mutate(agent.id);
                        }}
                        className="text-xs text-red-500 hover:text-red-700 font-medium px-2 py-1 rounded hover:bg-red-50"
                      >
                        Deactivate
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
            {visible.length === 0 && (
              <tr>
                <td colSpan={11} className="px-4 py-10 text-center text-sm text-gray-500">
                  No agents found.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Modals */}
      {showCreate && (
        <CreateAgentModal agents={agents} loginName={loginName ?? 'system'} onClose={() => setShowCreate(false)} />
      )}
      {editAgent && (
        <EditAgentModal agent={editAgent} loginName={loginName ?? 'system'} onClose={() => setEditAgent(null)} />
      )}
      {moveAgent_ && (
        <MoveAgentModal agent={moveAgent_} agents={agents} loginName={loginName ?? 'system'} onClose={() => setMoveAgent(null)} />
      )}
    </div>
  );
}
