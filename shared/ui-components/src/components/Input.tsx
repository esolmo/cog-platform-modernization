'use strict'

import React from 'react'
import { cn } from '../utils/cn'

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  error?: string
  prefix?: string
}

export const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ error, prefix, className, ...rest }, ref) => {
    return (
      <div className="relative">
        {prefix && (
          <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-gray-400 text-sm">
            {prefix}
          </span>
        )}
        <input
          ref={ref}
          className={cn(
            'w-full rounded-md bg-gray-700 border text-white text-sm placeholder-gray-500 py-2',
            'focus:outline-none focus:ring-1 focus:ring-yellow-500 focus:border-yellow-500',
            error ? 'border-red-500' : 'border-gray-600',
            prefix ? 'pl-7 pr-3' : 'px-3',
            className
          )}
          {...rest}
        />
        {error && <p className="mt-1 text-xs text-red-400">{error}</p>}
      </div>
    )
  }
)
Input.displayName = 'Input'
