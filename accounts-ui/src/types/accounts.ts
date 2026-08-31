export interface BalanceSummary {
  creditLimit: number;
  wagerLimit: number;
  currentBalance: number;
  availableCredit: number;
  pendingWagerBalance: number;
  pendingWagerCount: number;
  freePlayBalance: number;
}

export interface Customer {
  id: number;
  loginName: string;
  alternateLoginName?: string;
  agentId: number;
  agentLoginName: string;
  status: 'Active' | 'Inactive' | 'Suspended';
  oddsFormat: 'American' | 'Decimal' | 'Fractional';
  instantActionEnabled: boolean;
  email?: string;
  phone?: string;
  createdAt: string;
  balance: BalanceSummary;
}

export type CommissionType =
  | 'WeeklyProfit'
  | 'Split'
  | 'SplitVariant'
  | 'AffiliateWeekly'
  | 'RedFigure'
  | 'RedFigureVariant';

export interface Agent {
  id: number;
  loginName: string;
  name?: string;
  parentAgentId?: number;
  parentLoginName?: string;
  agentType: 'Master' | 'Agent' | 'SubAgent';
  creditLimitMax: number;
  wagerLimitMax: number;
  commissionType: CommissionType;
  commissionRate: number;
  isActive: boolean;
  customerCount: number;
  subAgentCount: number;
  /** Only populated in the response to createAgent() — shown once, never returned again. */
  temporaryPassword?: string;
}

export interface CreateAgentRequest {
  loginName: string;
  name?: string;
  parentAgentId?: number;
  agentType: 'Master' | 'Agent' | 'SubAgent';
  creditLimitMax: number;
  wagerLimitMax: number;
  commissionType: CommissionType;
  commissionRate: number;
  createdBy: string;
}

export interface UpdateAgentRequest {
  name?: string;
  creditLimitMax?: number;
  wagerLimitMax?: number;
  commissionType?: CommissionType;
  commissionRate?: number;
  updatedBy: string;
}

export interface UpdateAgentCreditLimitsRequest {
  creditLimitMax: number;
  wagerLimitMax: number;
  updatedBy: string;
}

export interface MoveAgentRequest {
  newParentAgentId: number;
  updatedBy: string;
}

export interface AgentDistribution {
  id: number;
  agentId: number;
  agentLoginName: string;
  weekEnding: string;
  winAmount: number;
  lossAmount: number;
  netAmount: number;
  casinoWinAmount: number;
  casinoLossAmount: number;
  casinoFeeAmount: number;
  liveDealerWin: number;
  liveDealerLoss: number;
  liveDealerFee: number;
  creditAdjustments: number;
  debitAdjustments: number;
  commissionType: CommissionType;
  commissionRate: number;
  commissionAmount: number;
  previousMakeup: number;
  newMakeup: number;
  headCountFee: number;
  activePlayerCount: number;
  newBalance: number;
  isConfirmed: boolean;
  calculatedAt: string;
  confirmedAt?: string;
}

export interface AgentMakeup {
  agentId: number;
  agentLoginName: string;
  currentMakeup: number;
  asOfWeekEnding?: string;
}

export interface AgentHierarchyNode {
  id: number;
  loginName: string;
  name?: string;
  agentType: string;
  level: number;
  children: AgentHierarchyNode[];
}

