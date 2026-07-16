'use strict'

import type { GameStatus } from '@/types/betting'

const CONFIG: Record<GameStatus, { label: string; className: string }> = {
  Upcoming:   { label: 'Upcoming',    className: 'bg-blue-100 text-blue-800' },
  InProgress: { label: 'In Progress', className: 'bg-yellow-100 text-yellow-800' },
  Final:      { label: 'Final',       className: 'bg-gray-100 text-gray-700' },
  Postponed:  { label: 'Postponed',   className: 'bg-orange-100 text-orange-800' },
  Cancelled:  { label: 'Cancelled',   className: 'bg-red-100 text-red-700' },
}

interface Props {
  status: GameStatus
}

export function GameStatusBadge({ status }: Props): React.ReactElement {
  const { label, className } = CONFIG[status] ?? { label: status, className: 'bg-gray-100 text-gray-700' }
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${className}`}>
      {label}
    </span>
  )
}
