'use strict';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { updateAgent, updateAgentCreditLimits, deactivateAgent } from '../../api/accountsApi';
import type { Agent, CommissionType } from '../../types/accounts';

const COMMISSION_TYPES: CommissionType[] = [
  'WeeklyProfit', 'Split', 'SplitVariant', 'AffiliateWeekly', 'RedFigure', 'RedFigureVariant',
];

const infoSchema = z.object({
  name:           z.string().optional(),
  commissionType: z.enum(['WeeklyProfit', 'Split', 'SplitVariant', 'AffiliateWeekly', 'RedFigure', 'RedFigureVariant'] as const),
  commissionRate: z.coerce.number().min(0).max(100),
});
type InfoFields = z.infer<typeof infoSchema>;

const limitsSchema = z.object({
  creditLimitMax: z.coerce.number().min(0),
  wagerLimitMax:  z.coerce.number().min(0),
});
type LimitsFields = z.infer<typeof limitsSchema>;

const fmt = (n: number) =>
  n.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 });

const typeColour = (t: string) =>
  ({ Master: 'bg-purple-100 text-purple-700', Agent: 'bg-blue-100 text-blue-700', SubAgent: 'bg-gray-100 text-gray-700' }[t] ?? 'bg-gray-100 text-gray-700');

// ─── Info edit form ───────────────────────────────────────────────────────────

function EditInfoForm({ agent, loginName, onCancel }: { agent: Agent; loginName: string; onCancel: () => void }) {
  const qc = useQueryClient();
  const { register, handleSubmit, formState: { errors } } = useForm<InfoFields>({
    resolver: zodResolver(infoSchema),
    defaultValues: {
      name:           agent.name ?? '',
      commissionType: agent.commissionType,
      commissionRate: agent.commissionRate,
    },
  });

  const { mutate, isPending, error } = useMutation({
    mutationFn: (data: InfoFields) =>
      updateAgent(agent.id, { ...data, updatedBy: loginName }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['agent', agent.id] });
      onCancel();
    },
  });

  return (
    <form onSubmit={handleSubmit((d) => mutate(d))} className="space-y-4">
      <div>
        <label className="block text-xs font-medium text-gray-600 mb-1">Display Name</label>
        <input {...register('name')} className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">Commission Type</label>
          <select {...register('commissionType')} className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm">
            {COMMISSION_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
          </select>
          {errors.commissionType && <p className="text-xs text-red-500 mt-0.5">{errors.commissionType.message}</p>}
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">Commission Rate (%)</label>
          <input {...register('commissionRate')} type="number" step="0.5" className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
          {errors.commissionRate && <p className="text-xs text-red-500 mt-0.5">{errors.commissionRate.message}</p>}
        </div>
      </div>
      {error && <p className="text-xs text-red-500">{String(error)}</p>}
      <div className="flex gap-2 pt-2">
        <button type="submit" disabled={isPending} className="px-4 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
          {isPending ? 'Saving…' : 'Save'}
        </button>
        <button type="button" onClick={onCancel} className="px-4 py-1.5 text-sm bg-gray-100 text-gray-700 rounded hover:bg-gray-200">
          Cancel
        </button>
      </div>
    </form>
  );
}

// ─── Limits edit form ─────────────────────────────────────────────────────────

function EditLimitsForm({ agent, loginName, onCancel }: { agent: Agent; loginName: string; onCancel: () => void }) {
  const qc = useQueryClient();
  const { register, handleSubmit, formState: { errors } } = useForm<LimitsFields>({
    resolver: zodResolver(limitsSchema),
    defaultValues: {
      creditLimitMax: agent.creditLimitMax,
      wagerLimitMax:  agent.wagerLimitMax,
    },
  });

  const { mutate, isPending, error } = useMutation({
    mutationFn: (data: LimitsFields) =>
      updateAgentCreditLimits(agent.id, { ...data, updatedBy: loginName }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['agent', agent.id] });
      onCancel();
    },
  });

  return (
    <form onSubmit={handleSubmit((d) => mutate(d))} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">Credit Limit Max</label>
          <input {...register('creditLimitMax')} type="number" className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
          {errors.creditLimitMax && <p className="text-xs text-red-500 mt-0.5">{errors.creditLimitMax.message}</p>}
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-600 mb-1">Wager Limit Max</label>
          <input {...register('wagerLimitMax')} type="number" className="w-full border border-gray-300 rounded px-3 py-1.5 text-sm" />
          {errors.wagerLimitMax && <p className="text-xs text-red-500 mt-0.5">{errors.wagerLimitMax.message}</p>}
        </div>
      </div>
      {error && <p className="text-xs text-red-500">{String(error)}</p>}
      <div className="flex gap-2 pt-2">
        <button type="submit" disabled={isPending} className="px-4 py-1.5 text-sm bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50">
          {isPending ? 'Saving…' : 'Save'}
        </button>
        <button type="button" onClick={onCancel} className="px-4 py-1.5 text-sm bg-gray-100 text-gray-700 rounded hover:bg-gray-200">
          Cancel
        </button>
      </div>
    </form>
  );
}

