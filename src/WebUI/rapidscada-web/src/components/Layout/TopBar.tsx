import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Bell, User, LogOut, Globe } from 'lucide-react';
import { useAuthStore } from '../../stores/authStore';
import { useState } from 'react';

export default function TopBar() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const { user, logout } = useAuthStore();
  const [showUserMenu, setShowUserMenu] = useState(false);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const changeLanguage = (lang: string) => {
    i18n.changeLanguage(lang);
  };

  return (
    <header className="h-16 bg-gray-800 border-b border-gray-700 flex items-center justify-between px-6">
      {/* Left side - could add breadcrumbs */}
      <div className="flex items-center">
        <h2 className="text-lg font-semibold text-gray-100">
          {/* Dynamic page title can go here */}
        </h2>
      </div>

      {/* Right side - actions */}
      <div className="flex items-center gap-4">
        {/* Language switcher */}
        <div className="flex items-center gap-2">
          <Globe className="w-5 h-5 text-gray-400" />
          <select
            value={i18n.language}
            onChange={(e) => changeLanguage(e.target.value)}
            className="bg-gray-700 text-gray-100 text-sm rounded px-2 py-1 border border-gray-600 focus:outline-none focus:ring-2 focus:ring-sky-500"
          >
            <option value="en">English</option>
            <option value="ru">Русский</option>
          </select>
        </div>

        {/* Notifications */}
        <button className="relative p-2 text-gray-400 hover:text-gray-100 rounded-lg hover:bg-gray-700">
          <Bell className="w-5 h-5" />
          <span className="absolute top-1 right-1 w-2 h-2 bg-red-500 rounded-full"></span>
        </button>

        {/* User menu */}
        <div className="relative">
          <button
            onClick={() => setShowUserMenu(!showUserMenu)}
            className="flex items-center gap-2 p-2 rounded-lg hover:bg-gray-700"
          >
            <div className="w-8 h-8 bg-sky-500 rounded-full flex items-center justify-center">
              <User className="w-5 h-5 text-white" />
            </div>
            <div className="text-left">
              <div className="text-sm font-medium text-gray-100">{user?.userName}</div>
              <div className="text-xs text-gray-400">{user?.roles[0]}</div>
            </div>
          </button>

          {/* Dropdown menu */}
          {showUserMenu && (
            <div className="absolute right-0 mt-2 w-48 glass-card border border-gray-700 rounded-lg shadow-lg z-50">
              <div className="p-2">
                <button
                  onClick={handleLogout}
                  className="w-full flex items-center gap-2 px-3 py-2 text-sm text-gray-300 hover:bg-gray-700 rounded-lg"
                >
                  <LogOut className="w-4 h-4" />
                  {t('auth.logout')}
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}
