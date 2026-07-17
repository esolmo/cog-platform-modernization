import type {
  AddCustomerCommentRequest,
  Agent,
  AgentDistribution,
  AgentFigures,
  AgentHierarchyNode,
  AgentMakeup,
  AgentPosition,
  AgentPositionSummary,
  AwardFreePlayRequest,
  BatchCreateTransactionRequest,
  BatchTransactionResponse,
  CreateAgentRequest,
  CreateCustomerRequest,
  CreateTransactionRequest,
  Customer,
  CustomerCasinoLimits,
  CustomerComment,
  CustomerFreePlay,
  CustomerPermissions,
  CustomerWagerLimits,
  MoveAgentRequest,
  PagedResult,
  Transaction,
  UpdateAgentCreditLimitsRequest,
  UpdateAgentRequest,
  UpdateCreditLimitRequest,
  UpdateCustomerCasinoLimitsRequest,
  UpdateCustomerPermissionsRequest,
  UpdateCustomerRequest,
  UpdateCustomerWagerLimitsRequest,
} from '../types/accounts';
import apiClient from './client';

// ─── Customer endpoints ───────────────────────────────────────────────────────

export const createCustomer = (request: CreateCustomerRequest) =>
  apiClient.post<Customer>('/customers', request).then((r) => r.data);

export const getCustomer = (id: number) =>
  apiClient.get<Customer>(`/customers/${id}`).then((r) => r.data);

export const getCustomerByLogin = (loginName: string) =>
  apiClient.get<Customer>(`/customers/by-login/${loginName}`).then((r) => r.data);

export const getCustomersByAgent = (agentId: number, page = 1, pageSize = 25) =>
  apiClient
    .get<PagedResult<Customer>>(`/customers/by-agent/${agentId}`, {
      params: { page, pageSize },
    })
    .then((r) => r.data);

export const getCustomers = (search = '', page = 1, pageSize = 25) =>
  apiClient
    .get<PagedResult<Customer>>('/customers', {
      params: { search: search || undefined, page, pageSize },
    })
    .then((r) => r.data);

export const updateCustomer = (id: number, request: UpdateCustomerRequest) =>
  apiClient.put<Customer>(`/customers/${id}`, request).then((r) => r.data);

export const updateCreditLimits = (id: number, request: UpdateCreditLimitRequest) =>
  apiClient.put<Customer>(`/customers/${id}/credit-limits`, request).then((r) => r.data);

export const suspendCustomer = (id: number, updatedBy: string) =>
  apiClient.post(`/customers/${id}/suspend`, null, { params: { updatedBy } });

export const activateCustomer = (id: number, updatedBy: string) =>
  apiClient.post(`/customers/${id}/activate`, null, { params: { updatedBy } });

export const getCustomerPermissions = (id: number) =>
  apiClient.get<CustomerPermissions>(`/customers/${id}/permissions`).then((r) => r.data);

export const updateCustomerPermissions = (id: number, request: UpdateCustomerPermissionsRequest) =>
  apiClient.put<CustomerPermissions>(`/customers/${id}/permissions`, request).then((r) => r.data);

export const getCustomerWagerLimits = (id: number) =>
  apiClient.get<CustomerWagerLimits>(`/customers/${id}/wager-limits`).then((r) => r.data);

export const updateCustomerWagerLimits = (id: number, request: UpdateCustomerWagerLimitsRequest) =>
  apiClient.put<CustomerWagerLimits>(`/customers/${id}/wager-limits`, request).then((r) => r.data);

export const getCustomerCasinoLimits = (id: number) =>
  apiClient.get<CustomerCasinoLimits>(`/customers/${id}/casino-limits`).then((r) => r.data);

export const updateCustomerCasinoLimits = (id: number, request: UpdateCustomerCasinoLimitsRequest) =>
  apiClient.put<CustomerCasinoLimits>(`/customers/${id}/casino-limits`, request).then((r) => r.data);

export const getCustomerComments = (id: number, includeCustomerVisible = false) =>
  apiClient
    .get<CustomerComment[]>(`/customers/${id}/comments`, { params: { includeCustomerVisible } })
    .then((r) => r.data);

export const addCustomerComment = (id: number, request: AddCustomerCommentRequest) =>
  apiClient.post<CustomerComment>(`/customers/${id}/comments`, request).then((r) => r.data);