export interface Transaction {
  id: number;
  customerId: number;
  code: 'Credit' | 'Debit';
  type: string;
  amount: number;
  balanceBefore: number;
  balanceAfter: number;
  description?: string;
  reference?: string;
  enteredBy: string;
  isVerified: boolean;
  transactionDate: string;
  valueDate?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface CreateCustomerRequest {
  loginName: string;
  alternateLoginName?: string;
  agentId: number;
  email?: string;
  phone?: string;
  oddsFormat: 'American' | 'Decimal' | 'Fractional';
  creditLimit: number;
  wagerLimit: number;
  maxStraightWager: number;
  maxParlayWager: number;
  maxParlayPayout: number;
  maxTeaserWager: number;
  maxIfBetWager: number;
  hardCreditLimit: number;
  createdBy: string;
}

export interface UpdateCustomerRequest {
  alternateLoginName?: string;
  email?: string;
  phone?: string;
  oddsFormat?: 'American' | 'Decimal' | 'Fractional';
  instantActionEnabled?: boolean;
  updatedBy: string;
}

export interface CreateTransactionRequest {
  customerId: number;
  code: 'Credit' | 'Debit';
  type: string;
  amount: number;
  description?: string;
  reference?: string;
  paymentMethod?: string;
  valueDate?: string;
  enteredBy: string;
}

export interface BatchCreateTransactionRequest {
  transactions: CreateTransactionRequest[];
  stopOnFirstError?: boolean;
}

export interface BatchTransactionLineResult {
  index: number;
  isSuccess: boolean;
  error?: string;
  errorCode?: string;
  transaction?: Transaction;
}

export interface BatchTransactionResponse {
  totalRequested: number;
  succeeded: number;
  failed: number;
  results: BatchTransactionLineResult[];
}

export interface UpdateCreditLimitRequest {
  creditLimit: number;
  wagerLimit: number;
  hardCreditLimit: number;
  updatedBy: string;
}

export interface CustomerPermissions {
  customerId: number;
  webSportsEnabled: boolean;
  callInEnabled: boolean;
  internetEnabled: boolean;
  racebookEnabled: boolean;
  casinoEnabled: boolean;
  lotteryEnabled: boolean;
  liveDealerEnabled: boolean;
  horseEnabled: boolean;
  parlayEnabled: boolean;
  teaserEnabled: boolean;
  ifBetEnabled: boolean;
  reverseEnabled: boolean;
  accountLocked: boolean;
  receiveAlerts: boolean;
  updatedAt: string;
  updatedBy: string;
}

export interface UpdateCustomerPermissionsRequest {
  webSportsEnabled?: boolean;
  callInEnabled?: boolean;
  internetEnabled?: boolean;
  racebookEnabled?: boolean;
  casinoEnabled?: boolean;
  lotteryEnabled?: boolean;
  liveDealerEnabled?: boolean;
  horseEnabled?: boolean;
  parlayEnabled?: boolean;
  teaserEnabled?: boolean;
  ifBetEnabled?: boolean;
  reverseEnabled?: boolean;
  accountLocked?: boolean;
  receiveAlerts?: boolean;
  updatedBy: string;
}

export interface CustomerWagerLimits {
  customerId: number;
  maxStraightWager: number;
  maxParlayWager: number;
  maxParlayPayout: number;
  maxTeaserWager: number;
  maxIfBetWager: number;
  maxLotteryPick3: number;
  maxLotteryPick4: number;
  maxParlayLegs: number;
  minimumWager: number;
}

export interface UpdateCustomerWagerLimitsRequest {
  maxStraightWager?: number;
  maxParlayWager?: number;
  maxParlayPayout?: number;
  maxTeaserWager?: number;
  maxIfBetWager?: number;
  maxLotteryPick3?: number;
  maxLotteryPick4?: number;
  maxParlayLegs?: number;
  minimumWager?: number;
  updatedBy: string;
}

export interface CustomerCasinoLimits {
  customerId: number;
  casinoWagerLimit: number;
  casinoCreditLimit: number;
}

export interface UpdateCustomerCasinoLimitsRequest {
  casinoWagerLimit?: number;
  casinoCreditLimit?: number;
  updatedBy: string;
}

export interface CustomerComment {
  id: number;
  customerId: number;
  body: string;
  visibleToCustomer: boolean;
  visibleToAgent: boolean;
  createdAt: string;
  createdBy: string;
}

export interface AddCustomerCommentRequest {
  body: string;
  visibleToCustomer: boolean;
  visibleToAgent: boolean;
  createdBy: string;
}

export interface CustomerFreePlay {
  id: number;
  customerId: number;
  amount: number;
  description: string;
  issuedAt: string;
  issuedBy: string;
  expiresAt?: string;
  isRedeemed: boolean;
  redeemedAmount: number;
}

export interface AwardFreePlayRequest {
  amount: number;
  description: string;
  expiresAt?: string;
  issuedBy: string;
}

// ─── Agent position & figures ─────────────────────────────────────────────────

export interface AgentPosition {
  agentId: number;
  agentLoginName: string;
  totalCustomers: number;
  activeCustomers: number;
  totalCurrentBalance: number;
  totalPendingWager: number;
  totalPendingWagerCount: number;
  totalFreePlay: number;
  totalCreditLimit: number;
  totalAvailableCredit: number;
  asOf: string;
}

export interface AgentPositionSummary {
  agentId: number;
  loginName: string;
  name?: string;
  agentType: string;
  customerCount: number;
  totalBalance: number;
  totalPending: number;
  totalCreditLimit: number;
  totalAvailableCredit: number;
}

export interface CustomerFiguresLine {
  customerId: number;
  loginName: string;
  credits: number;
  debits: number;
  net: number;
  casinoAdj: number;
  freePlay: number;
  currentBalance: number;
  txCount: number;
}

export interface AgentFigures {
  agentId: number;
  agentLoginName: string;
  from: string;
  to: string;
  totalCredits: number;
  totalDebits: number;
  netTransactions: number;
  casinoAdjustments: number;
  freePlayIssued: number;
  transactionCount: number;
  customers: CustomerFiguresLine[];
}
