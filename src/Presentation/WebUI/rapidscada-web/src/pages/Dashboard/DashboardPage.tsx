import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Activity, AlertTriangle, Cpu, Tag } from 'lucide-react';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';
import { deviceService, alarmService, tagService, systemService } from '../../services/api';

export default function DashboardPage() {
  const { t } = useTranslation();

  // Fetch dashboard data
  const { data: devices, isLoading: devicesLoading } = useQuery({
    queryKey: ['devices'],
    queryFn: () => deviceService.getAll(),
  });

  const { data: alarms, isLoading: alarmsLoading } = useQuery({
    queryKey: ['alarms'],
    queryFn: () => alarmService.getActive(),
  });

  const { data: tags, isLoading: tagsLoading } = useQuery({
    queryKey: ['tags'],
    queryFn: () => tagService.getAll(),
  });

  const { data: services, isLoading: servicesLoading } = useQuery({
    queryKey: ['services'],
    queryFn: () => systemService.getServicesStatus(),
  });

  if (devicesLoading || alarmsLoading || tagsLoading || servicesLoading) {
    return <LoadingSpinner message={t('common.loading')} />;
  }

  const devicesOnline = devices?.data.filter((d: any) => d.isOnline).length || 0;
  const devicesTotal = devices?.data.length || 0;
  const activeAlarms = alarms?.data.length || 0;
  const totalTags = tags?.data.length || 0;
  const servicesRunning =
    services?.data.filter((s: any) => s.status === 'Running').length || 0;
  const servicesTotal = services?.data.length || 0;

  const stats = [
    {
      title: t('dashboard.devicesOnline'),
      value: `${devicesOnline}/${devicesTotal}`,
      icon: Cpu,
      color: 'text-sky-400',
      bgColor: 'bg-sky-500/10',
    },
    {
      title: t('dashboard.activeAlarms'),
      value: activeAlarms,
      icon: AlertTriangle,
      color: activeAlarms > 0 ? 'text-red-400' : 'text-green-400',
      bgColor: activeAlarms > 0 ? 'bg-red-500/10' : 'bg-green-500/10',
    },
    {
      title: t('dashboard.tagsMonitored'),
      value: totalTags,
      icon: Tag,
      color: 'text-purple-400',
      bgColor: 'bg-purple-500/10',
    },
    {
      title: t('dashboard.systemHealth'),
      value: `${servicesRunning}/${servicesTotal}`,
      icon: Activity,
      color: 'text-green-400',
      bgColor: 'bg-green-500/10',
    },
  ];

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-100">{t('dashboard.title')}</h1>
        <p className="text-gray-400 mt-1">
          Real-time system overview and statistics
        </p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {stats.map((stat, index) => (
          <div key={index} className="glass-card p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-400">{stat.title}</p>
                <p className="text-3xl font-bold text-gray-100 mt-2">{stat.value}</p>
              </div>
              <div className={`p-3 rounded-lg ${stat.bgColor}`}>
                <stat.icon className={`w-8 h-8 ${stat.color}`} />
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Active Alarms */}
      {activeAlarms > 0 && (
        <div className="glass-card p-6">
          <h2 className="text-lg font-semibold text-gray-100 mb-4">
            {t('dashboard.activeAlarms')}
          </h2>
          <div className="space-y-3">
            {alarms?.data.slice(0, 5).map((alarm: any) => (
              <div
                key={alarm.id}
                className="flex items-center justify-between p-4 bg-red-500/10 border border-red-500/20 rounded-lg"
              >
                <div className="flex items-center gap-3">
                  <AlertTriangle className="w-5 h-5 text-red-400" />
                  <div>
                    <p className="text-sm font-medium text-gray-100">{alarm.message}</p>
                    <p className="text-xs text-gray-400">
                      {new Date(alarm.triggeredAt).toLocaleString()}
                    </p>
                  </div>
                </div>
                <span className="alarm-critical px-3 py-1 rounded-full text-xs">
                  {alarm.severity}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Services Status */}
      <div className="glass-card p-6">
        <h2 className="text-lg font-semibold text-gray-100 mb-4">
          {t('system.services')}
        </h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {services?.data.map((service: any) => (
            <div
              key={service.name}
              className="p-4 bg-gray-800/50 rounded-lg border border-gray-700"
            >
              <div className="flex items-center justify-between mb-2">
                <p className="text-sm font-medium text-gray-100">{service.displayName}</p>
                <span
                  className={`status-badge ${
                    service.status === 'Running'
                      ? 'status-online'
                      : 'status-offline'
                  }`}
                >
                  {service.status}
                </span>
              </div>
              <div className="text-xs text-gray-400">
                <p>Uptime: {service.uptime || 'N/A'}</p>
                <p>
                  CPU: {service.cpuUsage?.toFixed(1) || '0'}% | Memory:{' '}
                  {service.memoryUsage?.toFixed(1) || '0'}%
                </p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
