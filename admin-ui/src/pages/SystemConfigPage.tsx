import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiClient } from '../api/client';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

interface SystemConfigDto {
  id: number;
  key: string;
  value: string;
  category: string;
  description: string;
  isEncrypted: boolean;
  updatedAt: string;
}

const schema = z.object({
  key: z.string().min(1, 'Required'),
  value: z.string(),
  category: z.string().min(1, 'Required'),
  description: z.string(),
  isEncrypted: z.boolean().default(false),
});
type FormData = z.infer<typeof schema>;

export default function SystemConfigPage() {
  const qc = useQueryClient();
  const [selectedCategory, setSelectedCategory] = useState<string>('');
  const [showForm, setShowForm] = useState(false);
  const [editItem, setEditItem] = useState<SystemConfigDto | null>(null);

  const { data: configs, isLoading } = useQuery({
    queryKey: ['config', selectedCategory],
    queryFn: () =>
      apiClient.get<SystemConfigDto[]>('/config', { params: { category: selectedCategory || undefined } })
        .then((r) => r.data),
  });

  const { register, handleSubmit, reset, setValue, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
  });

  const upsertMutation = useMutation({
    mutationFn: (data: FormData) => apiClient.put('/config', data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['config'] });
      setShowForm(false);
      setEditItem(null);
      reset();
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (key: string) => apiClient.delete(`/config/${encodeURIComponent(key)}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['config'] }),
  });

  const handleEdit = (item: SystemConfigDto) => {
    setEditItem(item);
    setValue('key', item.key);
    setValue('value', item.isEncrypted ? '' : item.value);
    setValue('category', item.category);
    setValue('description', item.description);
    setValue('isEncrypted', item.isEncrypted);
    setShowForm(true);
  };

  const categories = [...new Set(configs?.map((c) => c.category) ?? [])];

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-gray-900">System Configuration</h2>
        <button
          onClick={() => { setEditItem(null); reset(); setShowForm(true); }}
          className="bg-indigo-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700"
        >
          + Add Setting
        </button>
      </div>

      {/* Category filter */}
      <div className="flex gap-2 mb-4 flex-wrap">
        <button
          onClick={() => setSelectedCategory('')}
          className={`px-3 py-1 rounded-full text-sm ${!selectedCategory ? 'bg-indigo-600 text-white' : 'bg-gray-200 text-gray-700'}`}
        >
          All
        </button>
        {categories.map((cat) => (
          <button
            key={cat}
            onClick={() => setSelectedCategory(cat)}
            className={`px-3 py-1 rounded-full text-sm ${selectedCategory === cat ? 'bg-indigo-600 text-white' : 'bg-gray-200 text-gray-700'}`}
          >
            {cat}
          </button>
        ))}
      </div>

      {isLoading ? (
        <p className="text-gray-500">Loading...</p>
      ) : (
        <div className="bg-white rounded-xl shadow overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Key</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Value</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Category</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Description</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {configs?.map((cfg) => (
                <tr key={cfg.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-mono text-gray-900">{cfg.key}</td>
                  <td className="px-4 py-3 text-gray-600">
                    {cfg.isEncrypted ? <span className="text-gray-400 italic">[encrypted]</span> : cfg.value}
                  </td>
                  <td className="px-4 py-3">
                    <span className="bg-blue-100 text-blue-700 text-xs px-2 py-0.5 rounded-full">{cfg.category}</span>
                  </td>
                  <td className="px-4 py-3 text-gray-500 text-xs max-w-xs truncate">{cfg.description}</td>
                  <td className="px-4 py-3">
                    <button onClick={() => handleEdit(cfg)} className="text-indigo-600 hover:underline text-xs mr-3">Edit</button>
                    <button
                      onClick={() => { if (confirm(`Delete key "${cfg.key}"?`)) deleteMutation.mutate(cfg.key); }}
                      className="text-red-500 hover:underline text-xs"
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Form modal */}
      {showForm && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-2xl p-8 w-full max-w-lg">
            <h3 className="text-lg font-bold mb-5">{editItem ? 'Edit Setting' : 'New Setting'}</h3>
            <form onSubmit={handleSubmit((d) => upsertMutation.mutate(d))} className="space-y-4">
              <div>
                <label className="text-sm font-medium">Key</label>
                <input {...register('key')} readOnly={!!editItem} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
                {errors.key && <p className="text-red-500 text-xs mt-1">{errors.key.message}</p>}
              </div>
              <div>
                <label className="text-sm font-medium">Value {editItem?.isEncrypted && <span className="text-gray-400">(leave blank to keep current)</span>}</label>
                <input {...register('value')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm font-medium">Category</label>
                  <input {...register('category')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
                </div>
                <div className="flex items-end">
                  <label className="flex items-center gap-2 text-sm cursor-pointer">
                    <input type="checkbox" {...register('isEncrypted')} className="rounded" />
                    <span>Encrypted</span>
                  </label>
                </div>
              </div>
              <div>
                <label className="text-sm font-medium">Description</label>
                <input {...register('description')} className="mt-1 w-full border px-3 py-2 rounded-lg text-sm" />
              </div>
              <div className="flex gap-3 pt-2">
                <button
                  type="submit"
                  disabled={upsertMutation.isPending}
                  className="bg-indigo-600 text-white px-5 py-2 rounded-lg text-sm font-medium hover:bg-indigo-700 disabled:opacity-50"
                >
                  Save
                </button>
                <button type="button" onClick={() => { setShowForm(false); setEditItem(null); reset(); }} className="border px-5 py-2 rounded-lg text-sm">
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
