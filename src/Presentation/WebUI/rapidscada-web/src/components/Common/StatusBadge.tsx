import { useTranslation } from 'react-i18next';

interface StatusBadgeProps {
  status: 'online' | 'offline' | 'warning' | 'error' | 'running';
  label?: string;
}

export function StatusBadge({ status, label }: StatusBadgeProps) {
  const { t } = useTranslation();

  const statusConfig = {
    running: {
      className: 'status-online',
      defaultLabel: t('devices.online'),
    },
    online: {
      className: 'status-online',
      defaultLabel: t('devices.online'),
    },
    offline: {
      className: 'status-offline',
      defaultLabel: t('devices.offline'),
    },
    warning: {
      className: 'status-warning',
      defaultLabel: t('common.warning'),
    },
    error: {
      className: 'status-badge bg-red-500/20 text-red-400 border-red-500/50',
      defaultLabel: t('common.error'),
    },
  };

  if(!status)
    return (
      <span className={`status-badge status-offline`}>
        {label || 'devices.offline'}
      </span>
    );

  const config = statusConfig[status];

  return (
    <span className={`status-badge ${config.className}`}>
      {label || config.defaultLabel}
    </span>
  );
}
