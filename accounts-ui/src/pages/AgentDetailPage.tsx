'use strict';
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { getAgent } from '../api/accountsApi';
import { useAuthStore } from '../stores/authStore';
import OverviewTab    from './agent-tabs/OverviewTab';
import SubAgentsTab   from './agent-tabs/SubAgentsTab';
import CustomersTab   from './agent-tabs/CustomersTab';
import SettlementTab  from './agent-tabs/SettlementTab';
import FiguresTab     from './agent-tabs/FiguresTab';

type Tab = 'overview' | 'sub-agents' | 'customers' | 'settlement' | 'figures';

const TAB_LABELS: { id: Tab; label: string }[] = [
  { id: 'overview',    label: 'Overview' },
  { id: 'sub-agents',  label: 'Sub-Agents' },
  { id: 'customers',   label: 'Customers' },
  { id: 'settlement',  label: 'Settlement' },
  { id: 'figures',     label: 'Position & Figures' },
];

const typeColour = (t: string) =>
  ({ Master: 'bg-purple-100 text-purple-700', Agent: 'bg-blue-100 text-blue-700', SubAgent: 'bg-gray-100 text-gray-700' }[t] ?? 'bg-gray-100 text-gray-700');

export default function AgentDetailPage() {
  const { id }     = useParams<{ id: string }>();
  const agentId    = parseInt(id ?? '0', 10);
  const [activeTab, setActiveTab] = useState<Tab>('overview');

  const { loginName } = useAuthStore();

  const { data: agent, isLoading } = useQuery({
    queryKey: ['agent', agentId],
    queryFn:  () => getAgent(agentId),
    enabled:  agentId > 0,
  });

  if (isLoading) return <p className="text-sm text-gray-500">Loading…</p>;
  if (!agent)    return <p className="text-sm text-red-500">Agent not found.</p>;

  return (
    <div>
      {/* Breadcrumb */}
      <nav className="text-xs text-gray-500 mb-4 flex items-center gap-1.5">
        <Link to="/agents" className="hover:text-blue-600">Agents</Link>
        <span>/</span>
        <span className="text-gray-700">{agent.loginName}</span>
      </nav>

      {/* Header */}
      <div className="mb-6">
        <div className="flex items-center gap-3 mb-1 flex-wrap">
          <h1 className="text-xl font-semibold text-gray-800">{agent.loginName}</h1>
          {agent.name && <span className="text-lg text-gray-500">{agent.name}</span>}
          <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${typeColour(agent.agentType)}`}>
            {agent.agentType}
          </span>
          <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${agent.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
            {agent.isActive ? 'Active' : 'Inactive'}
          </span>
        </div>
        {agent.parentLoginName && (
          <p className="text-sm text-gray-500">
            Parent: <span className="text-gray-700 font-medium">{agent.parentLoginName}</span>
          </p>
        )}
      </div>

      {/* Summary stat chips */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
        {[
          { label: 'Customers',     value: agent.customerCount.toString() },
          { label: 'Sub-Agents',    value: agent.subAgentCount.toString() },
          { label: 'Credit Max',    value: agent.creditLimitMax.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }) },
          { label: 'Commission',    value: `${agent.commissionType} ${agent.commissionRate}%` },
        ].map(({ label, value }) => (
          <div key={label} className="bg-white rounded-lg shadow p-4">
            <p className="text-xs text-gray-500 mb-1">{label}</p>
            <p className="text-base font-semibold text-gray-800">{value}</p>
          </div>
        ))}
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6">
        <nav className="-mb-px flex gap-6 overflow-x-auto">
          {TAB_LABELS.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`pb-3 text-sm font-medium border-b-2 whitespace-nowrap transition-colors ${
                activeTab === tab.id
                  ? 'border-blue-600 text-blue-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {/* Tab content */}
      {activeTab === 'overview'   && <OverviewTab   agent={agent} loginName={loginName ?? ''} />}
      {activeTab === 'sub-agents' && <SubAgentsTab  agent={agent} loginName={loginName ?? ''} />}
      {activeTab === 'customers'  && <CustomersTab  agent={agent} />}
      {activeTab === 'settlement' && <SettlementTab agent={agent} loginName={loginName ?? ''} />}
      {activeTab === 'figures'    && <FiguresTab    agent={agent} />}
    </div>
  );
}
