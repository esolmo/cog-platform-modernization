'use strict'

import React from 'react'
import { cn } from '../utils/cn'

type BadgeVariant = 'default' | 'success' | 'warning' | 'danger' | 'info'

export interface BadgeProps {
  variant?: BadgeVariant
  className?: string
  children: React.ReactNode
}

const variantClasses: Record<BadgeVariant, string> = {
  default: 'bg-gray-700 text-gray-300',
  success: 'bg-green-900/60 text-green-300 border border-green-700',
  warning: 'bg-yellow-900/60 text-yellow-300 border border-yellow-700',
  danger: 'bg-red-900/60 text-red-300 border border-red-700',
  info: 'bg-blue-900/60 text-blue-300 border border-blue-700',
}

export function Badge({ variant = 'default', className, children }: BadgeProps): React.ReactElement {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium',
        variantClasses[variant],
        className
      )}
    >
      {children}
    </span>
  )
}
