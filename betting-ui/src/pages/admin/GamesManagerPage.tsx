'use strict'

import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { bettingApi } from '@/api/bettingApi'
import { GameStatusBadge } from '@/components/admin/GameStatusBadge'
import { GameFormModal } from '@/components/admin/GameFormModal'
import type { Game, GameStatus, CreateGameRequest, UpdateGameRequest } from '@/types/betting'

const STATUS_OPTIONS: { value: string; label: string }[] = [
  { value: '',           label: 'All statuses' },
  { value: 'Upcoming',   label: 'Upcoming' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'Final',      label: 'Final' },
  { value: 'Postponed',  label: 'Postponed' },
  { value: 'Cancelled',  label: 'Cancelled' },
]

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    month: 'short', day: 'numeric', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

export function GamesManagerPage(): React.ReactElement {
  const navigate = useNavigate()
  const qc = useQueryClient()

  const [sportFilter, setSportFilter] = useState<number | undefined>()
  const [statusFilter, setStatusFilter] = useState<GameStatus | ''>('')
  const [page, setPage] = useState(1)
  const [showModal, setShowModal] = useState(false)
  const [editGame, setEditGame] = useState<Game | null>(null)
  const [error, setError] = useState<string | null>(null)

  const sportsQuery = useQuery({
    queryKey: ['sports-all'],
    queryFn: () => bettingApi.getAllSportTypes(),
  })

  const gamesQuery = useQuery({
    queryKey: ['games-admin', sportFilter, statusFilter, page],
    queryFn: () => bettingApi.getGames({ sportTypeId: sportFilter, page, pageSize: 25 }),
  })

  const createMutation = useMutation({
    mutationFn: (req: CreateGameRequest) => bettingApi.createGame(req),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['games-admin'] })
      setShowModal(false)
      setError(null)
    },
    onError: (err: Error) => setError(err.message),
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, req }: { id: number; req: UpdateGameRequest }) =>
      bettingApi.updateGame(id, req),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ['games-admin'] })
      setShowModal(false)
      setEditGame(null)
      setError(null)
    },
    onError: (err: Error) => setError(err.message),
  })

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: number; status: GameStatus }) =>
      bettingApi.updateGameStatus(id, { status }),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['games-admin'] }),
    onError: (err: Error) => setError(err.message),
  })

  const deleteMutation = useMutation({
    mutationFn: (id: number) => bettingApi.deleteGame(id),
    onSuccess: () => void qc.invalidateQueries({ queryKey: ['games-admin'] }),
    onError: (err: Error) => setError(err.message),
  })

  const handleSave = async (data: CreateGameRequest | UpdateGameRequest): Promise<void> => {
    if (editGame) {
      await updateMutation.mutateAsync({ id: editGame.id, req: data as UpdateGameRequest })
    } else {
      await createMutation.mutateAsync(data as CreateGameRequest)
    }
  }

  const games = gamesQuery.data?.items ?? []
  const sports = sportsQuery.data ?? []

  const filteredGames = statusFilter
    ? games.filter(g => g.status === statusFilter)
    : games

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Games Manager</h1>
        <button
          type="button"
          onClick={() => { setEditGame(null); setShowModal(true) }}
          className="px-4 py-2 bg-indigo-600 text-white text-sm font-medium rounded-md hover:bg-indigo-700"
        >
          + New Game
        </button>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-md text-red-700 text-sm">
          {error}
        </div>
      )}

      {/* Filters */}
      <div className="flex gap-4 mb-4">
        <select
          value={sportFilter ?? ''}
          onChange={e => { setSportFilter(e.target.value ? Number(e.target.value) : undefined); setPage(1) }}
          className="border rounded-md px-3 py-2 text-sm"
        >
          <option value="">All sports</option>
          {sports.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
        <select
          value={statusFilter}
          onChange={e => { setStatusFilter(e.target.value as GameStatus | ''); setPage(1) }}
          className="border rounded-md px-3 py-2 text-sm"
        >
          {STATUS_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
      </div>

      {/* Table */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        {gamesQuery.isLoading ? (
          <div className="p-8 text-center text-gray-400">Loading…</div>
        ) : filteredGames.length === 0 ? (
          <div className="p-8 text-center text-gray-400">No games found.</div>
        ) : (
          <table className="min-w-full text-sm">
            <thead className="bg-gray-50 text-gray-600 text-xs uppercase">
              <tr>
                <th className="px-4 py-3 text-left">Rot #</th>
                <th className="px-4 py-3 text-left">Matchup</th>
                <th className="px-4 py-3 text-left">Sport</th>
                <th className="px-4 py-3 text-left">Date</th>
                <th className="px-4 py-3 text-left">Status</th>
                <th className="px-4 py-3 text-left">Periods</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {filteredGames.map(game => (
                <tr key={game.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-500">{game.rotationNumber ?? '—'}</td>
                  <td className="px-4 py-3 font-medium">
                    <button
                      type="button"
                      onClick={() => navigate(`/admin/games/${game.id}`)}
                      className="text-indigo-600 hover:underline text-left"
                    >
                      {game.awayTeam} @ {game.homeTeam}
                    </button>
                  </td>
                  <td className="px-4 py-3 text-gray-600">{game.sportName}</td>
                  <td className="px-4 py-3 text-gray-600">{formatDate(game.gameDate)}</td>
                  <td className="px-4 py-3">
                    <GameStatusBadge status={game.status} />
                  </td>
                  <td className="px-4 py-3 text-gray-500">{game.periods.length}</td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex items-center justify-end gap-2">
                      {game.status === 'Upcoming' && (
                        <button
                          type="button"
                          onClick={() => statusMutation.mutate({ id: game.id, status: 'InProgress' })}
                          className="text-xs px-2 py-1 rounded bg-yellow-100 text-yellow-800 hover:bg-yellow-200"
                        >
                          Start
                        </button>
                      )}
                      {game.status === 'InProgress' && (
                        <button
                          type="button"
                          onClick={() => statusMutation.mutate({ id: game.id, status: 'Final' })}
                          className="text-xs px-2 py-1 rounded bg-gray-100 text-gray-700 hover:bg-gray-200"
                        >
                          Final
                        </button>
                      )}
                      {(game.status === 'Upcoming' || game.status === 'InProgress') && (
                        <button
                          type="button"
                          onClick={() => navigate(`/admin/games/${game.id}`)}
                          className="text-xs px-2 py-1 rounded bg-indigo-50 text-indigo-700 hover:bg-indigo-100"
                        >
                          Lines
                        </button>
                      )}
                      {game.status === 'Upcoming' && (
                        <button
                          type="button"
                          onClick={() => { setEditGame(game); setShowModal(true) }}
                          className="text-xs px-2 py-1 rounded border hover:bg-gray-50"
                        >
                          Edit
                        </button>
                      )}
                      {game.status === 'Upcoming' && (
                        <button
                          type="button"
                          onClick={() => {
                            if (confirm(`Delete ${game.awayTeam} @ ${game.homeTeam}?`))
                              deleteMutation.mutate(game.id)
                          }}
                          className="text-xs px-2 py-1 rounded text-red-600 hover:bg-red-50"
                        >
                          Delete
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Pagination */}
      {gamesQuery.data && gamesQuery.data.totalPages > 1 && (
        <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
          <span>Page {page} of {gamesQuery.data.totalPages} ({gamesQuery.data.totalCount} games)</span>
          <div className="flex gap-2">
            <button
              type="button"
              onClick={() => setPage(p => p - 1)}
              disabled={!gamesQuery.data.hasPreviousPage}
              className="px-3 py-1.5 border rounded-md disabled:opacity-40 hover:bg-gray-50"
            >
              Previous
            </button>
            <button
              type="button"
              onClick={() => setPage(p => p + 1)}
              disabled={!gamesQuery.data.hasNextPage}
              className="px-3 py-1.5 border rounded-md disabled:opacity-40 hover:bg-gray-50"
            >
              Next
            </button>
          </div>
        </div>
      )}

      {showModal && (
        <GameFormModal
          game={editGame}
          sports={sports}
          onSave={handleSave}
          onClose={() => { setShowModal(false); setEditGame(null) }}
        />
      )}
    </div>
  )
}
