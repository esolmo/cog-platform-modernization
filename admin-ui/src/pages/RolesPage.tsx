import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { rolesApi, type RoleDto, type CreateRoleRequest } from '../api/roles';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const schema = z.object({
  name: z.string().min(2, 'Min 2 characters'),
  description: z.string().min(1, 'Required'),
  permissionIds: z.array(z.number()).default([]),
});
type FormData = z.infer<typeof schema>;

export default function RolesPage() {
  const qc = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [expandedRole, setExpandedRole] = useState<number | null>(null);

  const { data: roles, isLoading } = useQuery({
    queryKey: ['roles'],
    queryFn: rolesApi.list,
  });

  const { data: permissions } = useQuery({
    queryKey: ['permissions'],
    queryFn: rolesApi.permissions,
  });

  const { register, handleSubmit, control, reset, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { permissionIds: [] },
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateRoleRequest) => rolesApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['roles'] });
      setShowCreate(false);
      reset();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => rolesApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['roles'] }),
  });

  const permissionsByCategory = permissions?.reduce<Record<string, typeof permissions>>((acc, p) => {
    (acc[p.category] ??= []).push(p);
    return acc;
  }, {}) ?? {};

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Roles & Permissions</h2>
        <button
          onClick={() => setShowCreate(true)}
          className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700"
        >
          + New Role
        </button>
      </div>

      {isLoading ? (
        <p className="text-gray-500">Loading...</p>
      ) : (
        <div className="space-y-3">
          {roles?.map((role) => (
            <div key={role.id} className="bg-white rounded-xl shadow overflow-hidden">
              <div
                className="flex items-center justify-between px-6 py-4 cursor-pointer hover:bg-gray-50"
                onClick={() => setExpandedRole(expandedRole === role.id ? null : role.id)}
              >
                <div className="flex items-center gap-3">
                  <span className="font-semibold text-gray-900">{role.name}</span>
                  {role.isSystemRole && (
                    <span className="text-xs bg-amber-100 text-amber-700 px-2 py-0.5 rounded-full">System</span>
                  )}
                  <span className={`text-xs px-2 py-0.5 rounded-full ${role.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                    {role.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
                <div className="flex items-center gap-4 text-sm text-gray-500">
                  <span>{role.userCount} users</span>
                  <span>{role.permissions.length} permissions</span>
                  {!role.isSystemRole && (
                    <button
                      onClick={(e) => { e.stopPropagation(); if (confirm(`Delete role ${role.name}?`)) deleteMutation.mutate(role.id); }}
                      className="text-red-500 hover:underline text-xs"
                    >
                      Delete
                    </button>
                  )}
                </div>
              </div>

              {expandedRole === role.id && (
                <div className="px-6 pb-4 border-t bg-gray-50">
                  <p className="text-sm text-gray-500 py-2">{role.description}</p>
                  <div className="flex flex-wrap gap-2 mt-2">
                    {role.permissions.map((p) => (
                      <span key={p.id} className="bg-white border text-gray-700 text-xs px-2 py-1 rounded-md" title={p.description}>
                        {p.name}
                      </span>
                    ))}
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Create modal */}
      {showCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-2xl p-8 w-full max-w-2xl max-h-[90vh] overflow-y-auto">
            <h3 className="text-lg font-bold mb-5">New Role</h3>
            <form onSubmit={handleSubmit((d) => createMutation.mutate(d))} className="space-y-4">
              <div>
                <label className="text-sm font-medium">Role name</label>
                <input {...register('name')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
                {errors.name && <p className="text-red-500 text-xs mt-1">{errors.name.message}</p>}
              </div>
              <div>
                <label className="text-sm font-medium">Description</label>
                <input {...register('description')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">Permissions</label>
                <Controller
                  name="permissionIds"
                  control={control}
                  render={({ field }) => (
                    <div className="space-y-4">
                      {Object.entries(permissionsByCategory).map(([category, perms]) => (
                        <div key={category}>
                          <p className="text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">{category}</p>
                          <div className="grid grid-cols-2 gap-2">
                            {perms.map((p) => (
                              <label key={p.id} className="flex items-center gap-2 text-sm cursor-pointer">
                                <input
                                  type="checkbox"
                                  checked={field.value.includes(p.id)}
                                  onChange={(e) => {
                                    if (e.target.checked) field.onChange([...field.value, p.id]);
                                    else field.onChange(field.value.filter((id) => id !== p.id));
                                  }}
                                  className="rounded"
                                />
                                <span title={p.description}>{p.name}</span>
                              </label>
                            ))}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                />
              </div>

              {createMutation.isError && (
                <p className="text-red-500 text-sm">Failed to create role.</p>
              )}

              <div className="flex gap-3 pt-2">
                <button
                  type="submit"
                  disabled={createMutation.isPending}
                  className="bg-indigo-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50"
                >
                  {createMutation.isPending ? 'Creating...' : 'Create role'}
                </button>
                <button type="button" onClick={() => { setShowCreate(false); reset(); }} className="border px-5 py-2 rounded-lg text-sm">
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
