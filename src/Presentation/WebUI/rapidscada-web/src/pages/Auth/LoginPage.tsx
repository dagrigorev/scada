import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuthStore } from '../../stores/authStore';
import { authService } from '../../services/api';
import toast from 'react-hot-toast';

export default function LoginPage() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const login = useAuthStore((state) => state.login);

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const response = await authService.login(username, password);
      const { user, accessToken, refreshToken } = response.data;

      login(user, accessToken, refreshToken);
      toast.success(t('auth.loginSuccess'));
      navigate('/');
    } catch (error: any) {
      const message =
        error.response?.data?.message || t('auth.loginError');
      toast.error(message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-gray-900 via-gray-800 to-gray-900">
      <div className="glass-card p-8 w-full max-w-md">
        {/* Header */}
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-sky-400 mb-2">RapidSCADA</h1>
          <p className="text-gray-400">{t('auth.signInToContinue')}</p>
        </div>

        {/* Login Form */}
        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-300 mb-2">
              {t('auth.username')}
            </label>
            <input
              type="text"
              className="input"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
              autoFocus
              placeholder="admin"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-300 mb-2">
              {t('auth.password')}
            </label>
            <input
              type="password"
              className="input"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              placeholder="••••••••"
            />
          </div>

          <button
            type="submit"
            className="btn btn-primary w-full"
            disabled={loading}
          >
            {loading ? (
              <span className="flex items-center justify-center">
                <svg
                  className="animate-spin h-5 w-5 mr-3"
                  viewBox="0 0 24 24"
                >
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                    fill="none"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  />
                </svg>
                {t('common.loading')}
              </span>
            ) : (
              t('auth.login')
            )}
          </button>
        </form>

        {/* Language Switcher */}
        <div className="mt-6 flex justify-center gap-4">
          <button
            onClick={() => i18n.changeLanguage('en')}
            className={`text-sm ${
              i18n.language === 'en'
                ? 'text-sky-400 font-semibold'
                : 'text-gray-500 hover:text-gray-300'
            }`}
          >
            English
          </button>
          <span className="text-gray-600">|</span>
          <button
            onClick={() => i18n.changeLanguage('ru')}
            className={`text-sm ${
              i18n.language === 'ru'
                ? 'text-sky-400 font-semibold'
                : 'text-gray-500 hover:text-gray-300'
            }`}
          >
            Русский
          </button>
        </div>
      </div>
    </div>
  );
}
