'use strict'

export interface CasinoPlayer {
  customerId: string
  nickname: string
  externalPlayerId: string
  registeredAt: string
}

export interface CasinoSession {
  customerId: string
  nickname: string
  lobbyUrl: string
  casinoBalance: number
  bonusBalance: number
  playthrough: number
}

export interface CasinoBalance {
  availableBalance: number
  casinoBalance: number
  bonusBalance: number
}

export interface TransferResult {
  success: boolean
  availableBalance: number
  casinoBalance: number
  errorMessage?: string
}

export interface ApiError {
  error: string
  code: string
}
