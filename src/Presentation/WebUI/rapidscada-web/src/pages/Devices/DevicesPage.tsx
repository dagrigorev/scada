import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Cpu, Plus, Edit, Trash2 } from 'lucide-react';
import { deviceService } from '../../services/api';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';
import { StatusBadge } from '../../components/Common/StatusBadge';

export default function DevicesPage() {
  const { t } = useTranslation();
  
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['devices'],
    queryFn: () => deviceService.getAll(),
  });

  if (isLoading) return <LoadingSpinner />;

  const devices = data?.data || [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-100">{t('devices.title')}</h1>
          <p className="text-gray-400 mt-1">Manage all SCADA devices</p>
        </div>
        <button className="btn btn-primary flex items-center gap-2">
          <Plus className="w-5 h-5" />
          {t('devices.addDevice')}
        </button>
      </div>

      {/* Devices Table */}
      <div className="glass-card overflow-hidden">
        <table className="table">
          <thead>
            <tr>
              <th>{t('devices.deviceName')}</th>
              <th>{t('devices.deviceType')}</th>
              <th>{t('devices.address')}</th>
              <th>{t('devices.communicationLine')}</th>
              <th>{t('common.status')}</th>
              <th>{t('devices.lastCommunication')}</th>
              <th>{t('common.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {devices.map((device: any) => (
              <tr key={device.id}>
                <td className="flex items-center gap-2">
                  <Cpu className="w-4 h-4 text-sky-400" />
                  <span className="font-medium">{device.name}</span>
                </td>
                <td>{device.deviceTypeName || 'Unknown'}</td>
                <td className="font-mono">{device.address}</td>
                <td>{device.communicationLineName || '-'}</td>
                <td>
                  <StatusBadge status={device.isOnline ? 'online' : 'offline'} />
                </td>
                <td className="text-sm text-gray-400">
                  {device.lastCommunication 
                    ? new Date(device.lastCommunication).toLocaleString()
                    : 'Never'}
                </td>
                <td>
                  <div className="flex gap-2">
                    <button className="btn btn-secondary text-sm px-2 py-1">
                      <Edit className="w-4 h-4" />
                    </button>
                    <button className="btn btn-danger text-sm px-2 py-1">
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
