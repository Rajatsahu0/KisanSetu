import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useLanguage } from '@/contexts/LanguageContext';
import { useNotifications } from '@/hooks/useNotifications';
import { profileService } from '@/services/profileService';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import { UserCircleIcon } from '@heroicons/react/24/outline';
import type { UserProfile } from '@/types';

const INDIAN_STATES = [
  'Andhra Pradesh', 'Arunachal Pradesh', 'Assam', 'Bihar', 'Chhattisgarh',
  'Goa', 'Gujarat', 'Haryana', 'Himachal Pradesh', 'Jharkhand',
  'Karnataka', 'Kerala', 'Madhya Pradesh', 'Maharashtra', 'Manipur',
  'Meghalaya', 'Mizoram', 'Nagaland', 'Odisha', 'Punjab',
  'Rajasthan', 'Sikkim', 'Tamil Nadu', 'Telangana', 'Tripura',
  'Uttar Pradesh', 'Uttarakhand', 'West Bengal',
  'Andaman and Nicobar Islands', 'Chandigarh', 'Dadra and Nagar Haveli and Daman and Diu',
  'Delhi', 'Jammu and Kashmir', 'Ladakh', 'Lakshadweep', 'Puducherry',
];

export const ProfileForm: React.FC = () => {
  const { user } = useAuth();
  const { t } = useLanguage();
  const { showSuccess, showError } = useNotifications();

  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [formData, setFormData] = useState<UserProfile>({
    name: '', phoneNumber: '', email: '', city: '', state: '', pincode: '',
  });

  useEffect(() => {
    if (!user) return;
    setIsLoading(true);
    profileService.getProfile()
      .then(p => setFormData({
        name: p.name || '', phoneNumber: p.phoneNumber || '', email: p.email || '',
        city: p.city || '', state: p.state || '', pincode: p.pincode || '',
      }))
      .catch(() => setFormData({
        name: user.name || '', phoneNumber: user.phoneNumber || '',
        email: '', city: '', state: '', pincode: '',
      }))
      .finally(() => setIsLoading(false));
  }, [user]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name.trim()) { showError(t('profile.errors.nameRequired') || 'Name is required', ''); return; }
    if (formData.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) { showError(t('profile.errors.invalidEmail') || 'Invalid email', ''); return; }
    if (formData.pincode && !/^\d{6}$/.test(formData.pincode)) { showError(t('profile.errors.invalidPincode') || 'Pincode must be 6 digits', ''); return; }

    setIsSaving(true);
    try {
      await profileService.updateProfile(formData);
      showSuccess(t('profile.success.updated') || 'Profile updated successfully', '');
    } catch {
      showError(t('profile.errors.updateFailed') || 'Failed to update profile', '');
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) return <div className="flex justify-center py-12"><LoadingSpinner size="lg" /></div>;

  const inputClass = "w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent bg-white dark:bg-gray-700 text-gray-900 dark:text-white";

  return (
    <form onSubmit={handleSubmit}>
      <div className="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
        {/* Header with Save button */}
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center">
            <UserCircleIcon className="h-6 w-6 text-primary-600 dark:text-primary-400 mr-2" />
            <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
              {t('profile.sections.personal') || 'Personal Information'}
            </h2>
          </div>
          <button type="submit" disabled={isSaving}
            className="px-5 py-2 bg-primary-600 hover:bg-primary-700 disabled:bg-gray-400 text-white text-sm font-medium rounded-lg transition-colors disabled:cursor-not-allowed flex items-center gap-2">
            {isSaving ? <><LoadingSpinner size="sm" />{t('profile.buttons.saving') || 'Saving...'}</> : (t('profile.buttons.save') || 'Save Changes')}
          </button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Name */}
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              {t('profile.fields.name') || 'Name'} <span className="text-red-500">*</span>
            </label>
            <input type="text" id="name" name="name" value={formData.name} onChange={handleChange} required className={inputClass}
              placeholder={t('profile.placeholders.name') || 'Enter your full name'} />
          </div>

          {/* Phone (Read-only) */}
          <div>
            <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              {t('profile.fields.phoneNumber') || 'Phone Number'} 🔒
            </label>
            <input type="tel" id="phoneNumber" value={formData.phoneNumber} readOnly disabled
              className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-gray-100 dark:bg-gray-900 text-gray-500 dark:text-gray-400 cursor-not-allowed" />
          </div>

          {/* Email */}
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Email <span className="text-gray-400 text-xs">(Optional)</span>
            </label>
            <input type="email" id="email" name="email" value={formData.email || ''} onChange={handleChange} className={inputClass}
              placeholder={t('profile.placeholders.email') || 'farmer@example.com'} />
          </div>

          {/* State */}
          <div>
            <label htmlFor="state" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              {t('profile.fields.state') || 'State / UT'}
            </label>
            <select id="state" name="state" value={formData.state} onChange={handleChange} className={inputClass}>
              <option value="">{t('profile.placeholders.state') || '-- Select State --'}</option>
              {INDIAN_STATES.map(s => <option key={s} value={s}>{s}</option>)}
            </select>
          </div>

          {/* City */}
          <div>
            <label htmlFor="city" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              {t('profile.fields.city') || 'City / District'}
            </label>
            <input type="text" id="city" name="city" value={formData.city} onChange={handleChange} className={inputClass}
              placeholder={t('profile.placeholders.city') || 'Enter your city or district'} />
          </div>

          {/* Pincode */}
          <div>
            <label htmlFor="pincode" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              {t('profile.fields.pincode') || 'Pincode'}
            </label>
            <input type="text" id="pincode" name="pincode" value={formData.pincode} onChange={handleChange}
              maxLength={6} pattern="\d{6}" className={inputClass}
              placeholder={t('profile.placeholders.pincode') || '6-digit pincode'} />
          </div>
        </div>
      </div>
    </form>
  );
};
