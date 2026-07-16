'use strict'

import { Navigate, Route, Routes } from 'react-router-dom'
import { useAuthStore } from '@/stores/authStore'
import { MainLayout } from '@/components/layout/MainLayout'
import { LoginPage } from '@/pages/LoginPage'
import { SetupPage } from '@/pages/SetupPage'
import { CasinoLobbyPage } from '@/pages/CasinoLobbyPage'

function RequireAuth({ children }: { children: React.ReactElement }): React.ReactElement {
  const accessToken = useAuthStore((s) => s.accessToken)
  return accessToken ? children : <Navigate to="/login" replace />
}

export default function App(): React.ReactElement {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <RequireAuth>
            <MainLayout />
          </RequireAuth>
        }
      >
        <Route path="/setup" element={<SetupPage />} />
        <Route path="/casino" element={<CasinoLobbyPage />} />
        <Route path="/" element={<Navigate to="/casino" replace />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
