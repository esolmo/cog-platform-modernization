import { apiClient } from './client';

export interface UserDto {
  id: number;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  maxAccessLevel: string;
  createdAt: string;
  lastLoginAt: string | null;
  roles: string[];
}

export interface UserPagedResult {
  items: UserDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  email: string;
  firstName: string;
  lastName: string;
  maxAccessLevel: string;
  roleIds: number[];
}

export interface UpdateUserRequest {
  email: string;
  firstName: string;
  lastName: string;
  maxAccessLevel: string;
  isActive: boolean;
  roleIds: number[];
}

export const usersApi = {
  list: (page = 1, pageSize = 20, search?: string) =>
    apiClient.get<UserPagedResult>('/users', { params: { page, pageSize, search } }).then((r) => r.data),

  get: (id: number) =>
    apiClient.get<UserDto>(`/users/${id}`).then((r) => r.data),

  create: (data: CreateUserRequest) =>
    apiClient.post<UserDto>('/users', data).then((r) => r.data),

  update: (id: number, data: UpdateUserRequest) =>
    apiClient.put<UserDto>(`/users/${id}`, data).then((r) => r.data),

  resetPassword: (id: number, newPassword: string) =>
    apiClient.post(`/users/${id}/reset-password`, { newPassword }),

  delete: (id: number) =>
    apiClient.delete(`/users/${id}`),
};
