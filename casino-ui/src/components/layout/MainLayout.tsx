'use strict'

import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'

const navLinks = [
  { to: '/casino', label: '♠ Live Dealer' },
  { to: '/setup',  label: 'Setup' },
]

export function MainLayout(): React.ReactElement {
  const navigate = useNavigate()
  const { loginName, logout } = useAuthStore()

  const handleLogout = (): void => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex h-screen bg-gray-900">
      {/* Sidebar */}
      <aside className="w-64 bg-gray-950 text-white flex flex-col border-r border-yellow-600/30">
        <div className="px-6 py-5 border-b border-yellow-600/30">
          <h1 className="text-xl font-bold tracking-wide text-yellow-400">COG Live Dealer</h1>
          {loginName && (
            <p className="text-xs text-gray-400 mt-1">{loginName}</p>
          )}
        </div>
        <nav className="flex-1 px-4 py-4 space-y-1">
          {navLinks.map(({ to, label }) => (
            <NavLink
              key={to}
              to={to}
              className={({ isActive }) =>
                `block px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-yellow-600 text-white'
                    : 'text-gray-300 hover:bg-gray-800 hover:text-white'
                }`
              }
            >
              {label}
            </NavLink>
          ))}
        </nav>
        <div className="px-4 py-4 border-t border-yellow-600/30">
          <button
            type="button"
            onClick={handleLogout}
            className="w-full px-4 py-2 text-sm text-gray-300 hover:text-white hover:bg-gray-800 rounded-md text-left"
          >
            Sign out
          </button>
        </div>
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-auto text-white">
        <div className="p-8">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
