import React from 'react';
import { useNavigate } from 'react-router-dom';
import { RegisterForm } from '@/components/auth/RegisterForm';
import { useLanguage } from '@/contexts/LanguageContext';
import { useNotifications } from '@/hooks/useNotifications';

export const RegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useLanguage();
  const { showSuccess, showError } = useNotifications();

  const handleRegisterSuccess = () => {
    // Check if user is now logged in (tokens exist)
    const accessToken = localStorage.getItem('accessToken');
    
    if (accessToken) {
      // User was auto-logged in, go to dashboard
      showSuccess(
        t('auth.success', 'Success'),
        t('auth.registrationSuccessLoggedIn', 'Registration successful! Welcome to KisanMitra AI.'),
        5000
      );
      navigate('/dashboard');
    } else {
      // User needs to login manually
      showSuccess(
        t('auth.success', 'Success'),
        t('auth.registrationSuccess', 'Registration successful! Please login.'),
        5000
      );
      navigate('/login');
    }
  };

  const handleRegisterError = (error: string) => {
    console.error('Registration error:', error);
    showError(
      t('auth.error', 'Error'),
      error,
      5000
    );
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 px-4 py-12">
      <div className="max-w-md w-full space-y-8">
        <div className="text-center">
          <h1 className="text-4xl font-bold text-primary-600 dark:text-primary-400 mb-2">
            KisanMitra AI
          </h1>
          <h2 className="text-2xl font-semibold text-gray-800 dark:text-gray-100 mb-2">
            {t('auth.createAccount', 'Create Account')}
          </h2>
          <p className="text-gray-600 dark:text-gray-400">
            {t('auth.registerPrompt', 'Sign up to start using your farming assistant')}
          </p>
        </div>

        <div className="bg-white dark:bg-gray-800 shadow-lg rounded-lg p-8">
          <RegisterForm onSuccess={handleRegisterSuccess} onError={handleRegisterError} />
        </div>

        <div className="text-center text-sm text-gray-600 dark:text-gray-400">
          <p>
            {t('auth.haveAccount', 'Already have an account?')}{' '}
            <button
              onClick={() => navigate('/login')}
              className="text-primary-600 dark:text-primary-400 hover:underline font-medium"
            >
              {t('auth.login', 'Login here')}
            </button>
          </p>
        </div>
      </div>
    </div>
  );
};
