import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Radio, PlayCircle, PauseCircle, Settings } from 'lucide-react';
import { systemService } from '../../services/api';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';

export default function CommunicatorPage() {
  const { t } = useTranslation();

  const { data, isLoading } = useQuery({
    queryKey: ['communicator-status'],
    queryFn: () => systemService.getCommunicatorStatus(),
    refetchInterval: 3000,
  });

  if (isLoading) return <LoadingSpinner />;

  const communicatorData = data?.data || {
    isRunning: false,
    devices: [],
    communicationLines: [],
    pollingRate: 0,
    errorCount: 0,
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-100">{t('system.communicator')}</h1>
          <p className="text-gray-400 mt-1">Device communication and polling status</p>
        </div>
        <div className="flex gap-2">
          {communicatorData.isRunning ? (
            <button className="btn btn-danger">
              <PauseCircle className="w-5 h-5 mr-2" />
              Stop Polling
            </button>
          ) : (
            <button className="btn btn-primary">
              <PlayCircle className="w-5 h-5 mr-2" />
              Start Polling
            </button>
          )}
          <button className="btn btn-secondary">
            <Settings className="w-5 h-5 mr-2" />
            Configure
          </button>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="glass-card p-6">
          <p className="text-sm font-medium text-gray-400">Status</p>
          <p className="text-2xl font-bold text-gray-100 mt-2">
            {communicatorData.isRunning ? 'Running' : 'Stopped'}
          </p>
          <div className={`mt-2 w-3 h-3 rounded-full ${communicatorData.isRunning ? 'bg-green-500 animate-pulse' : 'bg-red-500'}`}></div>
        </div>

        <div className="glass-card p-6">
          <p className="text-sm font-medium text-gray-400">Active Devices</p>
          <p className="text-2xl font-bold text-gray-100 mt-2">
            {communicatorData.devices?.length || 0}
          </p>
          <p className="text-xs text-gray-500 mt-1">Being polled</p>
        </div>

        <div className="glass-card p-6">
          <p className="text-sm font-medium text-gray-400">Polling Rate</p>
          <p className="text-2xl font-bold text-gray-100 mt-2">
            {communicatorData.pollingRate || 0}
          </p>
          <p className="text-xs text-gray-500 mt-1">Polls/second</p>
        </div>

        <div className="glass-card p-6">
          <p className="text-sm font-medium text-gray-400">Error Count</p>
          <p className="text-2xl font-bold text-red-400 mt-2">
            {communicatorData.errorCount || 0}
          </p>
          <p className="text-xs text-gray-500 mt-1">Last hour</p>
        </div>
      </div>

      {/* Communication Lines */}
      <div className="glass-card p-6">
        <h2 className="text-lg font-semibold text-gray-100 mb-4">Communication Lines</h2>
        
        <div className="space-y-3">
          {(communicatorData.communicationLines || [
            { id: 1, name: 'Modbus TCP Line 1', protocol: 'Modbus TCP', deviceCount: 5, status: 'Active' },
            { id: 2, name: 'MQTT Line 1', protocol: 'MQTT', deviceCount: 3, status: 'Active' },
            { id: 3, name: 'Serial Line 1', protocol: 'Modbus RTU', deviceCount: 2, status: 'Inactive' },
          ]).map((line: any) => (
            <div key={line.id} className="p-4 bg-gray-800 rounded-lg">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <Radio className={`w-5 h-5 ${line.status === 'Active' ? 'text-green-400' : 'text-gray-500'}`} />
                  <div>
                    <h3 className="font-semibold text-gray-100">{line.name}</h3>
                    <p className="text-sm text-gray-400">{line.protocol}</p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="text-sm text-gray-400">{line.deviceCount} devices</p>
                  <span className={`text-xs px-2 py-1 rounded ${
                    line.status === 'Active' ? 'bg-green-500/20 text-green-400' : 'bg-gray-700 text-gray-500'
                  }`}>
                    {line.status}
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Device Polling Status */}
      <div className="glass-card p-6">
        <h2 className="text-lg font-semibold text-gray-100 mb-4">Device Polling Status</h2>
        
        <div className="overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th>Device Name</th>
                <th>Address</th>
                <th>Status</th>
                <th>Last Poll</th>
                <th>Response Time</th>
                <th>Success Rate</th>
              </tr>
            </thead>
            <tbody>
              {(communicatorData.devices || [
                { name: 'Device 1', address: '192.168.1.10', status: 'Online', lastPoll: '2s ago', responseTime: '45ms', successRate: 99.8 },
                { name: 'Device 2', address: '192.168.1.11', status: 'Online', lastPoll: '1s ago', responseTime: '32ms', successRate: 100 },
                { name: 'Device 3', address: '192.168.1.12', status: 'Timeout', lastPoll: '30s ago', responseTime: '-', successRate: 85.2 },
              ]).map((device: any, index: number) => (
                <tr key={index}>
                  <td className="font-medium">{device.name}</td>
                  <td className="font-mono text-sm">{device.address}</td>
                  <td>
                    <span className={`status-badge ${
                      device.status === 'Online' ? 'status-online' : 
                      device.status === 'Timeout' ? 'bg-amber-500/20 text-amber-400' :
                      'status-offline'
                    }`}>
                      {device.status}
                    </span>
                  </td>
                  <td className="text-sm text-gray-400">{device.lastPoll}</td>
                  <td className="font-mono text-sm">{device.responseTime}</td>
                  <td>
                    <span className={`${device.successRate >= 95 ? 'text-green-400' : 'text-amber-400'}`}>
                      {device.successRate}%
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
