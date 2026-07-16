import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import GamesPage from './pages/GamesPage';
import PickPage from './pages/PickPage';
import HistoryPage from './pages/HistoryPage';
import TicketDetailPage from './pages/TicketDetailPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/games" replace />} />
        <Route path="/games" element={<GamesPage />} />
        <Route path="/games/:gameId/pick" element={<PickPage />} />
        <Route path="/history" element={<HistoryPage />} />
        <Route path="/tickets/:id" element={<TicketDetailPage />} />
      </Routes>
    </BrowserRouter>
  );
}
