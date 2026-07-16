// @vitest-environment node
import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '../store/authStore';

describe('authStore', () => {
  beforeEach(() => {
    useAuthStore.getState().clearAuth();
  });

  it('initialises as unauthenticated', () => {
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.accessToken).toBeNull();
    expect(state.user).toBeNull();
  });

  it('setTokens marks authenticated', () => {
    useAuthStore.getState().setTokens('access-123', 'refresh-456');
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(true);
    expect(state.accessToken).toBe('access-123');
    expect(state.refreshToken).toBe('refresh-456');
  });

  it('setUser stores user identity', () => {
    useAuthStore.getState().setUser({
      id: 1,
      loginName: 'admin',
      userType: 'Employee',
      domainEntityId: null,
      roles: ['SuperAdmin'],
      permissions: ['Users.Manage', 'Roles.Manage'],
    });
    const state = useAuthStore.getState();
    expect(state.user?.loginName).toBe('admin');
    expect(state.user?.roles).toContain('SuperAdmin');
  });

  it('hasPermission returns true when user has permission', () => {
    useAuthStore.getState().setUser({
      id: 1,
      loginName: 'admin',
      userType: 'Employee',
      domainEntityId: null,
      roles: ['Admin'],
      permissions: ['Users.Manage', 'System.Config'],
    });
    expect(useAuthStore.getState().hasPermission('System.Config')).toBe(true);
    expect(useAuthStore.getState().hasPermission('Users.Delete')).toBe(false);
  });

  it('clearAuth resets all state', () => {
    useAuthStore.getState().setTokens('t', 'r');
    useAuthStore.getState().clearAuth();
    const state = useAuthStore.getState();
    expect(state.isAuthenticated).toBe(false);
    expect(state.accessToken).toBeNull();
    expect(state.user).toBeNull();
  });
});
