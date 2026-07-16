'use strict'

import { create } from 'zustand'
import type { WagerItemType, WagerSide, WagerType } from '@/types/betting'

export interface WagerDraftItem {
  gamePeriodId: number
  homeTeam: string
  awayTeam: string
  periodDescription: string
  itemType: WagerItemType
  side: WagerSide
  line: number
}

interface WagerDraftState {
  customerId: number | null
  wagerType: WagerType
  riskAmount: string
  items: WagerDraftItem[]
  setCustomer: (customerId: number) => void
  setWagerType: (wagerType: WagerType) => void
  setRiskAmount: (amount: string) => void
  addItem: (item: WagerDraftItem) => void
  removeItem: (gamePeriodId: number) => void
  clearDraft: () => void
}

export const useWagerDraftStore = create<WagerDraftState>()((set) => ({
  customerId: null,
  wagerType: 'Straight',
  riskAmount: '',
  items: [],

  setCustomer: (customerId) => set({ customerId }),
  setWagerType: (wagerType) => set({ wagerType }),
  setRiskAmount: (riskAmount) => set({ riskAmount }),

  addItem: (item) =>
    set((state) => ({
      items: [...state.items.filter((i) => i.gamePeriodId !== item.gamePeriodId), item],
    })),

  removeItem: (gamePeriodId) =>
    set((state) => ({
      items: state.items.filter((i) => i.gamePeriodId !== gamePeriodId),
    })),

  clearDraft: () => set({ customerId: null, wagerType: 'Straight', riskAmount: '', items: [] }),
}))
