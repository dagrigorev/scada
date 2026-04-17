import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { AlertTriangle, CheckCircle } from 'lucide-react';
import { alarmService } from '../../services/api';
import { LoadingSpinner } from '../../components/Common/LoadingSpinner';
import { AlarmBadge } from '../../components/Common/AlarmBadge';

export default function AlarmsPage() {
  const { t } = useTranslation();
  
  const { data, isLoading } = useQuery({
    queryKey: ['alarms'],
    queryFn: () => alarmService.getActive(),
  });

  if (isLoading) return <LoadingSpinner />;

  const alarms = data?.data || [];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-100">{t('alarms.title')}</h1>
        <p className="text-gray-400 mt-1">Active alarms and alarm history</p>
      </div>

      {alarms.length === 0 ? (
        <div className="glass-card p-12 text-center">
          <CheckCircle className="w-16 h-16 text-green-400 mx-auto mb-4" />
          <h3 className="text-lg font-semibold text-gray-100">No Active Alarms</h3>
          <p className="text-gray-400 mt-2">System is running normally</p>
        </div>
      ) : (
        <div className="space-y-4">
          {alarms.map((alarm: any) => (
            <div key={alarm.id} className="glass-card p-6">
              <div className="flex items-start justify-between">
                <div className="flex items-start gap-4 flex-1">
                  <AlertTriangle className="w-6 h-6 text-red-400 mt-1" />
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <AlarmBadge severity={alarm.severity} />
                      <span className="text-sm text-gray-400">
                        {alarm.deviceName} / {alarm.tagName}
                      </span>
                    </div>
                    <h3 className="text-lg font-semibold text-gray-100">{alarm.message}</h3>
                    <p className="text-sm text-gray-400 mt-1">
                      Triggered: {new Date(alarm.triggeredAt).toLocaleString()}
                    </p>
                  </div>
                </div>
                <button className="btn btn-primary">
                  {t('alarms.acknowledge')}
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
