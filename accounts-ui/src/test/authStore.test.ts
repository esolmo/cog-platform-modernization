// @vitest-environment node
import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '../stores/authStore';

describe('authStore', () => {
  beforeEach(() => {
    useAuthStore.getState().clearAuth();
  });

  it('initialises with empty auth state', () => {
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.accessToken).toBeNull();
    expect(state.refreshToken).toBeNull();
  });

  it('setTokens marks as authenticated', () => {
    useAuthStore.getState().setTokens('access-abc', 'refresh-xyz');
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.accessToken).toBe('access-abc');
    expect(state.refreshToken).toBe('refresh-xyz');
  });

  it('setUser stores agent identity', () => {
    useAuthStore.getState().setUser(42, 'agent01', ['Agent', 'LinesManager']);
    const state = useAuthStore.getState();
    expect(state.agentId).toBe(42);
    expect(state.loginName).toBe('agent01');
    expect(state.roles).toEqual(['Agent', 'LinesManager']);
  });

  it('clearAuth resets all fields', () => {
    useAuthStore.getState().setTokens('token', 'refresh');
    useAuthStore.getState().setUser(1, 'user', ['Admin']);
    useAuthStore.getState().clearAuth();

    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.accessToken).toBeNull();
    expect(state.agentId).toBeNull();
    expect(state.roles).toEqual([]);
  });
});
