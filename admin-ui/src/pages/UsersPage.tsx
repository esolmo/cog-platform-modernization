import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { usersApi, type UserDto, type CreateUserRequest, type UpdateUserRequest } from '../api/users';
import { rolesApi } from '../api/roles';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const createSchema = z.object({
  username: z.string().min(2, 'Min 2 characters'),
  password: z.string().min(8, 'Min 8 characters'),
  email: z.string().email('Valid email required'),
  firstName: z.string().min(1, 'Required'),
  lastName: z.string().min(1, 'Required'),
  maxAccessLevel: z.string(),
  roleIds: z.array(z.number()).default([]),
});
type CreateForm = z.infer<typeof createSchema>;

const editSchema = z.object({
  email: z.string().email('Valid email required'),
  firstName: z.string().min(1, 'Required'),
  lastName: z.string().min(1, 'Required'),
  maxAccessLevel: z.string(),
  isActive: z.boolean(),
  roleIds: z.array(z.number()).default([]),
});
type EditForm = z.infer<typeof editSchema>;

export default function UsersPage() {
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['users', page, search],
    queryFn: () => usersApi.list(page, 20, search || undefined),
  });

  const { data: roles } = useQuery({
    queryKey: ['roles'],
    queryFn: rolesApi.list,
  });

  const { register, handleSubmit, reset, control, formState: { errors } } = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
    defaultValues: { roleIds: [], maxAccessLevel: 'Standard' },
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateUserRequest) => usersApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['users'] });
      setShowCreate(false);
      reset();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => usersApi.delete(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['users'] }),
  });

  const {
    register: editRegister,
    handleSubmit: editHandleSubmit,
    reset: editReset,
    control: editControl,
    formState: { errors: editErrors },
  } = useForm<EditForm>({
    resolver: zodResolver(editSchema),
    defaultValues: { roleIds: [], isActive: true, maxAccessLevel: 'Standard' },
  });

  const editMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateUserRequest }) =>
      usersApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['users'] });
      setSelectedUser(null);
      editReset();
    },
  });

  // Populate edit form whenever a user is selected
  useEffect(() => {
    if (!selectedUser) return;
    const userRoleIds = (roles ?? [])
      .filter((r) => selectedUser.roles.includes(r.name))
      .map((r) => r.id);
    editReset({
      email: selectedUser.email,
      firstName: selectedUser.firstName,
      lastName: selectedUser.lastName,
      maxAccessLevel: selectedUser.maxAccessLevel,
      isActive: selectedUser.isActive,
      roleIds: userRoleIds,
    });
  }, [selectedUser, roles, editReset]);

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-gray-900">Users</h2>
        <button
          onClick={() => setShowCreate(true)}
          className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700"
        >
          + New User
        </button>
      </div>

      {/* Search */}
      <div className="mb-4">
        <input
          type="search"
          placeholder="Search by username, email, or name..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          className="w-full max-w-md border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
        />
      </div>

      {/* Table */}
      {isLoading ? (
        <p className="text-gray-500">Loading...</p>
      ) : (
        <>
          <div className="bg-white rounded-xl shadow overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">Username</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">Email</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">Name</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">Roles</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">Status</th>
                  <th className="px-4 py-3 text-left font-medium text-gray-600">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {data?.items.map((user) => (
                  <tr key={user.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium text-gray-900">{user.username}</td>
                    <td className="px-4 py-3 text-gray-600">{user.email}</td>
                    <td className="px-4 py-3 text-gray-600">{user.firstName} {user.lastName}</td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {user.roles.map((r) => (
                          <span key={r} className="bg-indigo-100 text-indigo-700 text-xs px-2 py-0.5 rounded-full">
                            {r}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${user.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                        {user.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <button
                        onClick={() => setSelectedUser(user)}
                        className="text-indigo-600 hover:underline text-xs mr-3"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => {
                          if (confirm(`Deactivate ${user.username}?`)) deleteMutation.mutate(user.id);
                        }}
                        className="text-red-500 hover:underline text-xs"
                      >
                        Deactivate
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
              <span>{data.totalCount} users total</span>
              <div className="flex gap-2">
                <button
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={!data.hasPrevious}
                  className="px-3 py-1 border rounded disabled:opacity-40"
                >
                  Previous
                </button>
                <span className="px-3 py-1">Page {data.page} of {data.totalPages}</span>
                <button
                  onClick={() => setPage((p) => p + 1)}
                  disabled={!data.hasNext}
                  className="px-3 py-1 border rounded disabled:opacity-40"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </>
      )}

      {/* Create modal */}
      {showCreate && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-2xl p-8 w-full max-w-lg">
            <h3 className="text-lg font-bold mb-5">New User</h3>
            <form onSubmit={handleSubmit((d) => createMutation.mutate(d))} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium">Username</label>
                  <input {...register('username')} autoComplete="off" className="input mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
                  {errors.username && <p className="text-red-500 text-xs mt-1">{errors.username.message}</p>}
                </div>
                <div>
                  <label className="text-sm font-medium">Password</label>
                  <input type="password" {...register('password')} autoComplete="new-password" className="input mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
                  {errors.password && <p className="text-red-500 text-xs mt-1">{errors.password.message}</p>}
                </div>
                <div>
                  <label className="text-sm font-medium">First name</label>
                  <input {...register('firstName')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
                </div>
                <div>
                  <label className="text-sm font-medium">Last name</label>
                  <input {...register('lastName')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
                </div>
              </div>
              <div>
                <label className="text-sm font-medium">Email</label>
                <input {...register('email')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
                {errors.email && <p className="text-red-500 text-xs mt-1">{errors.email.message}</p>}
              </div>
              <div>
                <label className="text-sm font-medium">Access level</label>
                <select {...register('maxAccessLevel')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm bg-white">
                  <option value="Standard">Standard</option>
                  <option value="Supervisor">Supervisor</option>
                  <option value="Manager">Manager</option>
                  <option value="Admin">Admin</option>
                </select>
              </div>
              <div>
                <label className="text-sm font-medium">Roles</label>
                {roles && roles.length > 0 ? (
                  <Controller
                    name="roleIds"
                    control={control}
                    render={({ field }) => (
                      <div className="mt-1 border rounded-lg divide-y max-h-40 overflow-y-auto">
                        {roles.filter((r) => r.isActive).map((role) => (
                          <label key={role.id} className="flex items-center gap-3 px-3 py-2 cursor-pointer hover:bg-gray-50 text-sm">
                            <input
                              type="checkbox"
                              className="rounded border-gray-300 text-indigo-600"
                              checked={field.value.includes(role.id)}
                              onChange={(e) => {
                                const next = e.target.checked
                                  ? [...field.value, role.id]
                                  : field.value.filter((id) => id !== role.id);
                                field.onChange(next);
                              }}
                            />
                            <span className="font-medium">{role.name}</span>
                            {role.description && (
                              <span className="text-gray-400 truncate">{role.description}</span>
                            )}
                          </label>
                        ))}
                      </div>
                    )}
                  />
                ) : (
                  <p className="mt-1 text-xs text-gray-400">No roles available — create roles first.</p>
                )}
              </div>

              {createMutation.isError && (
                <p className="text-red-500 text-sm">Failed to create user. Username or email may already exist.</p>
              )}

              <div className="flex gap-3 pt-2">
                <button
                  type="submit"
                  disabled={createMutation.isPending}
                  className="bg-indigo-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50"
                >
                  {createMutation.isPending ? 'Creating...' : 'Create user'}
                </button>
                <button
                  type="button"
                  onClick={() => { setShowCreate(false); reset(); }}
                  className="border px-5 py-2 rounded-lg text-sm"
                >
                  Cancel
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit modal */}
      {selectedUser && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-2xl p-8 w-full max-w-lg">
            <h3 className="text-lg font-bold mb-1">Edit User</h3>
            <p className="text-sm text-gray-500 mb-5">@{selectedUser.username}</p>
            <form
              onSubmit={editHandleSubmit((d) =>
                editMutation.mutate({ id: selectedUser.id, data: d })
              )}
              className="space-y-4"
            >
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium">First name</label>
                  <input {...editRegister('firstName')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
                  {editErrors.firstName && <p className="text-red-500 text-xs mt-1">{editErrors.firstName.message}</p>}
                </div>
                <div>
                  <label className="text-sm font-medium">Last name</label>
                  <input {...editRegister('lastName')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
                  {editErrors.lastName && <p className="text-red-500 text-xs mt-1">{editErrors.lastName.message}</p>}
                </div>
              </div>
              <div>
                <label className="text-sm font-medium">Email</label>
                <input {...editRegister('email')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
                {editErrors.email && <p className="text-red-500 text-xs mt-1">{editErrors.email.message}</p>}
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium">Access level</label>
                  <select {...editRegister('maxAccessLevel')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm bg-white">
                    <option value="Standard">Standard</option>
                    <option value="Supervisor">Supervisor</option>
                    <option value="Manager">Manager</option>
                    <option value="Admin">Admin</option>
                  </select>
                </div>
                <div className="flex items-end pb-2">
                  <label className="flex items-center gap-2 text-sm font-medium cursor-pointer">
                    <input type="checkbox" {...editRegister('isActive')} className="rounded border-gray-300 text-indigo-600" />
                    Active
                  </label>
                </div>
              </div>
              <div>
                <label className="text-sm font-medium">Roles</label>
                {roles && roles.length > 0 ? (
                  <Controller
                    name="roleIds"
                    control={editControl}
                    render={({ field }) => (
                      <div className="mt-1 border rounded-lg divide-y max-h-40 overflow-y-auto">
                        {roles.filter((r) => r.isActive).map((role) => (
                          <label key={role.id} className="flex items-center gap-3 px-3 py-2 cursor-pointer hover:bg-gray-50 text-sm">
                            <input
                              type="checkbox"
                              className="rounded border-gray-300 text-indigo-600"
                              checked={field.value.includes(role.id)}
                              onChange={(e) => {
                                const next = e.target.checked
                                  ? [...field.value, role.id]
                                  : field.value.filter((id) => id !== role.id);
                                field.onChange(next);
                              }}
                            />
                            <span className="font-medium">{role.name}</span>
                            {role.description && (
                              <span className="text-gray-400 truncate">{role.description}</span>
                            )}
                          </label>
                        ))}
                      </div>
                    )}
                  />
                ) : (
                  <p className="mt-1 text-xs text-gray-400">No roles available.</p>
                )}
              </div>

              {editMutation.isError && (
                <p className="text-red-500 text-sm">Failed to update user.</p>
              )}

              <div className="flex gap-3 pt-2">
                <button
                  type="submit"
                  disabled={editMutation.isPending}
                  className="bg-indigo-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50"
                >
                  {editMutation.isPending ? 'Saving...' : 'Save changes'}
                </button>
                <button
                  type="button"
                  onClick={() => { setSelectedUser(null); editReset(); }}
                  className="border px-5 py-2 rounded-lg text-sm"
                >
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
