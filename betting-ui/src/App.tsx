'use strict'

import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'
import { MainLayout } from '@/components/layout/MainLayout'
import { LoginPage } from '@/pages/LoginPage'
import { SportSelectionPage } from '@/pages/SportSelectionPage'
import { GameSelectionPage } from '@/pages/GameSelectionPage'
import { WagerEntryPage } from '@/pages/WagerEntryPage'
import { WagerConfirmationPage } from '@/pages/WagerConfirmationPage'
import { PendingWagersPage } from '@/pages/PendingWagersPage'
import { GamesManagerPage } from '@/pages/admin/GamesManagerPage'
import { GameDetailPage } from '@/pages/admin/GameDetailPage'
import { SportsManagerPage } from '@/pages/admin/SportsManagerPage'

function RequireAuth({ children }: { children: React.ReactNode }): React.ReactElement {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated)
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />
}

export default function App(): React.ReactElement {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          element={
            <RequireAuth>
              <MainLayout />
            </RequireAuth>
          }
        >
          <Route index element={<Navigate to="/sports" replace />} />
          <Route path="/sports" element={<SportSelectionPage />} />
          <Route path="/sports/:sportId/games" element={<GameSelectionPage />} />
          <Route path="/wager/new" element={<WagerEntryPage />} />
          <Route path="/wager/confirm" element={<WagerConfirmationPage />} />
          <Route path="/wagers/pending" element={<PendingWagersPage />} />
          <Route path="/admin/games" element={<GamesManagerPage />} />
          <Route path="/admin/games/:gameId" element={<GameDetailPage />} />
          <Route path="/admin/sports" element={<SportsManagerPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
