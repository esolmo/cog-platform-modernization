'use strict'

import { useMutation } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { v4 as uuidv4 } from 'uuid'
import { bettingApi } from '@/api/bettingApi'
import { useWagerDraftStore } from '@/stores/wagerDraftStore'
import { useRef } from 'react'

export function WagerConfirmationPage(): React.ReactElement {
  const navigate = useNavigate()
  const { customerId, wagerType, riskAmount, items, clearDraft } = useWagerDraftStore()
  const idempotencyKey = useRef(uuidv4())

  const mutation = useMutation({
    mutationFn: () =>
      bettingApi.createWager({
        customerId: customerId!,
        wagerType,
        riskAmount: parseFloat(riskAmount),
        idempotencyKey: idempotencyKey.current,
        items: items.map((i) => ({
          gamePeriodId: i.gamePeriodId,
          itemType: i.itemType,
          side: i.side,
        })),
      }),
    onSuccess: (wager) => {
      clearDraft()
      navigate(`/wagers/pending`, { state: { newWagerId: wager.id } })
    },
  })

  if (!customerId || items.length === 0) {
    navigate('/sports')
    return <></>
  }

  return (
    <div className="p-6 max-w-lg mx-auto">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Confirm Wager</h1>

      <div className="rounded-lg border border-gray-200 bg-white p-4 mb-6 space-y-3">
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Customer ID</span>
          <span className="font-medium">{customerId}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Wager Type</span>
          <span className="font-medium">{wagerType}</span>
        </div>
        <div className="flex justify-between text-sm">
          <span className="text-gray-500">Risk Amount</span>
          <span className="font-medium">${parseFloat(riskAmount).toFixed(2)}</span>
        </div>
        <hr />
        {items.map((item) => (
          <div key={item.gamePeriodId} className="text-sm">
            <div className="font-medium">
              {item.side === 'Home' || item.side === 'Under' ? item.homeTeam : item.awayTeam}
              {' '}
              <span className="text-gray-500">({item.line > 0 ? '+' : ''}{item.line})</span>
            </div>
            <div className="text-xs text-gray-400">{item.awayTeam} @ {item.homeTeam} · {item.periodDescription}</div>
          </div>
        ))}
      </div>

      {mutation.isError && (
        <div className="mb-4 rounded-md bg-red-50 p-3 text-sm text-red-700">
          Failed to submit wager. Please try again.
        </div>
      )}

      <div className="flex gap-3">
        <button
          type="button"
          onClick={() => navigate(-1)}
          disabled={mutation.isPending}
          className="flex-1 rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50"
        >
          Back
        </button>
        <button
          type="button"
          onClick={() => mutation.mutate()}
          disabled={mutation.isPending}
          className="flex-1 rounded-md bg-green-600 px-4 py-2 text-sm font-semibold text-white hover:bg-green-700 disabled:opacity-50"
        >
          {mutation.isPending ? 'Submitting...' : 'Submit Wager'}
        </button>
      </div>
    </div>
  )
}
