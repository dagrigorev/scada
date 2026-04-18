import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Server, Activity, List, CheckCircle, XCircle, AlertCircle, RefreshCw } from 'lucide-react';
import {
  useServiceDiscovery,
  useServiceHealth,
  useServiceEndpoints,
} from '../../hooks/useServiceDiscovery';
import { LoadingSpinner } from '@/components/Common/LoadingSpinner';
import { StatusBadge } from '@/components/Common/StatusBadge';

type TabType = 'services' | 'health' | 'endpoints';

export default function ServiceDiscoveryPage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<TabType>('services');

  const { data: servicesData, isLoading: servicesLoading, refetch: refetchServices } = useServiceDiscovery();
  const { data: healthData, isLoading: healthLoading, refetch: refetchHealth } = useServiceHealth();
  const { data: endpointsData, isLoading: endpointsLoading } = useServiceEndpoints();

  const tabs = [
    { id: 'services' as TabType, label: t('discovery.services'), icon: Server },
    { id: 'health' as TabType, label: t('discovery.health'), icon: Activity },
    { id: 'endpoints' as TabType, label: t('discovery.endpoints'), icon: List },
  ];

  const handleRefresh = () => {
    if (activeTab === 'services') {
      refetchServices();
    } else if (activeTab === 'health') {
      refetchHealth();
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-gray-100">{t('discovery.title')}</h1>
          <p className="text-gray-400 mt-1">{t('discovery.subtitle')}</p>
        </div>
        <button
          onClick={handleRefresh}
          className="flex items-center gap-2 px-4 py-2 bg-sky-600 hover:bg-sky-700 text-white rounded-lg transition-colors"
        >
          <RefreshCw className="w-4 h-4" />
          {t('common.refresh')}
        </button>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-700">
        <nav className="flex gap-4">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex items-center gap-2 px-4 py-3 border-b-2 transition-colors ${
                activeTab === tab.id
                  ? 'border-sky-500 text-sky-400'
                  : 'border-transparent text-gray-400 hover:text-gray-300'
              }`}
            >
              <tab.icon className="w-5 h-5" />
              {tab.label}
            </button>
          ))}
        </nav>
      </div>

      {/* Tab Content */}
      <div className="min-h-[400px]">
        {activeTab === 'services' && (
          <ServicesTab data={servicesData} isLoading={servicesLoading} />
        )}
        {activeTab === 'health' && (
          <HealthTab data={healthData} isLoading={healthLoading} />
        )}
        {activeTab === 'endpoints' && (
          <EndpointsTab data={endpointsData} isLoading={endpointsLoading} />
        )}
      </div>
    </div>
  );
}

function ServicesTab({ data, isLoading }: { data: any; isLoading: boolean }) {
  const { t } = useTranslation();

  if (isLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-4">
      {/* Summary Card */}
      <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <p className="text-sm text-gray-400">{t('discovery.totalServices')}</p>
            <p className="text-2xl font-bold text-gray-100">{data?.totalServices || 0}</p>
          </div>
          <div>
            <p className="text-sm text-gray-400">{t('discovery.environment')}</p>
            <p className="text-2xl font-bold text-gray-100">{data?.environment}</p>
          </div>
          <div>
            <p className="text-sm text-gray-400">{t('discovery.lastUpdated')}</p>
            <p className="text-sm text-gray-100">
              {data?.timestamp ? new Date(data.timestamp).toLocaleTimeString() : '-'}
            </p>
          </div>
        </div>
      </div>

      {/* Services Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {data?.services.map((service: any) => (
          <div key={service.name} className="bg-gray-800 rounded-lg p-6 border border-gray-700 hover:border-gray-600 transition-colors">
            <div className="flex justify-between items-start mb-4">
              <div>
                <h3 className="text-xl font-bold text-gray-100">{service.name}</h3>
                <p className="text-sm text-gray-400 mt-1">{service.description}</p>
              </div>
              <StatusBadge status={String(service.status).toLowerCase() as "online" | "offline" | "warning" | "error" | "running"} />
            </div>

            <div className="space-y-3">
              <div>
                <p className="text-xs text-gray-500 uppercase tracking-wider mb-1">{t('discovery.version')}</p>
                <p className="text-sm text-gray-300">{service.version}</p>
              </div>

              <div>
                <p className="text-xs text-gray-500 uppercase tracking-wider mb-1">{t('discovery.baseUrl')}</p>
                <p className="text-sm text-gray-300 font-mono">{service.baseUrl}</p>
              </div>

              <div>
                <p className="text-xs text-gray-500 uppercase tracking-wider mb-1">{t('discovery.capabilities')}</p>
                <div className="flex flex-wrap gap-1 mt-1">
                  {service.capabilities.map((cap: string) => (
                    <span
                      key={cap}
                      className="px-2 py-1 bg-gray-700 text-gray-300 text-xs rounded"
                    >
                      {cap}
                    </span>
                  ))}
                </div>
              </div>

              <div>
                <p className="text-xs text-gray-500 uppercase tracking-wider mb-1">
                  {t('discovery.endpoints')} ({service.endpoints.length})
                </p>
                <div className="space-y-1 mt-1">
                  {service.endpoints.slice(0, 3).map((endpoint: string) => (
                    <p key={endpoint} className="text-xs text-gray-400 font-mono">
                      {endpoint}
                    </p>
                  ))}
                  {service.endpoints.length > 3 && (
                    <p className="text-xs text-gray-500">
                      +{service.endpoints.length - 3} {t('common.more')}
                    </p>
                  )}
                </div>
              </div>

              <div className="flex items-center gap-2 mt-2">
                {service.requiresAuth ? (
                  <span className="text-xs text-yellow-500 flex items-center gap-1">
                    <AlertCircle className="w-3 h-3" />
                    {t('discovery.requiresAuth')}
                  </span>
                ) : (
                  <span className="text-xs text-green-500 flex items-center gap-1">
                    <CheckCircle className="w-3 h-3" />
                    {t('discovery.noAuth')}
                  </span>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function HealthTab({ data, isLoading }: { data: any; isLoading: boolean }) {
  const { t } = useTranslation();

  if (isLoading) return <LoadingSpinner />;

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'healthy':
        return 'text-green-500';
      case 'degraded':
        return 'text-yellow-500';
      case 'unhealthy':
      case 'unavailable':
        return 'text-red-500';
      default:
        return 'text-gray-500';
    }
  };

  const getStatusIcon = (isHealthy: boolean) => {
    return isHealthy ? (
      <CheckCircle className="w-5 h-5 text-green-500" />
    ) : (
      <XCircle className="w-5 h-5 text-red-500" />
    );
  };

  return (
    <div className="space-y-4">
      {/* Overall Status Card */}
      <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-gray-400">{t('discovery.overallStatus')}</p>
            <p className={`text-3xl font-bold ${getStatusColor(data?.overallStatus || 'Unknown')}`}>
              {data?.overallStatus || 'Unknown'}
            </p>
          </div>
          <div className="text-right">
            <p className="text-sm text-gray-400">{t('discovery.lastChecked')}</p>
            <p className="text-sm text-gray-100">
              {data?.timestamp ? new Date(data.timestamp).toLocaleTimeString() : '-'}
            </p>
          </div>
        </div>
      </div>

      {/* Services Health Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {data?.services.map((service: any) => (
          <div
            key={service.serviceName}
            className={`bg-gray-800 rounded-lg p-6 border transition-colors ${
              service.isHealthy ? 'border-green-900/50 hover:border-green-700' : 'border-red-900/50 hover:border-red-700'
            }`}
          >
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-bold text-gray-100">{service.serviceName}</h3>
              {getStatusIcon(service.isHealthy)}
            </div>

            <div className="space-y-2">
              <div className="flex justify-between">
                <span className="text-sm text-gray-400">{t('discovery.status')}</span>
                <span className={`text-sm font-medium ${getStatusColor(service.status)}`}>
                  {service.status}
                </span>
              </div>

              <div className="flex justify-between">
                <span className="text-sm text-gray-400">{t('discovery.responseTime')}</span>
                <span className="text-sm text-gray-100">{service.responseTimeMs}ms</span>
              </div>

              <div className="flex justify-between">
                <span className="text-sm text-gray-400">{t('discovery.lastChecked')}</span>
                <span className="text-sm text-gray-100">
                  {new Date(service.lastChecked).toLocaleTimeString()}
                </span>
              </div>

              {service.message && service.message !== 'OK' && (
                <div className="mt-3 pt-3 border-t border-gray-700">
                  <p className="text-xs text-gray-400">{t('discovery.message')}</p>
                  <p className="text-sm text-gray-300 mt-1">{service.message}</p>
                </div>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function EndpointsTab({ data, isLoading }: { data: any; isLoading: boolean }) {
  const { t } = useTranslation();
  const [selectedService, setSelectedService] = useState<string>('all');

  if (isLoading) return <LoadingSpinner />;

  const services = ['all', ...new Set(data?.endpoints.map((e: any) => e.serviceName) || [])];
  const filteredEndpoints =
    selectedService === 'all'
      ? data?.endpoints
      : data?.endpoints.filter((e: any) => e.serviceName === selectedService);

  return (
    <div className="space-y-4">
      {/* Summary and Filter */}
      <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
        <div className="flex justify-between items-center">
          <div>
            <p className="text-sm text-gray-400">{t('discovery.totalEndpoints')}</p>
            <p className="text-2xl font-bold text-gray-100">
              {filteredEndpoints?.length || 0} / {data?.totalEndpoints || 0}
            </p>
          </div>
          <div>
            <label className="text-sm text-gray-400 mr-2">{t('discovery.filterByService')}</label>
            <select
              value={selectedService}
              onChange={(e) => setSelectedService(e.target.value)}
              className="bg-gray-700 text-gray-100 border border-gray-600 rounded px-3 py-2"
            >
              {services.map((service) => (
                <option key={String(service)} value={String(service)}>
                  {String(service) === 'all' ? t('common.all') : String(service)}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Endpoints Table */}
      <div className="bg-gray-800 rounded-lg border border-gray-700 overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-900">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">
                  {t('discovery.service')}
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">
                  {t('discovery.method')}
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">
                  {t('discovery.path')}
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-400 uppercase tracking-wider">
                  {t('discovery.auth')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-700">
              {filteredEndpoints?.map((endpoint: any, index: number) => (
                <tr key={index} className="hover:bg-gray-750 transition-colors">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className="px-2 py-1 bg-gray-700 text-gray-300 text-xs rounded">
                      {endpoint.serviceName}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className="px-2 py-1 bg-sky-900 text-sky-300 text-xs rounded font-mono">
                      {endpoint.method}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <p className="text-sm text-gray-300 font-mono">{endpoint.path}</p>
                    <p className="text-xs text-gray-500 mt-1">{endpoint.fullUrl}</p>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {endpoint.requiresAuth ? (
                      <span className="text-xs text-yellow-500 flex items-center gap-1">
                        <AlertCircle className="w-3 h-3" />
                        {t('discovery.required')}
                      </span>
                    ) : (
                      <span className="text-xs text-green-500 flex items-center gap-1">
                        <CheckCircle className="w-3 h-3" />
                        {t('discovery.notRequired')}
                      </span>
                    )}
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