// ─── Overview tab ─────────────────────────────────────────────────────────────

export default function OverviewTab({ agent, loginName }: { agent: Agent; loginName: string }) {
  const [editInfo,   setEditInfo]   = useState(false);
  const [editLimits, setEditLimits] = useState(false);
  const [confirming, setConfirming] = useState(false);
  const navigate    = useNavigate();
  const qc          = useQueryClient();

  const { mutate: doDeactivate, isPending: deactivating } = useMutation({
    mutationFn: () => deactivateAgent(agent.id, loginName),
    onSuccess:  () => {
      qc.invalidateQueries({ queryKey: ['agent', agent.id] });
      navigate('/agents');
    },
  });

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
      {/* Agent info card */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">Agent Info</h2>
          {!editInfo && (
            <button onClick={() => setEditInfo(true)} className="text-xs text-blue-600 hover:text-blue-800">
              Edit
            </button>
          )}
        </div>

        {editInfo ? (
          <EditInfoForm agent={agent} loginName={loginName} onCancel={() => setEditInfo(false)} />
        ) : (
          <dl className="space-y-3">
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Login</dt>
              <dd className="text-sm font-medium text-gray-800">{agent.loginName}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Display Name</dt>
              <dd className="text-sm text-gray-800">{agent.name ?? '—'}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Type</dt>
              <dd>
                <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${typeColour(agent.agentType)}`}>
                  {agent.agentType}
                </span>
              </dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Parent Agent</dt>
              <dd className="text-sm text-gray-800">{agent.parentLoginName ?? '—'}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Commission</dt>
              <dd className="text-sm text-gray-800">{agent.commissionType} @ {agent.commissionRate}%</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Status</dt>
              <dd>
                <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${agent.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                  {agent.isActive ? 'Active' : 'Inactive'}
                </span>
              </dd>
            </div>
          </dl>
        )}
      </div>

      {/* Limits card */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">Limits &amp; Counts</h2>
          {!editLimits && (
            <button onClick={() => setEditLimits(true)} className="text-xs text-blue-600 hover:text-blue-800">
              Edit
            </button>
          )}
        </div>

        {editLimits ? (
          <EditLimitsForm agent={agent} loginName={loginName} onCancel={() => setEditLimits(false)} />
        ) : (
          <dl className="space-y-3">
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Credit Limit Max</dt>
              <dd className="text-sm font-medium text-gray-800">{fmt(agent.creditLimitMax)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Wager Limit Max</dt>
              <dd className="text-sm font-medium text-gray-800">{fmt(agent.wagerLimitMax)}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Customers</dt>
              <dd className="text-sm font-medium text-gray-800">{agent.customerCount}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-sm text-gray-500">Sub-Agents</dt>
              <dd className="text-sm font-medium text-gray-800">{agent.subAgentCount}</dd>
            </div>
          </dl>
        )}
      </div>

      {/* Danger zone */}
      {agent.isActive && (
        <div className="lg:col-span-2 bg-white rounded-lg shadow p-6 border border-red-100">
          <h2 className="text-sm font-semibold text-red-700 uppercase tracking-wide mb-3">Danger Zone</h2>
          {confirming ? (
            <div className="flex items-center gap-3">
              <span className="text-sm text-gray-700">Deactivate this agent? All sub-agents and customers will be affected.</span>
              <button
                onClick={() => doDeactivate()}
                disabled={deactivating}
                className="px-4 py-1.5 text-sm bg-red-600 text-white rounded hover:bg-red-700 disabled:opacity-50"
              >
                {deactivating ? 'Deactivating…' : 'Confirm'}
              </button>
              <button onClick={() => setConfirming(false)} className="px-4 py-1.5 text-sm bg-gray-100 text-gray-700 rounded hover:bg-gray-200">
                Cancel
              </button>
            </div>
          ) : (
            <button onClick={() => setConfirming(true)} className="px-4 py-1.5 text-sm bg-red-50 text-red-600 rounded border border-red-200 hover:bg-red-100">
              Deactivate Agent
            </button>
          )}
        </div>
      )}
    </div>
  );
}
