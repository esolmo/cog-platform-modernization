'use strict'

import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { bettingApi } from '@/api/bettingApi'
import type { SportTypeDetail, CreateSportTypeRequest, UpdateSportTypeRequest } from '@/types/betting'

const schema = z.object({
  name: z.string().min(1, 'Name required'),
  code: z.string().min(1, 'Code required').max(10, 'Max 10 chars').toUpperCase(),
  isActive: z.boolean(),
  displayOrder: z.coerce.number().int().min(0),
})
type FormValues = z.infer<typeof schema>

interface SportFormModalProps {
  sport?: SportTypeDetail | null
  onSave: (data: CreateSportTypeRequest | UpdateSportTypeRequest) => Promise<void>
  onClose: () => void
}

function SportFormModal({ sport, onSave, onClose }: SportFormModalProps): React.ReactElement {
  const isEdit = sport != null
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: sport?.name ?? '',
      code: sport?.code ?? '',
      isActive: sport?.isActive ?? true,
      displayOrder: sport?.displayOrder ?? 0,
    },
  })

  const onSubmit = async (values: FormValues): Promise<void> => {
    await onSave(values)
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-sm p-6">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold">{isEdit ? 'Edit Sport Type' : 'New Sport Type'}</h2>
          <button type="button" onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl leading-none">&times;</button>
        </div>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Name</label>
            <input {...register('name')} className="w-full border rounded-md px-3 py-2 text-sm" />
            {errors.name && <p className="text-red-600 text-xs mt-1">{errors.name.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Code</label>
            <input {...register('code')} className="w-full border rounded-md px-3 py-2 text-sm uppercase" placeholder="e.g. NFL" />
            {errors.code && <p className="text-red-600 text-xs mt-1">{errors.code.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Display Order</label>
            <input type="number" {...register('displayOrder')} className="w-full border rounded-md px-3 py-2 text-sm" />
          </div>
          <div className="flex items-center gap-2">
            <input type="checkbox" {...register('isActive')} id="isActive" className="rounded" />
            <label htmlFor="isActive" className="text-sm text-gray-700">Active</label>
          </div>
          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose} className="px-4 py-2 text-sm border rounded-md hover:bg-gray-50">Cancel</button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:opacity-50"
            >
              {isSubmitting ? 'Saving…' : isEdit ? 'Save Changes' : 'Create'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

export function SportsManagerPage(): React.ReactElement {
  const qc = useQueryClient()
  const [showModal, setShowModal] = useState(false)
  const [editSport, setEditSport] = useState<SportTypeDetail | null>(null)
  const [error, setError] = useState<string | null>(null)

  const sportsQuery = useQuery({
    queryKey: ['sports-all'],
    queryFn: () => bettingApi.getAllSportTypes(),
  })

  const createMutation = useMutation({
    mutationFn: (req: CreateSportTypeRequest) => bettingApi.createSportType(req),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['sports-all'] })
      setShowModal(false)
      setError(null)
    },
    onError: (err: Error) => setError(err.message),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, req }: { id: number; req: UpdateSportTypeRequest }) =>
      bettingApi.updateSportType(id, req),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['sports-all'] })
      setShowModal(false)
      setEditSport(null)
      setError(null)
    },
    onError: (err: Error) => setError(err.message),
  })

  const handleSave = async (data: CreateSportTypeRequest | UpdateSportTypeRequest): Promise<void> => {
    if (editSport) {
      await updateMutation.mutateAsync({ id: editSport.id, req: data as UpdateSportTypeRequest })
    } else {
      await createMutation.mutateAsync(data as CreateSportTypeRequest)
    }
  }

  const sports = sportsQuery.data ?? []

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Sport Types</h1>
        <button
          type="button"
          onClick={() => { setEditSport(null); setShowModal(true) }}
          className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-md hover:bg-indigo-700"
        >
          + New Sport Type
        </button>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-md text-red-700 text-sm">
          {error}
        </div>
      )}

      <div className="bg-white rounded-lg shadow overflow-hidden">
        {sportsQuery.isLoading ? (
          <div className="p-8 text-center text-gray-400">Loading…</div>
        ) : sports.length === 0 ? (
          <div className="p-8 text-center text-gray-400">No sport types configured.</div>
        ) : (
          <table className="min-w-full text-sm">
            <thead className="bg-gray-50 text-gray-600 text-xs uppercase">
              <tr>
                <th className="px-4 py-3 text-left">Code</th>
                <th className="px-4 py-3 text-left">Name</th>
                <th className="px-4 py-3 text-left">Order</th>
                <th className="px-4 py-3 text-left">Status</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {sports
                .sort((a, b) => a.displayOrder - b.displayOrder)
                .map(sport => (
                  <tr key={sport.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 font-mono font-medium text-gray-800">{sport.code}</td>
                    <td className="px-4 py-3 text-gray-700">{sport.name}</td>
                    <td className="px-4 py-3 text-gray-500">{sport.displayOrder}</td>
                    <td className="px-4 py-3">
                      {sport.isActive
                        ? <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">Active</span>
                        : <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-600">Inactive</span>
                      }
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button
                        type="button"
                        onClick={() => { setEditSport(sport); setShowModal(true) }}
                        className="text-xs px-2 py-1 rounded border hover:bg-gray-50"
                      >
                        Edit
                      </button>
                    </td>
                  </tr>
                ))}
            </tbody>
          </table>
        )}
      </div>

      {showModal && (
        <SportFormModal
          sport={editSport}
          onSave={handleSave}
          onClose={() => { setShowModal(false); setEditSport(null) }}
        />
      )}
    </div>
  )
}
