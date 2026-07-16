'use strict'

import { create } from 'zustand'
import { persist } from 'zustand/middleware'

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  agentId: number | null
  loginName: string | null
  roles: string[]
  isAuthenticated: boolean
  login: (accessToken: string, refreshToken: string, agentId: number, loginName: string, roles: string[]) => void
  logout: () => void
  setTokens: (accessToken: string, refreshToken: string) => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      agentId: null,
      loginName: null,
      roles: [],
      isAuthenticated: false,

      login: (accessToken, refreshToken, agentId, loginName, roles) =>
        set({ accessToken, refreshToken, agentId, loginName, roles, isAuthenticated: true }),

      logout: () =>
        set({ accessToken: null, refreshToken: null, agentId: null, loginName: null, roles: [], isAuthenticated: false }),

      setTokens: (accessToken, refreshToken) =>
        set({ accessToken, refreshToken }),
    }),
    {
      name: 'cog-auth',
      partialize: (state) => ({
        accessToken: state.accessToken,
        refreshToken: state.refreshToken,
        agentId: state.agentId,
        loginName: state.loginName,
        roles: state.roles,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
)
