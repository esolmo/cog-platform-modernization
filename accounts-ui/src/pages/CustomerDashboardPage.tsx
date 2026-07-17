import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  activateCustomer,
  getCustomer,
  getTransactionsByCustomer,
  suspendCustomer,
} from '../api/accountsApi';
import { useAuthStore } from '../stores/authStore';
import PersonalTab from './customer-tabs/PersonalTab';
import LimitsTab from './customer-tabs/LimitsTab';
import TransactionsTab from './customer-tabs/TransactionsTab';
import PermissionsTab from './customer-tabs/PermissionsTab';
import FreePlayTab from './customer-tabs/FreePlayTab';
import CommentsTab from './customer-tabs/CommentsTab';

type Tab = 'personal' | 'limits' | 'transactions' | 'permissions' | 'free-play' | 'comments';

export default function CustomerDashboardPage() {
  const { id }         = useParams<{ id: string }>();
  const customerId     = parseInt(id ?? '0', 10);
  const [activeTab, setActiveTab] = useState<Tab>('personal');
  const qc        = useQueryClient();
  const loginName = useAuthStore((s) => s.loginName) ?? 'system';

  const { data: customer, isLoading } = useQuery({
    queryKey: ['customer', customerId],
    queryFn:  () => getCustomer(customerId),
    enabled:  customerId > 0,
  });

  const suspendMutation = useMutation({
    mutationFn: () => suspendCustomer(customerId, loginName),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['customer', customerId] }),
  });

  const activateMutation = useMutation({
    mutationFn: () => activateCustomer(customerId, loginName),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['customer', customerId] }),
  });

  const { data: transactions } = useQuery({
    queryKey: ['transactions', customerId, 1],
    queryFn:  () => getTransactionsByCustomer(customerId, 1, 25),
    enabled:  customerId > 0 && activeTab === 'transactions',
  });

  const tabs: { id: Tab; label: string }[] = [
    { id: 'personal',     label: 'Personal' },
    { id: 'limits',       label: 'Limits' },
    { id: 'transactions', label: 'Transactions' },
    { id: 'permissions',  label: 'Permissions' },
    { id: 'free-play',    label: 'Free Play' },
    { id: 'comments',     label: 'Comments' },
  ];

  if (isLoading) return <p className="text-sm text-gray-500">Loading...</p>;
  if (!customer) return <p className="text-sm text-red-500">Customer not found.</p>;

  return (
    <div>
      {/* Header */}
      <div className="mb-6 flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3 mb-1">
            <h1 className="text-xl font-semibold text-gray-800">{customer.loginName}</h1>
            <span
              className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                customer.status === 'Active'
                  ? 'bg-green-100 text-green-800'
                  : 'bg-red-100 text-red-700'
              }`}
            >
              {customer.status}
            </span>
          </div>
          <p className="text-sm text-gray-500">Agent: {customer.agentLoginName}</p>
        </div>
        {customer.status === 'Active' ? (
          <button
            onClick={() => suspendMutation.mutate()}
            disabled={suspendMutation.isPending}
            className="px-4 py-2 text-sm font-medium bg-red-600 text-white rounded-md hover:bg-red-700 disabled:opacity-50"
          >
            {suspendMutation.isPending ? 'Suspending…' : 'Suspend Customer'}
          </button>
        ) : (
          <button
            onClick={() => activateMutation.mutate()}
            disabled={activateMutation.isPending}
            className="px-4 py-2 text-sm font-medium bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50"
          >
            {activateMutation.isPending ? 'Activating…' : 'Activate Customer'}
          </button>
        )}
      </div>

      {/* Balance summary cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        {[
          { label: 'Credit Limit',    value: customer.balance.creditLimit },
          { label: 'Current Balance', value: customer.balance.currentBalance },
          { label: 'Available',       value: customer.balance.availableCredit },
          { label: 'Free Play',       value: customer.balance.freePlayBalance },
        ].map(({ label, value }) => (
          <div key={label} className="bg-white rounded-lg shadow p-4">
            <p className="text-xs text-gray-500 mb-1">{label}</p>
            <p className="text-lg font-semibold text-gray-800">${value.toFixed(2)}</p>
          </div>
        ))}
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 mb-6 overflow-x-auto">
        <nav className="-mb-px flex gap-6 min-w-max">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`pb-3 text-sm font-medium border-b-2 transition-colors ${
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
      {activeTab === 'personal'     && <PersonalTab customer={customer} />}
      {activeTab === 'limits'       && <LimitsTab customer={customer} />}
      {activeTab === 'transactions' && <TransactionsTab customerId={customerId} transactions={transactions} />}
      {activeTab === 'permissions'  && <PermissionsTab customer={customer} />}
      {activeTab === 'free-play'    && <FreePlayTab customer={customer} />}
      {activeTab === 'comments'     && <CommentsTab customer={customer} />}
    </div>
  );
}
