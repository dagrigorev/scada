import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  LayoutDashboard,
  Cpu,
  Tag,
  AlertTriangle,
  BarChart3,
  Layers,
  Server,
  Users,
  Settings,
  Network,
} from 'lucide-react';

export default function Sidebar() {
  const { t } = useTranslation();
  const location = useLocation();

  const isActive = (path: string) => location.pathname === path;

  const navItems = [
    { path: '/', icon: LayoutDashboard, label: t('nav.dashboard') },
    { path: '/devices', icon: Cpu, label: t('nav.devices') },
    { path: '/tags', icon: Tag, label: t('nav.tags') },
    { path: '/alarms', icon: AlertTriangle, label: t('nav.alarms') },
    { path: '/historical', icon: BarChart3, label: t('nav.historical') },
    { path: '/mnemonic', icon: Layers, label: t('nav.mnemonic') },
  ];

  const systemItems = [
    { path: '/system/status', icon: Server, label: t('nav.systemStatus') },
    { path: '/system/communicator', icon: Server, label: t('nav.communicator') },
    { path: '/system/discovery', icon: Network, label: t('nav.discovery') },
  ];

  const adminItems = [
    { path: '/admin/users', icon: Users, label: t('nav.users') },
    { path: '/settings', icon: Settings, label: t('nav.settings') },
  ];

  return (
    <aside className="w-64 bg-gray-800 border-r border-gray-700">
      {/* Logo */}
      <div className="h-16 flex items-center justify-center border-b border-gray-700">
        <h1 className="text-2xl font-bold text-sky-400">RapidSCADA</h1>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-4">
        {/* Main Navigation */}
        <div className="px-3 mb-6">
          {navItems.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              className={`sidebar-link ${isActive(item.path) ? 'active' : ''}`}
            >
              <item.icon className="w-5 h-5 mr-3" />
              <span>{item.label}</span>
            </Link>
          ))}
        </div>

        {/* System Section */}
        <div className="px-3 mb-6">
          <div className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2 px-4">
            {t('nav.system')}
          </div>
          {systemItems.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              className={`sidebar-link ${isActive(item.path) ? 'active' : ''}`}
            >
              <item.icon className="w-5 h-5 mr-3" />
              <span>{item.label}</span>
            </Link>
          ))}
        </div>

        {/* Admin Section */}
        <div className="px-3">
          <div className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2 px-4">
            {t('nav.admin')}
          </div>
          {adminItems.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              className={`sidebar-link ${isActive(item.path) ? 'active' : ''}`}
            >
              <item.icon className="w-5 h-5 mr-3" />
              <span>{item.label}</span>
            </Link>
          ))}
        </div>
      </nav>
    </aside>
  );
}
