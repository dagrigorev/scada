import { useTranslation } from 'react-i18next';
import { AlarmSeverity } from '../../types';

interface AlarmBadgeProps {
  severity: AlarmSeverity;
}

export function AlarmBadge({ severity }: AlarmBadgeProps) {
  const { t } = useTranslation();

  const getSeverityConfig = () => {
    switch (severity) {
      case AlarmSeverity.Critical:
        return { className: 'alarm-critical', label: t('alarms.critical') };
      case AlarmSeverity.High:
        return { className: 'alarm-high', label: t('alarms.high') };
      case AlarmSeverity.Warning:
        return { className: 'alarm-warning', label: t('alarms.warning') };
      case AlarmSeverity.Low:
        return { className: 'alarm-low', label: t('alarms.low') };
      case AlarmSeverity.Info:
        return {
          className: 'status-badge bg-gray-500/20 text-gray-400',
          label: t('alarms.info'),
        };
      default:
        return { className: '', label: '' };
    }
  };

  const { className, label } = getSeverityConfig();

  return <span className={`status-badge ${className}`}>{label}</span>;
}
