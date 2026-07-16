import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  agentId: number | null;
  loginName: string | null;
  roles: string[];
  isAuthenticated: boolean;
  setTokens: (accessToken: string, refreshToken: string) => void;
  setUser: (agentId: number, loginName: string, roles: string[]) => void;
  clearAuth: () => void;
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

      setTokens: (accessToken, refreshToken) =>
        set({ accessToken, refreshToken, isAuthenticated: true }),

      setUser: (agentId, loginName, roles) =>
        set({ agentId, loginName, roles }),

      clearAuth: () =>
        set({
          accessToken: null,
          refreshToken: null,
          agentId: null,
          loginName: null,
          roles: [],
          isAuthenticated: false,
        }),
    }),
    {
      name: 'cog-accounts-auth',
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
);
