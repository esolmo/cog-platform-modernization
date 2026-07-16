import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import DashboardLayout from './layouts/DashboardLayout';
import WagersReport from './pages/WagersReport';
import TransactionsReport from './pages/TransactionsReport';
import AgentsReport from './pages/AgentsReport';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Navigate to="/wagers" replace />} />
        <Route element={<DashboardLayout />}>
          <Route path="/wagers" element={<WagersReport />} />
          <Route path="/transactions" element={<TransactionsReport />} />
          <Route path="/agents" element={<AgentsReport />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
