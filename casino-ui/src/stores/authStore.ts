'use strict'

import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface AuthState {
  accessToken: string | null
  loginName: string | null
  customerId: string | null
  setAuth: (token: string, loginName: string, customerId: string) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      loginName: null,
      customerId: null,
      setAuth: (accessToken, loginName, customerId) =>
        set({ accessToken, loginName, customerId }),
      logout: () => set({ accessToken: null, loginName: null, customerId: null }),
    }),
    { name: 'cog-casino-auth' }
  )
)
