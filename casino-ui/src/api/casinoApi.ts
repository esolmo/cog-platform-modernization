'use strict'

import axios from 'axios'
import type { CasinoPlayer, CasinoSession, CasinoBalance, TransferResult } from '@/types/casino'
import { useAuthStore } from '@/stores/authStore'

const client = axios.create({ baseURL: '/api' })

client.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

export const casinoApi = {
  register: (nickname: string): Promise<CasinoPlayer> =>
    client.post<CasinoPlayer>('/Casino/register', { nickname }).then((r) => r.data),

  getSession: (): Promise<CasinoSession> =>
    client.get<CasinoSession>('/Casino/session').then((r) => r.data),

  getBalance: (): Promise<CasinoBalance> =>
    client.get<CasinoBalance>('/Casino/balance').then((r) => r.data),

  deposit: (amount: number): Promise<TransferResult> =>
    client.post<TransferResult>('/Casino/deposit', { amount }).then((r) => r.data),

  withdraw: (amount: number): Promise<TransferResult> =>
    client.post<TransferResult>('/Casino/withdraw', { amount }).then((r) => r.data),
}
