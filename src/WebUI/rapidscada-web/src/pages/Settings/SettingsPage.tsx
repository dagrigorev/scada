import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Save, Bell, Lock, Database, Globe, Palette } from 'lucide-react';

export default function SettingsPage() {
  const { t, i18n } = useTranslation();
  const [activeTab, setActiveTab] = useState('general');

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-100">{t('settings.title')}</h1>
        <p className="text-gray-400 mt-1">Configure system preferences and options</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        {/* Sidebar */}
        <div className="glass-card p-4">
          <nav className="space-y-1">
            <button
              onClick={() => setActiveTab('general')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-left ${
                activeTab === 'general' 
                  ? 'bg-sky-500/20 text-sky-400 border-l-4 border-sky-500' 
                  : 'text-gray-400 hover:bg-gray-800'
              }`}
            >
              <Globe className="w-5 h-5" />
              General
            </button>

            <button
              onClick={() => setActiveTab('appearance')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-left ${
                activeTab === 'appearance' 
                  ? 'bg-sky-500/20 text-sky-400 border-l-4 border-sky-500' 
                  : 'text-gray-400 hover:bg-gray-800'
              }`}
            >
              <Palette className="w-5 h-5" />
              Appearance
            </button>

            <button
              onClick={() => setActiveTab('notifications')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-left ${
                activeTab === 'notifications' 
                  ? 'bg-sky-500/20 text-sky-400 border-l-4 border-sky-500' 
                  : 'text-gray-400 hover:bg-gray-800'
              }`}
            >
              <Bell className="w-5 h-5" />
              Notifications
            </button>

            <button
              onClick={() => setActiveTab('security')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-left ${
                activeTab === 'security' 
                  ? 'bg-sky-500/20 text-sky-400 border-l-4 border-sky-500' 
                  : 'text-gray-400 hover:bg-gray-800'
              }`}
            >
              <Lock className="w-5 h-5" />
              Security
            </button>

            <button
              onClick={() => setActiveTab('database')}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-left ${
                activeTab === 'database' 
                  ? 'bg-sky-500/20 text-sky-400 border-l-4 border-sky-500' 
                  : 'text-gray-400 hover:bg-gray-800'
              }`}
            >
              <Database className="w-5 h-5" />
              Database
            </button>
          </nav>
        </div>

        {/* Content */}
        <div className="md:col-span-3 glass-card p-6">
          {/* General Settings */}
          {activeTab === 'general' && (
            <div className="space-y-6">
              <h2 className="text-xl font-semibold text-gray-100">General Settings</h2>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Language
                </label>
                <select 
                  className="input max-w-xs"
                  value={i18n.language}
                  onChange={(e) => i18n.changeLanguage(e.target.value)}
                >
                  <option value="en">English</option>
                  <option value="ru">Русский (Russian)</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Time Zone
                </label>
                <select className="input max-w-xs">
                  <option>UTC</option>
                  <option>America/New_York</option>
                  <option>Europe/London</option>
                  <option>Europe/Moscow</option>
                  <option>Asia/Tokyo</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Date Format
                </label>
                <select className="input max-w-xs">
                  <option>YYYY-MM-DD</option>
                  <option>DD/MM/YYYY</option>
                  <option>MM/DD/YYYY</option>
                </select>
              </div>

              <div>
                <label className="flex items-center gap-2">
                  <input type="checkbox" className="rounded" defaultChecked />
                  <span className="text-sm text-gray-300">
                    Enable auto-refresh for real-time data
                  </span>
                </label>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Refresh Interval (seconds)
                </label>
                <input 
                  type="number" 
                  className="input max-w-xs" 
                  defaultValue="5"
                  min="1"
                  max="60"
                />
              </div>
            </div>
          )}

          {/* Appearance Settings */}
          {activeTab === 'appearance' && (
            <div className="space-y-6">
              <h2 className="text-xl font-semibold text-gray-100">Appearance</h2>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Theme
                </label>
                <div className="flex gap-4">
                  <button className="px-4 py-2 bg-gray-800 border-2 border-sky-500 rounded-lg">
                    Dark (Current)
                  </button>
                  <button className="px-4 py-2 bg-gray-800 border-2 border-gray-700 rounded-lg opacity-50">
                    Light (Coming Soon)
                  </button>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Primary Color
                </label>
                <div className="flex gap-3">
                  <button className="w-10 h-10 rounded-full bg-sky-500 border-2 border-white"></button>
                  <button className="w-10 h-10 rounded-full bg-blue-500"></button>
                  <button className="w-10 h-10 rounded-full bg-purple-500"></button>
                  <button className="w-10 h-10 rounded-full bg-green-500"></button>
                  <button className="w-10 h-10 rounded-full bg-amber-500"></button>
                </div>
              </div>

              <div>
                <label className="flex items-center gap-2">
                  <input type="checkbox" className="rounded" defaultChecked />
                  <span className="text-sm text-gray-300">
                    Enable glass-morphism effects
                  </span>
                </label>
              </div>

              <div>
                <label className="flex items-center gap-2">
                  <input type="checkbox" className="rounded" defaultChecked />
                  <span className="text-sm text-gray-300">
                    Show animations
                  </span>
                </label>
              </div>
            </div>
          )}

          {/* Notifications Settings */}
          {activeTab === 'notifications' && (
            <div className="space-y-6">
              <h2 className="text-xl font-semibold text-gray-100">Notifications</h2>

              <div className="space-y-4">
                <label className="flex items-center justify-between p-4 bg-gray-800 rounded-lg">
                  <div>
                    <p className="font-medium text-gray-100">Critical Alarms</p>
                    <p className="text-sm text-gray-400">Get notified about critical alarms</p>
                  </div>
                  <input type="checkbox" className="rounded" defaultChecked />
                </label>

                <label className="flex items-center justify-between p-4 bg-gray-800 rounded-lg">
                  <div>
                    <p className="font-medium text-gray-100">Device Offline</p>
                    <p className="text-sm text-gray-400">Alert when devices go offline</p>
                  </div>
                  <input type="checkbox" className="rounded" defaultChecked />
                </label>

                <label className="flex items-center justify-between p-4 bg-gray-800 rounded-lg">
                  <div>
                    <p className="font-medium text-gray-100">System Updates</p>
                    <p className="text-sm text-gray-400">Notifications about system updates</p>
                  </div>
                  <input type="checkbox" className="rounded" />
                </label>

                <label className="flex items-center justify-between p-4 bg-gray-800 rounded-lg">
                  <div>
                    <p className="font-medium text-gray-100">Email Notifications</p>
                    <p className="text-sm text-gray-400">Receive alerts via email</p>
                  </div>
                  <input type="checkbox" className="rounded" />
                </label>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Email Address
                </label>
                <input 
                  type="email" 
                  className="input max-w-md" 
                  placeholder="your@email.com"
                />
              </div>
            </div>
          )}

          {/* Security Settings */}
          {activeTab === 'security' && (
            <div className="space-y-6">
              <h2 className="text-xl font-semibold text-gray-100">Security</h2>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Session Timeout (minutes)
                </label>
                <input 
                  type="number" 
                  className="input max-w-xs" 
                  defaultValue="30"
                  min="5"
                  max="480"
                />
              </div>

              <div>
                <label className="flex items-center gap-2">
                  <input type="checkbox" className="rounded" defaultChecked />
                  <span className="text-sm text-gray-300">
                    Require password change every 90 days
                  </span>
                </label>
              </div>

              <div>
                <label className="flex items-center gap-2">
                  <input type="checkbox" className="rounded" defaultChecked />
                  <span className="text-sm text-gray-300">
                    Enable two-factor authentication (2FA)
                  </span>
                </label>
              </div>

              <div>
                <label className="flex items-center gap-2">
                  <input type="checkbox" className="rounded" />
                  <span className="text-sm text-gray-300">
                    Log all user actions
                  </span>
                </label>
              </div>

              <div className="pt-4 border-t border-gray-700">
                <button className="btn btn-danger">
                  Change Password
                </button>
              </div>
            </div>
          )}

          {/* Database Settings */}
          {activeTab === 'database' && (
            <div className="space-y-6">
              <h2 className="text-xl font-semibold text-gray-100">Database Settings</h2>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Data Retention Period (days)
                </label>
                <input 
                  type="number" 
                  className="input max-w-xs" 
                  defaultValue="365"
                  min="30"
                />
                <p className="text-xs text-gray-500 mt-1">
                  Historical data older than this will be archived
                </p>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-300 mb-2">
                  Backup Schedule
                </label>
                <select className="input max-w-xs">
                  <option>Daily at 2:00 AM</option>
                  <option>Every 12 hours</option>
                  <option>Weekly on Sunday</option>
                  <option>Manual only</option>
                </select>
              </div>

              <div>
                <label className="flex items-center gap-2">
                  <input type="checkbox" className="rounded" defaultChecked />
                  <span className="text-sm text-gray-300">
                    Enable automatic database optimization
                  </span>
                </label>
              </div>

              <div className="pt-4 border-t border-gray-700">
                <button className="btn btn-secondary mr-2">
                  Test Connection
                </button>
                <button className="btn btn-primary">
                  Backup Now
                </button>
              </div>
            </div>
          )}

          {/* Save Button */}
          <div className="flex justify-end pt-6 border-t border-gray-700 mt-8">
            <button className="btn btn-primary">
              <Save className="w-5 h-5 mr-2" />
              Save Changes
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
