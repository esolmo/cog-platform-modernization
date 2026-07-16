'use strict'

import { useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { bettingApi } from '@/api/bettingApi'
import { GameStatusBadge } from '@/components/admin/GameStatusBadge'
import type { GamePeriod, SetSpreadRequest, SetMoneyLineRequest, SetTotalRequest } from '@/types/betting'

// ── Inline line editor sub-forms ─────────────────────────────────────────────

const spreadSchema = z.object({
  spread: z.coerce.number(),
  juice: z.coerce.number(),
})
const mlSchema = z.object({
  homeMoneyLine: z.coerce.number(),
  awayMoneyLine: z.coerce.number(),
})
const totalSchema = z.object({
  total: z.coerce.number(),
  overJuice: z.coerce.number(),
  underJuice: z.coerce.number(),
})

type SpreadForm = z.infer<typeof spreadSchema>
type MLForm = z.infer<typeof mlSchema>
type TotalForm = z.infer<typeof totalSchema>

function formatOdds(v: number | null | undefined): string {
  if (v == null) return '—'
  return v > 0 ? `+${v}` : `${v}`
}

interface SpreadEditorProps {
  periodId: number
  lines: GamePeriod['lines']
  onSaved: () => void
}

function SpreadEditor({ periodId, lines, onSaved }: SpreadEditorProps): React.ReactElement {
  const [open, setOpen] = useState(false)
  const { register, handleSubmit, formState: { isSubmitting } } = useForm<SpreadForm>({
    resolver: zodResolver(spreadSchema),
    defaultValues: { spread: lines?.spread ?? 0, juice: lines?.spreadJuice ?? -110 },
  })

  const onSubmit = async (data: SpreadForm): Promise<void> => {
    const req: SetSpreadRequest = { spread: data.spread, juice: data.juice }
    await bettingApi.setSpread(periodId, req)
    onSaved()
    setOpen(false)
  }

  if (!open) {
    return (
      <div className="flex items-center gap-2">
        <span className="text-sm text-gray-700">
          {lines?.spread != null ? `${lines.spread} (${formatOdds(lines.spreadJuice)})` : '—'}
        </span>
        <button type="button" onClick={() => setOpen(true)} className="text-xs text-indigo-600 hover:underline">Edit</button>
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex items-center gap-2">
      <input {...register('spread')} placeholder="Spread" className="w-20 border rounded px-2 py-1 text-sm" />
      <input {...register('juice')} placeholder="Juice" className="w-20 border rounded px-2 py-1 text-sm" />
      <button type="submit" disabled={isSubmitting} className="text-xs px-2 py-1 bg-indigo-600 text-white rounded disabled:opacity-50">Save</button>
      <button type="button" onClick={() => setOpen(false)} className="text-xs text-gray-500 hover:text-gray-700">Cancel</button>
    </form>
  )
}

interface MLEditorProps {
  periodId: number
  lines: GamePeriod['lines']
  homeTeam: string
  awayTeam: string
  onSaved: () => void
}

function MoneyLineEditor({ periodId, lines, homeTeam, awayTeam, onSaved }: MLEditorProps): React.ReactElement {
  const [open, setOpen] = useState(false)
  const { register, handleSubmit, formState: { isSubmitting } } = useForm<MLForm>({
    resolver: zodResolver(mlSchema),
    defaultValues: { homeMoneyLine: lines?.homeMoneyLine ?? 0, awayMoneyLine: lines?.awayMoneyLine ?? 0 },
  })

  const onSubmit = async (data: MLForm): Promise<void> => {
    const req: SetMoneyLineRequest = { homeMoneyLine: data.homeMoneyLine, awayMoneyLine: data.awayMoneyLine }
    await bettingApi.setMoneyLine(periodId, req)
    onSaved()
    setOpen(false)
  }

  if (!open) {
    return (
      <div className="flex items-center gap-2">
        <span className="text-sm text-gray-700">
          {lines?.homeMoneyLine != null
            ? `${homeTeam}: ${formatOdds(lines.homeMoneyLine)} / ${awayTeam}: ${formatOdds(lines.awayMoneyLine)}`
            : '—'}
        </span>
        <button type="button" onClick={() => setOpen(true)} className="text-xs text-indigo-600 hover:underline">Edit</button>
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex items-center gap-2 flex-wrap">
      <span className="text-xs text-gray-500">{homeTeam}:</span>
      <input {...register('homeMoneyLine')} className="w-20 border rounded px-2 py-1 text-sm" />
      <span className="text-xs text-gray-500">{awayTeam}:</span>
      <input {...register('awayMoneyLine')} className="w-20 border rounded px-2 py-1 text-sm" />
      <button type="submit" disabled={isSubmitting} className="text-xs px-2 py-1 bg-indigo-600 text-white rounded disabled:opacity-50">Save</button>
      <button type="button" onClick={() => setOpen(false)} className="text-xs text-gray-500 hover:text-gray-700">Cancel</button>
    </form>
  )
}

interface TotalEditorProps {
  periodId: number
  lines: GamePeriod['lines']
  onSaved: () => void
}

function TotalEditor({ periodId, lines, onSaved }: TotalEditorProps): React.ReactElement {
  const [open, setOpen] = useState(false)
  const { register, handleSubmit, formState: { isSubmitting } } = useForm<TotalForm>({
    resolver: zodResolver(totalSchema),
    defaultValues: {
      total: lines?.total ?? 0,
      overJuice: lines?.overJuice ?? -110,
      underJuice: lines?.underJuice ?? -110,
    },
  })

  const onSubmit = async (data: TotalForm): Promise<void> => {
    const req: SetTotalRequest = { total: data.total, overJuice: data.overJuice, underJuice: data.underJuice }
    await bettingApi.setTotal(periodId, req)
    onSaved()
    setOpen(false)
  }

  if (!open) {
    return (
      <div className="flex items-center gap-2">
        <span className="text-sm text-gray-700">
          {lines?.total != null
            ? `O/U ${lines.total} (O: ${formatOdds(lines.overJuice)} / U: ${formatOdds(lines.underJuice)})`
            : '—'}
        </span>
        <button type="button" onClick={() => setOpen(true)} className="text-xs text-indigo-600 hover:underline">Edit</button>
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex items-center gap-2 flex-wrap">
      <span className="text-xs text-gray-500">Total:</span>
      <input {...register('total')} className="w-20 border rounded px-2 py-1 text-sm" />
      <span className="text-xs text-gray-500">O-Juice:</span>
      <input {...register('overJuice')} className="w-20 border rounded px-2 py-1 text-sm" />
      <span className="text-xs text-gray-500">U-Juice:</span>
      <input {...register('underJuice')} className="w-20 border rounded px-2 py-1 text-sm" />
      <button type="submit" disabled={isSubmitting} className="text-xs px-2 py-1 bg-indigo-600 text-white rounded disabled:opacity-50">Save</button>
      <button type="button" onClick={() => setOpen(false)} className="text-xs text-gray-500 hover:text-gray-700">Cancel</button>
    </form>
  )
}

// ── Add period modal ──────────────────────────────────────────────────────────

const addPeriodSchema = z.object({
  periodDescription: z.string().min(1, 'Description required'),
  periodNumber: z.coerce.number().int().min(0),
})
type AddPeriodForm = z.infer<typeof addPeriodSchema>

interface AddPeriodModalProps {
  gameId: number
  onSaved: () => void
  onClose: () => void
}

function AddPeriodModal({ gameId, onSaved, onClose }: AddPeriodModalProps): React.ReactElement {
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<AddPeriodForm>({
    resolver: zodResolver(addPeriodSchema),
    defaultValues: { periodDescription: '', periodNumber: 0 },
  })

  const onSubmit = async (data: AddPeriodForm): Promise<void> => {
    await bettingApi.addPeriod(gameId, data)
    onSaved()
    onClose()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-sm p-6">
        <h2 className="text-lg font-semibold mb-4">Add Period</h2>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
            <input {...register('periodDescription')} className="w-full border rounded-md px-3 py-2 text-sm" placeholder="e.g. 1st Half" />
            {errors.periodDescription && <p className="text-red-600 text-xs mt-1">{errors.periodDescription.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Period Number</label>
            <input type="number" {...register('periodNumber')} className="w-full border rounded-md px-3 py-2 text-sm" />
          </div>
          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose} className="px-4 py-2 text-sm border rounded-md hover:bg-gray-50">Cancel</button>
            <button type="submit" disabled={isSubmitting} className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-md hover:bg-indigo-700 disabled:opacity-50">
              {isSubmitting ? 'Adding…' : 'Add Period'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Main page ─────────────────────────────────────────────────────────────────

export function GameDetailPage(): React.ReactElement {
  const { gameId } = useParams<{ gameId: string }>()
  const navigate = useNavigate()
  const qc = useQueryClient()
  const [showAddPeriod, setShowAddPeriod] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const id = Number(gameId)
  const queryKey = ['game-detail', id]

  const gameQuery = useQuery({
    queryKey,
    queryFn: () => bettingApi.getGame(id),
    enabled: !isNaN(id),
  })

  const removePeriodMutation = useMutation({
    mutationFn: (periodId: number) => bettingApi.removePeriod(id, periodId),
    onSuccess: () => void qc.invalidateQueries({ queryKey }),
    onError: (err: Error) => setError(err.message),
  })

  const refresh = (): void => { void qc.invalidateQueries({ queryKey }) }

  const game = gameQuery.data

  if (gameQuery.isLoading) {
    return <div className="p-8 text-center text-gray-400">Loading…</div>
  }

  if (!game) {
    return <div className="p-8 text-center text-gray-500">Game not found.</div>
  }

  const canEditLines = game.status === 'Upcoming' || game.status === 'InProgress'

  return (
    <div>
      {/* Header */}
      <div className="mb-6">
        <button type="button" onClick={() => navigate('/admin/games')} className="text-sm text-indigo-600 hover:underline mb-2 block">
          ← Back to Games
        </button>
        <div className="flex items-center gap-4">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">
              {game.awayTeam} @ {game.homeTeam}
            </h1>
            <p className="text-sm text-gray-500 mt-1">
              {game.sportName} · {new Date(game.gameDate).toLocaleString()} {game.rotationNumber && `· Rot #${game.rotationNumber}`}
            </p>
          </div>
          <GameStatusBadge status={game.status} />
        </div>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-md text-red-700 text-sm">
          {error}
        </div>
      )}

      {/* Periods & Lines */}
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-gray-800">Periods &amp; Lines</h2>
          {canEditLines && (
            <button
              type="button"
              onClick={() => setShowAddPeriod(true)}
              className="text-sm px-3 py-1.5 border rounded-md hover:bg-gray-50"
            >
              + Add Period
            </button>
          )}
        </div>

        {game.periods.length === 0 ? (
          <p className="text-gray-400 text-sm">No periods configured.</p>
        ) : (
          game.periods.map(period => (
            <div key={period.id} className="bg-white rounded-lg shadow-sm border p-5">
              <div className="flex items-center justify-between mb-4">
                <h3 className="font-medium text-gray-800">{period.periodDescription}</h3>
                {canEditLines && (
                  <button
                    type="button"
                    onClick={() => {
                      if (confirm(`Remove period "${period.periodDescription}"?`))
                        removePeriodMutation.mutate(period.id)
                    }}
                    className="text-xs text-red-500 hover:text-red-700"
                  >
                    Remove
                  </button>
                )}
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div>
                  <p className="text-xs font-medium text-gray-500 uppercase mb-1">Spread</p>
                  {canEditLines
                    ? <SpreadEditor periodId={period.id} lines={period.lines} onSaved={refresh} />
                    : <span className="text-sm">{period.lines?.spread != null ? `${period.lines.spread} (${formatOdds(period.lines.spreadJuice)})` : '—'}</span>
                  }
                </div>
                <div>
                  <p className="text-xs font-medium text-gray-500 uppercase mb-1">Money Line</p>
                  {canEditLines
                    ? <MoneyLineEditor periodId={period.id} lines={period.lines} homeTeam={game.homeTeam} awayTeam={game.awayTeam} onSaved={refresh} />
                    : <span className="text-sm">{period.lines?.homeMoneyLine != null ? `H: ${formatOdds(period.lines.homeMoneyLine)} / A: ${formatOdds(period.lines.awayMoneyLine)}` : '—'}</span>
                  }
                </div>
                <div>
                  <p className="text-xs font-medium text-gray-500 uppercase mb-1">Total (O/U)</p>
                  {canEditLines
                    ? <TotalEditor periodId={period.id} lines={period.lines} onSaved={refresh} />
                    : <span className="text-sm">{period.lines?.total != null ? `${period.lines.total} (O: ${formatOdds(period.lines.overJuice)} / U: ${formatOdds(period.lines.underJuice)})` : '—'}</span>
                  }
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {showAddPeriod && (
        <AddPeriodModal
          gameId={id}
          onSaved={refresh}
          onClose={() => setShowAddPeriod(false)}
        />
      )}
    </div>
  )
}
