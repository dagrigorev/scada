import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import { useAuthStore } from './stores/authStore';

// Layout
import DashboardLayout from './components/Layout/DashboardLayout';

// Pages
import LoginPage from './pages/Auth/LoginPage';
import DashboardPage from './pages/Dashboard/DashboardPage';
import DevicesPage from './pages/Devices/DevicesPage';
import TagsPage from './pages/Tags/TagsPage';
import AlarmsPage from './pages/Alarms/AlarmsPage';
import HistoricalPage from './pages/Historical/HistoricalPage';
import MnemonicPage from './pages/Mnemonic/MnemonicPage';
import SystemStatusPage from './pages/System/SystemStatusPage';
import CommunicatorPage from './pages/System/CommunicatorPage';
import ServiceDiscoveryPage from './pages/System/ServiceDiscoveryPage';
import UsersPage from './pages/Admin/UsersPage';
import SettingsPage from './pages/Settings/SettingsPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
      staleTime: 5000,
    },
  },
});

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          
          <Route
            path="/"
            element={
              <PrivateRoute>
                <DashboardLayout />
              </PrivateRoute>
            }
          >
            <Route index element={<DashboardPage />} />
            <Route path="devices" element={<DevicesPage />} />
            <Route path="tags" element={<TagsPage />} />
            <Route path="alarms" element={<AlarmsPage />} />
            <Route path="historical" element={<HistoricalPage />} />
            <Route path="mnemonic" element={<MnemonicPage />} />
            <Route path="system">
              <Route path="status" element={<SystemStatusPage />} />
              <Route path="communicator" element={<CommunicatorPage />} />
              <Route path="discovery" element={<ServiceDiscoveryPage />} />
            </Route>
            <Route path="admin">
              <Route path="users" element={<UsersPage />} />
            </Route>
            <Route path="settings" element={<SettingsPage />} />
          </Route>
        </Routes>
        <Toaster position="top-right" />
      </BrowserRouter>
    </QueryClientProvider>
  );
}

export default App;
