'use strict'

import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { casinoApi } from '@/api/casinoApi'

export function SetupPage(): React.ReactElement {
  const navigate = useNavigate()
  const [nickname, setNickname] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent): Promise<void> => {
    e.preventDefault()
    setError(null)
    setLoading(true)

    try {
      await casinoApi.register(nickname.trim())
      navigate('/casino', { replace: true })
    } catch (err: unknown) {
      const message =
        err instanceof Error ? err.message : 'Registration failed. Please try again.'
      setError(message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex items-center justify-center py-16">
      <div className="w-full max-w-sm">
        <div className="text-center mb-8">
          <h1 className="text-2xl font-bold text-yellow-400">Welcome to COG Live Dealer</h1>
          <p className="text-gray-400 mt-2 text-sm">Choose a display name to get started</p>
        </div>

        <div className="bg-gray-800 rounded-lg border border-gray-700 p-6 shadow-xl">
          {error && (
            <div className="mb-4 rounded-md bg-red-900/50 border border-red-700 px-4 py-3 text-sm text-red-300">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label htmlFor="nickname" className="block text-sm font-medium text-gray-300 mb-1">
                Nickname
              </label>
              <input
                id="nickname"
                type="text"
                required
                minLength={2}
                maxLength={50}
                value={nickname}
                onChange={(e) => setNickname(e.target.value)}
                className="w-full rounded-md bg-gray-700 border border-gray-600 text-white px-3 py-2 text-sm placeholder-gray-500 focus:outline-none focus:border-yellow-500 focus:ring-1 focus:ring-yellow-500"
                placeholder="e.g. LuckyAce99"
              />
              <p className="mt-1 text-xs text-gray-500">This name will appear at the live dealer tables.</p>
            </div>

            <button
              type="submit"
              disabled={loading || nickname.trim().length < 2}
              className="w-full rounded-md bg-yellow-500 hover:bg-yellow-400 disabled:bg-yellow-700 disabled:cursor-not-allowed text-gray-900 font-semibold py-2 text-sm transition-colors"
            >
              {loading ? 'Creating account...' : 'Enter the Casino'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
