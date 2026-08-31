'use strict'

import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { casinoApi } from '@/api/casinoApi'
import type { TransferResult } from '@/types/casino'
import axios from 'axios'

// ─── Balance card ────────────────────────────────────────────────────────────

function BalanceCard({
  label,
  value,
  highlight,
}: {
  label: string
  value: number
  highlight?: boolean
}): React.ReactElement {
  return (
    <div className={`rounded-lg p-4 ${highlight ? 'bg-yellow-500/10 border border-yellow-500/40' : 'bg-gray-800 border border-gray-700'}`}>
      <p className="text-xs text-gray-400 uppercase tracking-wider mb-1">{label}</p>
      <p className={`text-2xl font-bold ${highlight ? 'text-yellow-400' : 'text-white'}`}>
        ${value.toFixed(2)}
      </p>
    </div>
  )
}

// ─── Transfer modal ───────────────────────────────────────────────────────────

interface TransferModalProps {
  type: 'deposit' | 'withdraw'
  onClose: () => void
  onSuccess: (result: TransferResult) => void
}

function TransferModal({ type, onClose, onSuccess }: TransferModalProps): React.ReactElement {
  const [amount, setAmount] = useState('')
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: (amt: number) =>
      type === 'deposit' ? casinoApi.deposit(amt) : casinoApi.withdraw(amt),
    onSuccess: (result) => {
      if (result.success) {
        onSuccess(result)
      } else {
        setError(result.errorMessage ?? 'Transfer failed.')
      }
    },
    onError: (err: unknown) => {
      if (axios.isAxiosError(err)) {
        setError(err.response?.data?.error ?? 'Transfer failed.')
      } else {
        setError('An unexpected error occurred.')
      }
    },
  })

  const handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault()
    setError(null)
    const parsed = parseFloat(amount)
    if (isNaN(parsed) || parsed <= 0) {
      setError('Enter a valid amount greater than zero.')
      return
    }
    mutation.mutate(parsed)
  }

  const title = type === 'deposit' ? 'Deposit to Casino' : 'Withdraw from Casino'
  const actionLabel = type === 'deposit' ? 'Deposit' : 'Withdraw'

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 px-4">
      <div className="w-full max-w-sm bg-gray-800 border border-gray-700 rounded-lg shadow-2xl p-6">
        <div className="flex items-center justify-between mb-5">
          <h2 className="text-lg font-semibold text-white">{title}</h2>
          <button
            type="button"
            onClick={onClose}
            className="text-gray-400 hover:text-white text-xl leading-none"
            aria-label="Close"
          >
            ×
          </button>
        </div>

        {error && (
          <div className="mb-4 rounded-md bg-red-900/50 border border-red-700 px-4 py-3 text-sm text-red-300">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label htmlFor="amount" className="block text-sm font-medium text-gray-300 mb-1">
              Amount (USD)
            </label>
            <div className="relative">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400">$</span>
              <input
                id="amount"
                type="number"
                min="0.01"
                step="0.01"
                required
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className="w-full rounded-md bg-gray-700 border border-gray-600 text-white pl-7 pr-3 py-2 text-sm focus:outline-none focus:border-yellow-500 focus:ring-1 focus:ring-yellow-500"
                placeholder="0.00"
              />
            </div>
          </div>

          <div className="flex gap-3 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 rounded-md bg-gray-700 hover:bg-gray-600 text-white py-2 text-sm transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={mutation.isPending}
              className="flex-1 rounded-md bg-yellow-500 hover:bg-yellow-400 disabled:bg-yellow-700 disabled:cursor-not-allowed text-gray-900 font-semibold py-2 text-sm transition-colors"
            >
              {mutation.isPending ? 'Processing...' : actionLabel}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ─── Lobby page ───────────────────────────────────────────────────────────────

export function CasinoLobbyPage(): React.ReactElement {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [modal, setModal] = useState<'deposit' | 'withdraw' | null>(null)
  const [flashMessage, setFlashMessage] = useState<string | null>(null)

  const {
    data: session,
    isLoading: sessionLoading,
    error: sessionError,
    refetch: refetchSession,
  } = useQuery({
    queryKey: ['casino-session'],
    queryFn: casinoApi.getSession,
    retry: (count, err) => {
      if (axios.isAxiosError(err) && err.response?.status === 404) return false
      return count < 2
    },
  })

  const {
    data: balance,
    isLoading: balanceLoading,
    refetch: refetchBalance,
  } = useQuery({
    queryKey: ['casino-balance'],
    queryFn: casinoApi.getBalance,
    enabled: !!session,
    refetchInterval: 60_000,
  })

  const showFlash = (msg: string): void => {
    setFlashMessage(msg)
    setTimeout(() => setFlashMessage(null), 4000)
  }

  const handleTransferSuccess = (result: TransferResult): void => {
    setModal(null)
    queryClient.invalidateQueries({ queryKey: ['casino-balance'] })
    queryClient.invalidateQueries({ queryKey: ['casino-session'] })
    showFlash(
      `Transfer complete. Casino balance: $${result.casinoBalance.toFixed(2)} | Available: $${result.availableBalance.toFixed(2)}`
    )
    void refetchBalance()
    void refetchSession()
  }

  // Not registered yet — redirect to setup
  if (
    !sessionLoading &&
    axios.isAxiosError(sessionError) &&
    sessionError?.response?.status === 404
  ) {
    navigate('/setup', { replace: true })
    return <></>
  }

  const loading = sessionLoading || balanceLoading

  return (
    <div className="space-y-6">
      {/* Flash banner */}
      {flashMessage && (
        <div className="rounded-md bg-green-900/50 border border-green-600 px-4 py-3 text-sm text-green-300">
          {flashMessage}
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-yellow-400">
            {session ? `Welcome, ${session.nickname}` : 'Live Dealer Casino'}
          </h1>
          {session && (
            <p className="text-sm text-gray-400 mt-0.5">
              Playthrough: {session.playthrough > 0 ? `$${session.playthrough.toFixed(2)} remaining` : 'No requirement'}
            </p>
          )}
        </div>
        <button
          type="button"
          onClick={() => void refetchBalance()}
          className="text-xs text-gray-400 hover:text-white transition-colors"
          aria-label="Refresh balance"
        >
          ↻ Refresh
        </button>
      </div>

      {/* Loading skeleton */}
      {loading && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-20 rounded-lg bg-gray-800 border border-gray-700 animate-pulse" />
          ))}
        </div>
      )}

      {/* Balance cards */}
      {!loading && balance && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <BalanceCard label="Available Balance" value={balance.availableBalance} />
          <BalanceCard label="Casino Balance" value={balance.casinoBalance} highlight />
          <BalanceCard label="Bonus Balance" value={balance.bonusBalance} />
        </div>
      )}

      {/* Actions */}
      {!loading && session && (
        <div className="bg-gray-800 border border-gray-700 rounded-lg p-5 space-y-4">
          <h2 className="text-sm font-semibold text-gray-300 uppercase tracking-wider">Actions</h2>

          <div className="flex flex-wrap gap-3">
            <button
              type="button"
              onClick={() => setModal('deposit')}
              className="rounded-md bg-yellow-500 hover:bg-yellow-400 text-gray-900 font-semibold px-4 py-2 text-sm transition-colors"
            >
              Deposit to Casino
            </button>
            <button
              type="button"
              onClick={() => setModal('withdraw')}
              className="rounded-md bg-gray-700 hover:bg-gray-600 text-white px-4 py-2 text-sm transition-colors"
            >
              Withdraw from Casino
            </button>
          </div>

          <div className="border-t border-gray-700 pt-4">
            <a
              href={session.lobbyUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 rounded-md bg-green-600 hover:bg-green-500 text-white font-semibold px-5 py-2.5 text-sm transition-colors"
            >
              <span>Play Now</span>
              <span className="text-xs opacity-75">↗</span>
            </a>
            <p className="mt-2 text-xs text-gray-500">
              Opens the live dealer lobby in a new tab. Make sure funds are in your casino balance before playing.
            </p>
          </div>
        </div>
      )}

      {/* Error state */}
      {!loading && sessionError && !axios.isAxiosError(sessionError) && (
        <div className="rounded-md bg-red-900/50 border border-red-700 px-4 py-3 text-sm text-red-300">
          Failed to load session. Please refresh the page.
        </div>
      )}

      {/* Transfer modals */}
      {modal && (
        <TransferModal
          type={modal}
          onClose={() => setModal(null)}
          onSuccess={handleTransferSuccess}
        />
      )}
    </div>
  )
}
