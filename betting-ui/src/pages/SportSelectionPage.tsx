'use strict'

import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { bettingApi } from '@/api/bettingApi'

export function SportSelectionPage(): React.ReactElement {
  const { data: sports, isLoading, isError } = useQuery({
    queryKey: ['sports'],
    queryFn: bettingApi.getSportTypes,
    staleTime: 5 * 60_000,
  })

  if (isLoading) return <div className="p-8 text-center">Loading sports...</div>
  if (isError) return <div className="p-8 text-center text-red-600">Failed to load sports. Please try again.</div>

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Select Sport</h1>
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 md:grid-cols-4">
        {sports?.map((sport) => (
          <Link
            key={sport.id}
            to={`/sports/${sport.id}/games`}
            className="flex items-center justify-center rounded-lg border-2 border-gray-200 bg-white p-6 text-center font-semibold text-gray-800 shadow-sm transition hover:border-blue-500 hover:bg-blue-50 hover:text-blue-700"
          >
            {sport.name}
          </Link>
        ))}
      </div>
    </div>
  )
}
