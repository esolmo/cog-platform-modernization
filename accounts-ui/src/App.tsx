import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { Navigate, Route, BrowserRouter as Router, Routes } from 'react-router-dom';
import RequireAuth from './components/RequireAuth';
import Layout from './components/Layout';
import CustomerDashboardPage from './pages/CustomerDashboardPage';
import CustomerListPage from './pages/CustomerListPage';
import CreateCustomerPage from './pages/CreateCustomerPage';
import AgentHierarchyPage from './pages/AgentHierarchyPage';
import AgentListPage from './pages/AgentListPage';
import AgentDetailPage from './pages/AgentDetailPage';
import SettlementPage from './pages/SettlementPage';
import PositionPage from './pages/PositionPage';
import BatchTransactionsPage from './pages/BatchTransactionsPage';
import LoginPage from './pages/LoginPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <Router>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            element={
              <RequireAuth>
                <Layout />
              </RequireAuth>
            }
          >
            <Route index element={<Navigate to="/customers" replace />} />
            <Route path="/customers" element={<CustomerListPage />} />
            <Route path="/customers/new" element={<CreateCustomerPage />} />
            <Route path="/customers/:id" element={<CustomerDashboardPage />} />
            <Route path="/agents" element={<AgentListPage />} />
            <Route path="/agents/:id" element={<AgentDetailPage />} />
            <Route path="/agents/:id/hierarchy" element={<AgentHierarchyPage />} />
            <Route path="/settlement"  element={<SettlementPage />} />
            <Route path="/position"    element={<PositionPage />} />
            <Route path="/transactions/batch" element={<BatchTransactionsPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/customers" replace />} />
        </Routes>
      </Router>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
}
