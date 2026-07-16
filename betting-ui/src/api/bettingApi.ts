'use strict'

import { apiClient } from './client'
import type {
  CreateGamePeriodRequest,
  CreateGameRequest,
  CreateSportTypeRequest,
  CreateWagerRequest,
  Game,
  GamePeriod,
  PagedResult,
  SetMoneyLineRequest,
  SetSpreadRequest,
  SetTotalRequest,
  SportType,
  SportTypeDetail,
  UpdateGameRequest,
  UpdateGameStatusRequest,
  UpdateSportTypeRequest,
  Wager,
} from '@/types/betting'

export const bettingApi = {
  getSportTypes: async (): Promise<SportType[]> => {
    const { data } = await apiClient.get<SportType[]>('/games/sports')
    return data
  },

  getGames: async (params: {
    sportTypeId?: number
    dateFrom?: string
    dateTo?: string
    page?: number
    pageSize?: number
  }): Promise<PagedResult<Game>> => {
    const { data } = await apiClient.get<PagedResult<Game>>('/games', { params })
    return data
  },

  getGame: async (id: number): Promise<Game> => {
    const { data } = await apiClient.get<Game>(`/games/${id}`)
    return data
  },

  createWager: async (request: CreateWagerRequest): Promise<Wager> => {
    const { data } = await apiClient.post<Wager>('/wagers', request)
    return data
  },

  getWager: async (id: number): Promise<Wager> => {
    const { data } = await apiClient.get<Wager>(`/wagers/${id}`)
    return data
  },

  getWagersByCustomer: async (customerId: number, page = 1, pageSize = 25): Promise<PagedResult<Wager>> => {
    const { data } = await apiClient.get<PagedResult<Wager>>('/wagers', {
      params: { customerId, page, pageSize },
    })
    return data
  },

  getPendingWagers: async (agentId: number, page = 1, pageSize = 50): Promise<PagedResult<Wager>> => {
    const { data } = await apiClient.get<PagedResult<Wager>>('/wagers/pending', {
      params: { agentId, page, pageSize },
    })
    return data
  },

  cancelWager: async (id: number): Promise<void> => {
    await apiClient.delete(`/wagers/${id}`)
  },

  // ── Admin: sport types ────────────────────────────────────────────────────

  getAllSportTypes: async (): Promise<SportTypeDetail[]> => {
    const { data } = await apiClient.get<SportTypeDetail[]>('/games/sports', {
      params: { includeInactive: true },
    })
    return data
  },

  createSportType: async (request: CreateSportTypeRequest): Promise<SportTypeDetail> => {
    const { data } = await apiClient.post<SportTypeDetail>('/games/sports', request)
    return data
  },

  updateSportType: async (id: number, request: UpdateSportTypeRequest): Promise<SportTypeDetail> => {
    const { data } = await apiClient.put<SportTypeDetail>(`/games/sports/${id}`, request)
    return data
  },

  // ── Admin: games ──────────────────────────────────────────────────────────

  createGame: async (request: CreateGameRequest): Promise<Game> => {
    const { data } = await apiClient.post<Game>('/games', request)
    return data
  },

  updateGame: async (id: number, request: UpdateGameRequest): Promise<Game> => {
    const { data } = await apiClient.put<Game>(`/games/${id}`, request)
    return data
  },

  updateGameStatus: async (id: number, request: UpdateGameStatusRequest): Promise<Game> => {
    const { data } = await apiClient.patch<Game>(`/games/${id}/status`, request)
    return data
  },

  deleteGame: async (id: number): Promise<void> => {
    await apiClient.delete(`/games/${id}`)
  },

  addPeriod: async (gameId: number, request: CreateGamePeriodRequest): Promise<GamePeriod> => {
    const { data } = await apiClient.post<GamePeriod>(`/games/${gameId}/periods`, request)
    return data
  },

  removePeriod: async (gameId: number, periodId: number): Promise<void> => {
    await apiClient.delete(`/games/${gameId}/periods/${periodId}`)
  },

  // ── Admin: lines ──────────────────────────────────────────────────────────

  setSpread: async (periodId: number, request: SetSpreadRequest): Promise<void> => {
    await apiClient.put(`/lines/${periodId}/spread`, request)
  },

  setMoneyLine: async (periodId: number, request: SetMoneyLineRequest): Promise<void> => {
    await apiClient.put(`/lines/${periodId}/moneyline`, request)
  },

  setTotal: async (periodId: number, request: SetTotalRequest): Promise<void> => {
    await apiClient.put(`/lines/${periodId}/total`, request)
  },
}
