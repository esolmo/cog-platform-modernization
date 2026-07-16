'use strict'

import { useQuery } from '@tanstack/react-query'
import { useNavigate, useParams } from 'react-router-dom'
import { format } from 'date-fns'
import { bettingApi } from '@/api/bettingApi'
import { useWagerDraftStore } from '@/stores/wagerDraftStore'
import type { Game, GamePeriod, WagerItemType, WagerSide } from '@/types/betting'

export function GameSelectionPage(): React.ReactElement {
  const { sportId } = useParams<{ sportId: string }>()
  const navigate = useNavigate()
  const addItem = useWagerDraftStore((s) => s.addItem)

  const { data, isLoading, isError } = useQuery({
    queryKey: ['games', sportId],
    queryFn: () => bettingApi.getGames({ sportTypeId: Number(sportId), pageSize: 100 }),
    enabled: !!sportId,
  })

  const handleSelectLine = (
    game: Game,
    period: GamePeriod,
    itemType: WagerItemType,
    side: WagerSide,
    line: number
  ): void => {
    addItem({
      gamePeriodId: period.id,
      homeTeam: game.homeTeam,
      awayTeam: game.awayTeam,
      periodDescription: period.periodDescription,
      itemType,
      side,
      line,
    })
    navigate('/wager/new')
  }

  if (isLoading) return <div className="p-8 text-center">Loading games...</div>
  if (isError) return <div className="p-8 text-center text-red-600">Failed to load games.</div>

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Select Game</h1>
      <div className="space-y-3">
        {data?.items.map((game) => (
          <div key={game.id} className="rounded-lg border border-gray-200 bg-white shadow-sm overflow-hidden">
            <div className="bg-gray-50 px-4 py-2 text-sm text-gray-500 flex items-center justify-between">
              <span>{format(new Date(game.gameDate), 'EEE MMM d, h:mm a')}</span>
              {game.rotationNumber && <span className="font-mono">#{game.rotationNumber}</span>}
            </div>
            {game.periods.every(p => !p.lines) ? (
              <div className="px-4 py-3">
                <div className="flex items-center justify-between mb-1">
                  <div className="font-medium text-gray-800 text-sm">
                    <div>{game.awayTeam}</div>
                    <div>{game.homeTeam}</div>
                  </div>
                  <span className="text-xs text-gray-400 italic">Lines not yet available</span>
                </div>
              </div>
            ) : (
              game.periods.map((period) => {
                const lines = period.lines
                if (!lines) return null
                return (
                  <div key={period.id} className="px-4 py-3 border-t border-gray-100 first:border-0">
                    <div className="text-xs text-gray-400 uppercase mb-2">{period.periodDescription}</div>
                    <div className="grid grid-cols-3 gap-2 text-sm">
                      <div className="font-medium text-gray-700 col-span-3 sm:col-span-1">
                        <div>{game.awayTeam}</div>
                        <div>{game.homeTeam}</div>
                      </div>
                      {lines.offeringSpread && (
                        <div className="space-y-1">
                          <button
                            onClick={() => handleSelectLine(game, period, 'Spread', 'Away', lines.spread! * -1)}
                            className="w-full rounded bg-gray-100 px-2 py-1 text-xs hover:bg-blue-100 hover:text-blue-800"
                          >
                            {lines.spread! > 0 ? '+' : ''}{(lines.spread! * -1).toFixed(1)} ({lines.spreadJuice})
                          </button>
                          <button
                            onClick={() => handleSelectLine(game, period, 'Spread', 'Home', lines.spread!)}
                            className="w-full rounded bg-gray-100 px-2 py-1 text-xs hover:bg-blue-100 hover:text-blue-800"
                          >
                            {lines.spread! > 0 ? '+' : ''}{lines.spread!.toFixed(1)} ({lines.spreadJuice})
                          </button>
                        </div>
                      )}
                      {lines.offeringMoneyLine && (
                        <div className="space-y-1">
                          <button
                            onClick={() => handleSelectLine(game, period, 'MoneyLine', 'Away', lines.awayMoneyLine!)}
                            className="w-full rounded bg-gray-100 px-2 py-1 text-xs hover:bg-blue-100 hover:text-blue-800"
                          >
                            {lines.awayMoneyLine! > 0 ? '+' : ''}{lines.awayMoneyLine}
                          </button>
                          <button
                            onClick={() => handleSelectLine(game, period, 'MoneyLine', 'Home', lines.homeMoneyLine!)}
                            className="w-full rounded bg-gray-100 px-2 py-1 text-xs hover:bg-blue-100 hover:text-blue-800"
                          >
                            {lines.homeMoneyLine! > 0 ? '+' : ''}{lines.homeMoneyLine}
                          </button>
                        </div>
                      )}
                      {lines.offeringTotal && (
                        <div className="space-y-1">
                          <button
                            onClick={() => handleSelectLine(game, period, 'Total', 'Over', lines.total!)}
                            className="w-full rounded bg-gray-100 px-2 py-1 text-xs hover:bg-blue-100 hover:text-blue-800"
                          >
                            O {lines.total} ({lines.overJuice})
                          </button>
                          <button
                            onClick={() => handleSelectLine(game, period, 'Total', 'Under', lines.total!)}
                            className="w-full rounded bg-gray-100 px-2 py-1 text-xs hover:bg-blue-100 hover:text-blue-800"
                          >
                            U {lines.total} ({lines.underJuice})
                          </button>
                        </div>
                      )}
                    </div>
                  </div>
                )
              })
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
