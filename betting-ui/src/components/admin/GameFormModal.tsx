'use strict'

import { useEffect } from 'react'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import type { Game, SportTypeDetail, CreateGameRequest, UpdateGameRequest } from '@/types/betting'

const periodSchema = z.object({
  periodDescription: z.string().min(1, 'Description required'),
  periodNumber: z.coerce.number().int().min(0),
})

const schema = z.object({
  sportTypeId: z.coerce.number().int().positive('Sport required'),
  homeTeam: z.string().min(1, 'Home team required'),
  awayTeam: z.string().min(1, 'Away team required'),
  gameDate: z.string().min(1, 'Date required'),
  rotationNumber: z.string().optional(),
  periods: z.array(periodSchema),
})

type FormValues = z.infer<typeof schema>

interface Props {
  game?: Game | null
  sports: SportTypeDetail[]
  onSave: (data: CreateGameRequest | UpdateGameRequest) => Promise<void>
  onClose: () => void
}

export function GameFormModal({ game, sports, onSave, onClose }: Props): React.ReactElement {
  const isEdit = game != null

  const { register, control, handleSubmit, reset, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: {
        sportTypeId: game?.sportTypeId ?? 0,
        homeTeam: game?.homeTeam ?? '',
        awayTeam: game?.awayTeam ?? '',
        gameDate: game ? game.gameDate.slice(0, 16) : '',
        rotationNumber: game?.rotationNumber ?? '',
        periods: game?.periods.map(p => ({
          periodDescription: p.periodDescription,
          periodNumber: p.periodNumber,
        })) ?? [{ periodDescription: 'Full Game', periodNumber: 0 }],
      },
    })

  const { fields, append, remove } = useFieldArray({ control, name: 'periods' })

  useEffect(() => {
    reset({
      sportTypeId: game?.sportTypeId ?? 0,
      homeTeam: game?.homeTeam ?? '',
      awayTeam: game?.awayTeam ?? '',
      gameDate: game ? game.gameDate.slice(0, 16) : '',
      rotationNumber: game?.rotationNumber ?? '',
      periods: game?.periods.map(p => ({
        periodDescription: p.periodDescription,
        periodNumber: p.periodNumber,
      })) ?? [{ periodDescription: 'Full Game', periodNumber: 0 }],
    })
  }, [game, reset])

  const onSubmit = async (values: FormValues): Promise<void> => {
    if (isEdit) {
      const req: UpdateGameRequest = {
        homeTeam: values.homeTeam,
        awayTeam: values.awayTeam,
        gameDate: new Date(values.gameDate).toISOString(),
        rotationNumber: values.rotationNumber || undefined,
      }
      await onSave(req)
    } else {
      const req: CreateGameRequest = {
        sportTypeId: values.sportTypeId,
        homeTeam: values.homeTeam,
        awayTeam: values.awayTeam,
        gameDate: new Date(values.gameDate).toISOString(),
        rotationNumber: values.rotationNumber || undefined,
        periods: values.periods,
      }
      await onSave(req)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-lg max-h-[90vh] flex flex-col">
        <div className="px-6 py-4 border-b flex items-center justify-between">
          <h2 className="text-lg font-semibold">{isEdit ? 'Edit Game' : 'New Game'}</h2>
          <button type="button" onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl leading-none">&times;</button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="flex-1 overflow-auto px-6 py-4 space-y-4">
          {!isEdit && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Sport</label>
              <select {...register('sportTypeId')} className="w-full border rounded-md px-3 py-2 text-sm">
                <option value={0}>Select sport…</option>
                {sports.filter(s => s.isActive).map(s => (
                  <option key={s.id} value={s.id}>{s.name}</option>
                ))}
              </select>
              {errors.sportTypeId && <p className="text-red-600 text-xs mt-1">{errors.sportTypeId.message}</p>}
            </div>
          )}

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Home Team</label>
              <input {...register('homeTeam')} className="w-full border rounded-md px-3 py-2 text-sm" />
              {errors.homeTeam && <p className="text-red-600 text-xs mt-1">{errors.homeTeam.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Away Team</label>
              <input {...register('awayTeam')} className="w-full border rounded-md px-3 py-2 text-sm" />
              {errors.awayTeam && <p className="text-red-600 text-xs mt-1">{errors.awayTeam.message}</p>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Game Date / Time</label>
              <input type="datetime-local" {...register('gameDate')} className="w-full border rounded-md px-3 py-2 text-sm" />
              {errors.gameDate && <p className="text-red-600 text-xs mt-1">{errors.gameDate.message}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Rotation #</label>
              <input {...register('rotationNumber')} className="w-full border rounded-md px-3 py-2 text-sm" placeholder="Optional" />
            </div>
          </div>

          {!isEdit && (
            <div>
              <div className="flex items-center justify-between mb-2">
                <label className="block text-sm font-medium text-gray-700">Periods</label>
                <button
                  type="button"
                  onClick={() => append({ periodDescription: '', periodNumber: fields.length })}
                  className="text-xs text-indigo-600 hover:text-indigo-800"
                >
                  + Add period
                </button>
              </div>
              {fields.map((field, idx) => (
                <div key={field.id} className="flex gap-2 items-start mb-2">
                  <input
                    {...register(`periods.${idx}.periodDescription`)}
                    placeholder="Description (e.g. 1st Half)"
                    className="flex-1 border rounded-md px-3 py-1.5 text-sm"
                  />
                  <input
                    type="number"
                    {...register(`periods.${idx}.periodNumber`)}
                    placeholder="#"
                    className="w-16 border rounded-md px-2 py-1.5 text-sm"
                  />
                  {fields.length > 1 && (
                    <button type="button" onClick={() => remove(idx)} className="text-red-400 hover:text-red-600 text-lg leading-none mt-1">&times;</button>
                  )}
                </div>
              ))}
            </div>
          )}

          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose} className="px-4 py-2 text-sm border rounded-md hover:bg-gray-50">Cancel</button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:opacity-50"
            >
              {isSubmitting ? 'Saving…' : isEdit ? 'Save Changes' : 'Create Game'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