export const getCustomerFreePlay = (id: number) =>
  apiClient.get<CustomerFreePlay[]>(`/customers/${id}/free-play`).then((r) => r.data);

export const awardFreePlay = (id: number, request: AwardFreePlayRequest) =>
  apiClient.post<CustomerFreePlay>(`/customers/${id}/free-play`, request).then((r) => r.data);

// ─── Agent endpoints ──────────────────────────────────────────────────────────

export const listAgents = () =>
  apiClient.get<Agent[]>('/agents').then((r) => r.data);

export const getAgent = (id: number) =>
  apiClient.get<Agent>(`/agents/${id}`).then((r) => r.data);

export const getSubAgents = (id: number) =>
  apiClient.get<Agent[]>(`/agents/${id}/sub-agents`).then((r) => r.data);

export const getAgentHierarchy = (id: number) =>
  apiClient.get<AgentHierarchyNode>(`/agents/${id}/hierarchy`).then((r) => r.data);

export const createAgent = (request: CreateAgentRequest) =>
  apiClient.post<Agent>('/agents', request).then((r) => r.data);

export const updateAgent = (id: number, request: UpdateAgentRequest) =>
  apiClient.put<Agent>(`/agents/${id}`, request).then((r) => r.data);

export const updateAgentCreditLimits = (id: number, request: UpdateAgentCreditLimitsRequest) =>
  apiClient.put<Agent>(`/agents/${id}/credit-limits`, request).then((r) => r.data);

export const moveAgent = (id: number, request: MoveAgentRequest) =>
  apiClient.post<Agent>(`/agents/${id}/move`, request).then((r) => r.data);

export const deactivateAgent = (id: number, updatedBy: string) =>
  apiClient.post(`/agents/${id}/deactivate`, null, { params: { updatedBy } });

export const getAgentMakeup = (id: number) =>
  apiClient.get<AgentMakeup>(`/agents/${id}/makeup`).then((r) => r.data);

export const getDistributionHistory = (id: number, weeksBack = 12) =>
  apiClient
    .get<AgentDistribution[]>(`/agents/${id}/distribution`, { params: { weeksBack } })
    .then((r) => r.data);

export const calculateDistribution = (id: number, weekEnding: string, calculatedBy: string) =>
  apiClient
    .post<AgentDistribution>(
      `/agents/${id}/distribution/${weekEnding}/calculate`,
      null,
      { params: { calculatedBy } }
    )
    .then((r) => r.data);

export const confirmDistribution = (id: number, weekEnding: string, confirmedBy: string) =>
  apiClient
    .post<AgentDistribution>(
      `/agents/${id}/distribution/${weekEnding}/confirm`,
      null,
      { params: { confirmedBy } }
    )
    .then((r) => r.data);

// ─── Transaction endpoints ────────────────────────────────────────────────────

export const createTransaction = (request: CreateTransactionRequest) =>
  apiClient.post<Transaction>('/transactions', request).then((r) => r.data);

export const getTransactionsByCustomer = (
  customerId: number,
  page = 1,
  pageSize = 25
) =>
  apiClient
    .get<PagedResult<Transaction>>(`/transactions/by-customer/${customerId}`, {
      params: { page, pageSize },
    })
    .then((r) => r.data);

export const verifyTransaction = (id: number, verifiedBy: string) =>
  apiClient
    .post<Transaction>(`/transactions/${id}/verify`, null, { params: { verifiedBy } })
    .then((r) => r.data);

export const createBatchTransactions = (request: BatchCreateTransactionRequest) =>
  apiClient.post<BatchTransactionResponse>('/transactions/batch', request).then((r) => r.data);

// ─── Agent position & figures endpoints ──────────────────────────────────────

export const getAgentPositionSummary = () =>
  apiClient.get<AgentPositionSummary[]>('/agents/position-summary').then((r) => r.data);

export const getAgentPosition = (id: number) =>
  apiClient.get<AgentPosition>(`/agents/${id}/position`).then((r) => r.data);

export const getAgentFigures = (id: number, from?: string, to?: string) =>
  apiClient
    .get<AgentFigures>(`/agents/${id}/figures`, { params: { from, to } })
    .then((r) => r.data);
