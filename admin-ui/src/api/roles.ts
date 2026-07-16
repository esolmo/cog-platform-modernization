import { apiClient } from './client';

export interface PermissionDto {
  id: number;
  name: string;
  category: string;
  description: string;
}

export interface RoleDto {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
  isSystemRole: boolean;
  permissions: PermissionDto[];
  userCount: number;
}

export interface CreateRoleRequest {
  name: string;
  description: string;
  permissionIds: number[];
}

export interface UpdateRoleRequest {
  description: string;
  isActive: boolean;
  permissionIds: number[];
}

export const rolesApi = {
  list: () =>
    apiClient.get<RoleDto[]>('/roles').then((r) => r.data),

  get: (id: number) =>
    apiClient.get<RoleDto>(`/roles/${id}`).then((r) => r.data),

  permissions: () =>
    apiClient.get<PermissionDto[]>('/roles/permissions').then((r) => r.data),

  create: (data: CreateRoleRequest) =>
    apiClient.post<RoleDto>('/roles', data).then((r) => r.data),

  update: (id: number, data: UpdateRoleRequest) =>
    apiClient.put<RoleDto>(`/roles/${id}`, data).then((r) => r.data),

  delete: (id: number) =>
    apiClient.delete(`/roles/${id}`),
};
