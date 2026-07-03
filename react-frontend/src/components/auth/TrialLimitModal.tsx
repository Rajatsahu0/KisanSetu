import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useLanguage } from '@/contexts/LanguageContext';

interface TrialLimitModalProps {
  feature: string;
  limit: number;
  onClose: () => void;
}

export const TrialLimitModal: React.FC<TrialLimitModalProps> = ({ feature, limit, onClose }) => {
  const navigate = useNavigate();
  const { t } = useLanguage();

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm" onClick={onClose}>
      <div className="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl max-w-md w-full mx-4 p-6" onClick={e => e.stopPropagation()}>
        <div className="text-center">
          <div className="text-5xl mb-4">🌾</div>
          <h2 className="text-xl font-bold text-gray-900 dark:text-white mb-2">
            {t('auth.trialLimit', 'Free Trial Complete!')}
          </h2>
          <p className="text-gray-600 dark:text-gray-300 mb-1">
            You've used all <span className="font-bold text-green-600">{limit}</span> free {feature} queries.
          </p>
          <p className="text-sm text-gray-500 dark:text-gray-400 mb-6">
            {t('auth.trialLimitMessage', 'Register for free to get unlimited access to all features.')}
          </p>

          <div className="space-y-3">
            <button
              onClick={() => navigate('/register')}
              className="w-full py-3 bg-green-600 hover:bg-green-700 text-white font-medium rounded-xl transition-colors"
            >
              🚀 {t('auth.registerFree', 'Register Free — Unlimited Access')}
            </button>
            <button
              onClick={() => navigate('/login')}
              className="w-full py-2.5 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 rounded-xl transition-colors text-sm"
            >
              {t('auth.alreadyHaveAccount', 'Already have an account? Login')}
            </button>
            <button onClick={onClose} className="text-xs text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
              {t('auth.continueBrowsing', 'Continue browsing')}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
