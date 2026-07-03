import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { LoginForm } from '@/components/auth/LoginForm';
import { useLanguage } from '@/contexts/LanguageContext';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { t } = useLanguage();

  const from = (location.state as any)?.from?.pathname || '/dashboard';

  const handleLoginSuccess = () => {
    navigate(from, { replace: true });
  };

  const handleLoginError = (error: string) => {
    console.error('Login error:', error);
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 px-4 py-12">
      <div className="max-w-md w-full space-y-8">
        <div className="text-center">
          <h1 className="text-4xl font-bold text-primary-600 dark:text-primary-400 mb-2">
            KisanMitra AI
          </h1>
          <h2 className="text-2xl font-semibold text-gray-800 dark:text-gray-100 mb-2">
            {t('auth.welcomeBack', 'Welcome Back')}
          </h2>
          <p className="text-gray-600 dark:text-gray-400">
            {t('auth.loginPrompt', 'Sign in to access your farming assistant')}
          </p>
        </div>

        <div className="bg-white dark:bg-gray-800 shadow-lg rounded-lg p-8">
          <LoginForm onSuccess={handleLoginSuccess} onError={handleLoginError} />
        </div>

        <div className="text-center text-sm text-gray-600 dark:text-gray-400">
          <p>
            {t('auth.noAccount', "Don't have an account?")}{' '}
            <button
              onClick={() => navigate('/register')}
              className="text-primary-600 dark:text-primary-400 hover:underline font-medium"
            >
              {t('auth.register', 'Register here')}
            </button>
          </p>
        </div>
      </div>
    </div>
  );
};
