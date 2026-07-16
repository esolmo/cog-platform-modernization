'use strict'

import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'
import { useWagerDraftStore } from '@/stores/wagerDraftStore'

const bettingLinks = [
  { to: '/sports',         label: 'Sports' },
  { to: '/wagers/pending', label: 'Pending Wagers' },
]

const adminLinks = [
  { to: '/admin/games',  label: 'Games Manager' },
  { to: '/admin/sports', label: 'Sport Types' },
]

const ADMIN_ROLES = new Set(['Admin', 'LinesManager'])

export function MainLayout(): React.ReactElement {
  const navigate = useNavigate()
  const { loginName, logout, roles } = useAuthStore()
  const itemCount = useWagerDraftStore((s) => s.items.length)
  const isAdmin = roles.some(r => ADMIN_ROLES.has(r))

  const handleLogout = (): void => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex h-screen bg-gray-100">
      {/* Sidebar */}
      <aside className="w-64 bg-gray-900 text-white flex flex-col">
        <div className="px-6 py-5 border-b border-gray-700">
          <h1 className="text-xl font-bold tracking-wide">COG Betting</h1>
          {loginName && (
            <p className="text-xs text-gray-400 mt-1">{loginName}</p>
          )}
        </div>
        <nav className="flex-1 px-4 py-4 space-y-1 overflow-y-auto">
          {bettingLinks.map(({ to, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                `block px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-indigo-600 text-white'
                    : 'text-gray-300 hover:bg-gray-700 hover:text-white'
                }`
              }
            >
              {label}
            </NavLink>
          ))}
          {itemCount > 0 && (
            <NavLink
              to="/wager/new"
              className="block px-4 py-2 rounded-md text-sm font-medium text-yellow-300 bg-yellow-900/30 hover:bg-yellow-900/50 transition-colors"
            >
              Wager Slip ({itemCount})
            </NavLink>
          )}
          {isAdmin && (
            <>
              <div className="pt-4 pb-1 px-4 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                Lines Manager
              </div>
              {adminLinks.map(({ to, label }) => (
                <NavLink
                  key={to}
                  to={to}
                  className={({ isActive }) =>
                    `block px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                      isActive
                        ? 'bg-indigo-600 text-white'
                        : 'text-gray-300 hover:bg-gray-700 hover:text-white'
                    }`
                  }
                >
                  {label}
                </NavLink>
              ))}
            </>
          )}
        </nav>
        <div className="px-4 py-4 border-t border-gray-700">
          <button
            type="button"
            onClick={handleLogout}
            className="w-full px-4 py-2 text-sm text-gray-300 hover:text-white hover:bg-gray-700 rounded-md text-left"
          >
            Sign out
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-auto">
        <div className="p-8">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
