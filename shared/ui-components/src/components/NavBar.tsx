'use strict'

import React from 'react'
import { cn } from '../utils/cn'

export interface NavBarLink {
  label: string
  href: string
  active?: boolean
}

export interface NavBarProps {
  brand: React.ReactNode
  links?: NavBarLink[]
  actions?: React.ReactNode
  className?: string
}

export function NavBar({ brand, links, actions, className }: NavBarProps): React.ReactElement {
  return (
    <nav className={cn('bg-gray-800 border-b border-yellow-500 shadow', className)}>
      <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
        <div className="flex items-center gap-6">
          <div className="shrink-0">{brand}</div>
          {links && links.length > 0 && (
            <ul className="hidden sm:flex items-center gap-4">
              {links.map((link) => (
                <li key={link.href}>
                  <a
                    href={link.href}
                    className={cn(
                      'text-sm transition-colors',
                      link.active
                        ? 'text-yellow-400 font-medium'
                        : 'text-gray-400 hover:text-white'
                    )}
                  >
                    {link.label}
                  </a>
                </li>
              ))}
            </ul>
          )}
        </div>
        {actions && <div className="flex items-center gap-4">{actions}</div>}
      </div>
    </nav>
  )
}
