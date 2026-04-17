import { Outlet } from 'react-router-dom';
import { useEffect } from 'react';
import Sidebar from './Sidebar';
import TopBar from './TopBar';
import { signalrService } from '../../services/signalrService';
import { useTagStore } from '../../stores/tagStore';

export default function DashboardLayout() {
  const updateTagValues = useTagStore((state) => state.updateTagValues);

  useEffect(() => {
    // Start SignalR connection
    signalrService.start();

    // Subscribe to tag updates
    signalrService.on('tagValuesUpdated', (updates) => {
      updateTagValues(updates);
    });

    return () => {
      signalrService.stop();
    };
  }, [updateTagValues]);

  return (
    <div className="flex h-screen bg-gray-900">
      {/* Sidebar */}
      <Sidebar />

      {/* Main content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        <TopBar />
        
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
