import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { useLanguage } from '@/contexts/LanguageContext';

interface ActionItem {
  icon: string;
  label: string;
  path: string;
  requiresAuth: boolean;
  color: string;
}

const ACTIONS: ActionItem[] = [
  { icon: '🌱', label: 'soilAnalysis', path: '/soil-analysis', requiresAuth: true, color: 'bg-green-50 dark:bg-green-900/20 hover:bg-green-100 dark:hover:bg-green-900/40' },
  { icon: '📅', label: 'plantingAdvice', path: '/planting-advisory', requiresAuth: false, color: 'bg-purple-50 dark:bg-purple-900/20 hover:bg-purple-100 dark:hover:bg-purple-900/40' },
  { icon: '⭐', label: 'qualityGrade', path: '/quality-grading', requiresAuth: true, color: 'bg-yellow-50 dark:bg-yellow-900/20 hover:bg-yellow-100 dark:hover:bg-yellow-900/40' },
  { icon: '🎤', label: 'voiceQuery', path: '/voice-queries', requiresAuth: false, color: 'bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40' },
  { icon: '📊', label: 'analytics', path: '/historical-data', requiresAuth: true, color: 'bg-indigo-50 dark:bg-indigo-900/20 hover:bg-indigo-100 dark:hover:bg-indigo-900/40' },
  { icon: '👤', label: 'profile', path: '/profile', requiresAuth: true, color: 'bg-gray-50 dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600' },
];

export const ActionCenter: React.FC = () => {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const { t } = useLanguage();

  const actionLabels: Record<string, string> = {
    voiceQuery: t('dashboard.voiceQuery', 'Voice Query'),
    plantingAdvice: t('dashboard.plantingAdvice', 'Planting Advice'),
    soilAnalysis: t('nav.soilAnalysis', 'Soil Analysis'),
    qualityGrade: t('dashboard.qualityGrade', 'Quality Grade'),
    analytics: t('dashboard.analytics', 'Analytics'),
    profile: t('nav.profile', 'Profile'),
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 p-5">
      <h3 className="text-base font-semibold text-gray-900 dark:text-white mb-4">{t('dashboard.actionCenter', 'Action Center')}</h3>
      <div className="grid grid-cols-3 sm:grid-cols-6 gap-3">
        {ACTIONS.map(action => {
          const locked = action.requiresAuth && !isAuthenticated;
          return (
            <button key={action.path}
              onClick={() => navigate(locked ? '/login' : action.path)}
              className={`flex flex-col items-center gap-1.5 p-3 rounded-xl transition-colors ${action.color} relative`}
              aria-label={action.label}>
              <span className="text-2xl">{action.icon}</span>
              <span className="text-[11px] font-medium text-gray-700 dark:text-gray-300 text-center leading-tight">{actionLabels[action.label] || action.label}</span>
              {locked && (
                <span className="absolute top-1 right-1 text-[9px]">🔒</span>
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
};
