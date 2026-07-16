'use strict'

export type WagerType = 'Straight' | 'Parlay' | 'Teaser' | 'IfBet' | 'Reverse' | 'ActionReverse'
export type WagerStatus = 'Pending' | 'Won' | 'Lost' | 'Push' | 'Cancelled' | 'NoAction'
export type WagerItemType = 'Spread' | 'MoneyLine' | 'Total' | 'TeamTotal'
export type WagerSide = 'Home' | 'Away' | 'Over' | 'Under'
export type GameStatus = 'Upcoming' | 'InProgress' | 'Final' | 'Postponed' | 'Cancelled'

export interface SportType {
  id: number
  name: string
  code: string
}

export interface LineSet {
  id: number
  spread: number | null
  spreadJuice: number | null
  homeMoneyLine: number | null
  awayMoneyLine: number | null
  total: number | null
  overJuice: number | null
  underJuice: number | null
  offeringSpread: boolean
  offeringMoneyLine: boolean
  offeringTotal: boolean
  lastModified: string
}

export interface GamePeriod {
  id: number
  periodDescription: string
  periodNumber: number
  lines: LineSet | null
}

export interface Game {
  id: number
  sportTypeId: number
  sportName: string
  homeTeam: string
  awayTeam: string
  gameDate: string
  status: GameStatus
  rotationNumber: string | null
  periods: GamePeriod[]
}

export interface WagerItem {
  id: number
  gamePeriodId: number
  homeTeam: string
  awayTeam: string
  periodDescription: string
  itemType: WagerItemType
  side: WagerSide
  lineAtTimeOfWager: number
  status: WagerItemStatus
}

export type WagerItemStatus = 'Pending' | 'Won' | 'Lost' | 'Push' | 'NoAction'

export interface Wager {
  id: number
  customerId: number
  customerLoginName: string
  wagerType: WagerType
  status: WagerStatus
  riskAmount: number
  winAmount: number
  actualPayout: number | null
  ticketNumber: string | null
  createdAt: string
  gradedAt: string | null
  items: WagerItem[]
}

export interface CreateWagerRequest {
  customerId: number
  wagerType: WagerType
  riskAmount: number
  idempotencyKey?: string
  items: {
    gamePeriodId: number
    itemType: WagerItemType
    side: WagerSide
  }[]
}

// ── Admin / LinesManager types ────────────────────────────────────────────────

export interface SportTypeDetail {
  id: number
  name: string
  code: string
  isActive: boolean
  displayOrder: number
}

export interface CreateGameRequest {
  sportTypeId: number
  homeTeam: string
  awayTeam: string
  gameDate: string
  rotationNumber?: string
  periods: { periodDescription: string; periodNumber: number }[]
}

export interface UpdateGameRequest {
  homeTeam: string
  awayTeam: string
  gameDate: string
  rotationNumber?: string
}

export interface UpdateGameStatusRequest {
  status: GameStatus
}

export interface CreateGamePeriodRequest {
  periodDescription: string
  periodNumber: number
}

export interface CreateSportTypeRequest {
  name: string
  code: string
  displayOrder: number
}

export interface UpdateSportTypeRequest {
  name: string
  code: string
  isActive: boolean
  displayOrder: number
}

export interface SetSpreadRequest {
  spread: number
  juice: number
}

export interface SetMoneyLineRequest {
  homeMoneyLine: number
  awayMoneyLine: number
}

export interface SetTotalRequest {
  total: number
  overJuice: number
  underJuice: number
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}
