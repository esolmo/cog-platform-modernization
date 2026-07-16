'use strict'

import React from 'react'
import { cn } from '../utils/cn'

type AlertVariant = 'info' | 'success' | 'warning' | 'error'

export interface AlertProps {
  variant?: AlertVariant
  title?: string
  onDismiss?: () => void
  className?: string
  children: React.ReactNode
}

const variantClasses: Record<AlertVariant, string> = {
  info: 'bg-blue-900/50 border-blue-700 text-blue-300',
  success: 'bg-green-900/50 border-green-700 text-green-300',
  warning: 'bg-yellow-900/50 border-yellow-700 text-yellow-300',
  error: 'bg-red-900/50 border-red-700 text-red-300',
}

export function Alert({ variant = 'info', title, onDismiss, className, children }: AlertProps): React.ReactElement {
  return (
    <div
      role="alert"
      className={cn('rounded-md border px-4 py-3 text-sm', variantClasses[variant], className)}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex-1">
          {title && <p className="font-semibold mb-1">{title}</p>}
          <div>{children}</div>
        </div>
        {onDismiss && (
          <button
            type="button"
            onClick={onDismiss}
            className="shrink-0 opacity-60 hover:opacity-100 transition-opacity text-lg leading-none"
            aria-label="Dismiss"
          >
            ×
          </button>
        )}
      </div>
    </div>
  )
}
