import axios from 'axios';

const api = axios.create({ baseURL: '/api' });

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export interface WagerActivityRow {
  documentNumber: string;
  tranDateTime: string;
  tranType: string;
  amount: number;
  description: string;
  gradeNum: number;
}

export interface ChangedTransactionRow {
  idCustomer: number;
  loginName: string;
  updatedDateTime: string;
  transactionType: string;
  amount: number;
  description: string;
  reference: string;
}

export interface AgentRow {
  id: number;
  loginName: string;
  fullName: string;
  email: string;
  isActive: boolean;
}

export interface CustomerRow {
  id: number;
  loginName: string;
  fullName: string;
  agentId: number;
  balance: number;
}

export interface PackageTrackerRow {
  documentNumber: number;
  tranDate: string;
  amount: number;
  status: string;
  packageTo: string;
  reference: string;
  packageService: string;
}

export const reportsApi = {
  getWagerActivity: (loginId: string, from: string, to: string) =>
    api.get<WagerActivityRow[]>('/reports/wagers', { params: { loginId, from, to } }).then((r) => r.data),

  getChangedTransactions: (
    agentId: number,
    customerId = 0,
    from?: string,
    to?: string,
    includeAgentTransactions = false
  ) =>
    api
      .get<ChangedTransactionRow[]>('/reports/transactions', {
        params: { agentId, customerId, from, to, includeAgentTransactions },
      })
      .then((r) => r.data),

  searchAgents: (agentId: number, search = '') =>
    api.get<AgentRow[]>('/reports/agents', { params: { agentId, search } }).then((r) => r.data),

  searchCustomers: (agentId: number, search = '') =>
    api.get<CustomerRow[]>('/reports/customers', { params: { agentId, search } }).then((r) => r.data),

  getPackageTracker: (viewDepartment = 0, from?: string, to?: string, agentDestination = 0) =>
    api
      .get<PackageTrackerRow[]>('/reports/packages', {
        params: { viewDepartment, from, to, agentDestination },
      })
      .then((r) => r.data),
};
