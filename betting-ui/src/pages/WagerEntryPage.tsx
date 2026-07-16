'use strict'

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useNavigate } from 'react-router-dom'
import { useWagerDraftStore } from '@/stores/wagerDraftStore'

const schema = z.object({
  customerId: z.coerce.number().positive('Customer ID is required'),
  riskAmount: z.coerce
    .number()
    .positive('Amount must be positive')
    .max(100_000, 'Amount exceeds maximum'),
  wagerType: z.enum(['Straight', 'Parlay', 'Teaser', 'IfBet', 'Reverse', 'ActionReverse']),
})

type FormValues = z.infer<typeof schema>

export function WagerEntryPage(): React.ReactElement {
  const navigate = useNavigate()
  const { items, setCustomer, setRiskAmount, setWagerType, removeItem } = useWagerDraftStore()

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { wagerType: 'Straight' },
  })

  const onSubmit = (values: FormValues): void => {
    setCustomer(values.customerId)
    setRiskAmount(String(values.riskAmount))
    setWagerType(values.wagerType)
    navigate('/wager/confirm')
  }

  if (items.length === 0) {
    return (
      <div className="p-8 text-center text-gray-500">
        No games selected.{' '}
        <a href="/sports" className="text-blue-600 underline">
          Pick a game
        </a>
      </div>
    )
  }

  return (
    <div className="p-6 max-w-lg mx-auto">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Create Wager</h1>

      <div className="mb-6 space-y-2">
        <h2 className="text-sm font-semibold uppercase text-gray-500">Selected Plays</h2>
        {items.map((item) => (
          <div key={item.gamePeriodId} className="flex items-center justify-between rounded border border-gray-200 bg-white px-4 py-2 text-sm">
            <div>
              <span className="font-medium">{item.side === 'Home' || item.side === 'Under' ? item.homeTeam : item.awayTeam}</span>
              <span className="ml-2 text-gray-500">{item.itemType} ({item.line > 0 ? '+' : ''}{item.line})</span>
              <div className="text-xs text-gray-400">{item.awayTeam} @ {item.homeTeam} · {item.periodDescription}</div>
            </div>
            <button
              type="button"
              onClick={() => removeItem(item.gamePeriodId)}
              className="ml-4 text-red-500 hover:text-red-700 text-xs"
            >
              Remove
            </button>
          </div>
        ))}
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label htmlFor="customerId" className="block text-sm font-medium text-gray-700">Customer ID</label>
          <input
            id="customerId"
            type="number"
            {...register('customerId')}
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
          />
          {errors.customerId && <p className="mt-1 text-xs text-red-600">{errors.customerId.message}</p>}
        </div>

        <div>
          <label htmlFor="wagerType" className="block text-sm font-medium text-gray-700">Wager Type</label>
          <select
            id="wagerType"
            {...register('wagerType')}
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
          >
            <option value="Straight">Straight</option>
            <option value="Parlay">Parlay</option>
            <option value="Teaser">Teaser</option>
            <option value="IfBet">If Bet</option>
            <option value="Reverse">Reverse</option>
          </select>
        </div>

        <div>
          <label htmlFor="riskAmount" className="block text-sm font-medium text-gray-700">Risk Amount ($)</label>
          <input
            id="riskAmount"
            type="number"
            step="0.01"
            {...register('riskAmount')}
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
          />
          {errors.riskAmount && <p className="mt-1 text-xs text-red-600">{errors.riskAmount.message}</p>}
        </div>

        <button
          type="submit"
          className="w-full rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
        >
          Review Wager
        </button>
      </form>
    </div>
  )
}
