import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Server, Activity, HardDrive, Cpu, RotateCw } from 'lucide-react';
import { systemService } from '../../services/api';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';
import { StatusBadge } from '../../components/Common/StatusBadge';

export default function SystemStatusPage() {
  const { t } = useTranslation();

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['system-status'],
    queryFn: () => systemService.getServicesStatus(),
    refetchInterval: 5000, // Refresh every 5 seconds
  });

  if (isLoading) return <LoadingSpinner />;

  const services = data?.data || [];
  const runningCount = services.filter((s: any) => s.status === 'Running').length;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-100">{t('system.status')}</h1>
          <p className="text-gray-400 mt-1">Monitor system services and resources</p>
        </div>
        <button onClick={() => refetch()} className="btn btn-secondary">
          <RotateCw className="w-5 h-5 mr-2" />
          Refresh
        </button>
      </div>

      {/* Overview */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="glass-card p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-400">Services</p>
              <p className="text-3xl font-bold text-gray-100 mt-2">
                {runningCount}/{services.length}
              </p>
              <p className="text-xs text-gray-500 mt-1">Running</p>
            </div>
            <Server className="w-12 h-12 text-sky-400" />
          </div>
        </div>

        <div className="glass-card p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-400">Avg CPU</p>
              <p className="text-3xl font-bold text-gray-100 mt-2">
                {services.length > 0
                  ? (services.reduce((acc: number, s: any) => acc + (s.cpuUsage || 0), 0) / services.length).toFixed(1)
                  : '0'}%
              </p>
              <p className="text-xs text-gray-500 mt-1">Across services</p>
            </div>
            <Cpu className="w-12 h-12 text-green-400" />
          </div>
        </div>

        <div className="glass-card p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-400">Avg Memory</p>
              <p className="text-3xl font-bold text-gray-100 mt-2">
                {services.length > 0
                  ? (services.reduce((acc: number, s: any) => acc + (s.memoryUsage || 0), 0) / services.length).toFixed(1)
                  : '0'}%
              </p>
              <p className="text-xs text-gray-500 mt-1">Memory usage</p>
            </div>
            <HardDrive className="w-12 h-12 text-purple-400" />
          </div>
        </div>

        <div className="glass-card p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-400">System Health</p>
              <p className="text-3xl font-bold text-gray-100 mt-2">
                {((runningCount / services.length) * 100).toFixed(0)}%
              </p>
              <p className="text-xs text-gray-500 mt-1">Overall</p>
            </div>
            <Activity className="w-12 h-12 text-amber-400" />
          </div>
        </div>
      </div>

      {/* Services List */}
      <div className="glass-card p-6">
        <h2 className="text-lg font-semibold text-gray-100 mb-4">Service Details</h2>
        
        <div className="space-y-4">
          {services.map((service: any) => (
            <div key={service.name} className="p-4 bg-gray-800 rounded-lg">
              <div className="flex items-start justify-between mb-3">
                <div className="flex items-center gap-3">
                  <Server className="w-6 h-6 text-sky-400" />
                  <div>
                    <h3 className="font-semibold text-gray-100">{service.displayName || service.name}</h3>
                    <p className="text-sm text-gray-400">{service.description || 'No description'}</p>
                  </div>
                </div>
                <StatusBadge status={service.status === 'Running' ? 'online' : 'offline'} />
              </div>

              <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                <div>
                  <p className="text-gray-400">Uptime</p>
                  <p className="text-gray-100 font-medium">{service.uptime || 'N/A'}</p>
                </div>
                <div>
                  <p className="text-gray-400">CPU Usage</p>
                  <p className="text-gray-100 font-medium">{service.cpuUsage?.toFixed(1) || '0'}%</p>
                </div>
                <div>
                  <p className="text-gray-400">Memory</p>
                  <p className="text-gray-100 font-medium">{service.memoryUsage?.toFixed(1) || '0'}%</p>
                </div>
                <div>
                  <p className="text-gray-400">Port</p>
                  <p className="text-gray-100 font-medium font-mono">{service.port || 'N/A'}</p>
                </div>
              </div>

              {service.lastError && (
                <div className="mt-3 p-2 bg-red-500/10 border border-red-500/20 rounded text-sm text-red-400">
                  Error: {service.lastError}
                </div>
              )}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
