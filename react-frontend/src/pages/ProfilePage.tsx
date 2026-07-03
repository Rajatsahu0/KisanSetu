import React from 'react';
import { ProfileForm } from '@/components/profile/ProfileForm';
import { useLanguage } from '@/contexts/LanguageContext';

export const ProfilePage: React.FC = () => {
  const { t } = useLanguage();

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 py-4 sm:py-8">
      <div className="max-w-3xl mx-auto px-2 sm:px-6 lg:px-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
            {t('profile.title') || 'Profile'}
          </h1>
          <p className="mt-2 text-sm text-gray-600 dark:text-gray-400">
            {t('profile.subtitle') || 'Manage your account information'}
          </p>
        </div>

        <ProfileForm />
      </div>
    </div>
  );
};
