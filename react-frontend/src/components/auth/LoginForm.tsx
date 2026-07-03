import React, { useState } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import { authService } from '@/services/authService';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';

interface LoginFormProps {
  onSuccess?: () => void;
  onError?: (error: string) => void;
}

export const LoginForm: React.FC<LoginFormProps> = ({ onSuccess, onError }) => {
  const { t } = useLanguage();
  
  const [phoneNumber, setPhoneNumber] = useState('');
  const [otpCode, setOtpCode] = useState('');
  const [showOtp, setShowOtp] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSendOtp = async (e: React.FormEvent) => {
    e.preventDefault();
    
    const phoneWithoutSpaces = phoneNumber.replace(/\s/g, '');
    if (!phoneWithoutSpaces || phoneWithoutSpaces.length < 10) {
      setError(t('auth.errors.phoneInvalid', 'Please enter a valid phone number'));
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      let formattedPhone = phoneWithoutSpaces;
      if (!formattedPhone.startsWith('+')) {
        formattedPhone = formattedPhone.startsWith('91') 
          ? `+${formattedPhone}` 
          : `+91${formattedPhone}`;
      }

      authService.initRecaptcha('recaptcha-container');
      await authService.login(formattedPhone, '');
      setShowOtp(true);
    } catch (err: any) {
      const msg = err.message || 'Failed to send OTP';
      setError(msg);
      onError?.(msg);
    } finally {
      setIsLoading(false);
    }
  };

  const handleVerifyOtp = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!otpCode.trim() || otpCode.length < 6) {
      setError(t('auth.errors.codeInvalid', 'Please enter the 6-digit OTP'));
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      let formattedPhone = phoneNumber.replace(/\s/g, '');
      if (!formattedPhone.startsWith('+')) {
        formattedPhone = formattedPhone.startsWith('91') 
          ? `+${formattedPhone}` 
          : `+91${formattedPhone}`;
      }

      await authService.confirmRegistration(formattedPhone, otpCode.trim());
      localStorage.removeItem('persist:root');
      window.location.href = '/';
    } catch (err: any) {
      const msg = err.message || 'OTP verification failed';
      setError(msg);
      onError?.(msg);
    } finally {
      setIsLoading(false);
    }
  };

  if (showOtp) {
    return (
      <form onSubmit={handleVerifyOtp} className="space-y-6">
        <div>
          <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">
            {t('auth.enterOtp', 'Enter OTP')}
          </h3>
          <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
            {t('auth.otpSent', 'OTP sent to')} {phoneNumber}
          </p>
          <input
            type="text"
            inputMode="numeric"
            maxLength={6}
            value={otpCode}
            onChange={(e) => setOtpCode(e.target.value.replace(/\D/g, ''))}
            className="w-full px-4 py-3 text-center text-2xl tracking-widest border rounded-lg focus:ring-2 focus:ring-primary-500 bg-white dark:bg-gray-800 text-gray-900 dark:text-white dark:border-gray-600"
            placeholder="123456"
            disabled={isLoading}
            autoFocus
          />
        </div>

        {error && (
          <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
            <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
          </div>
        )}

        <button
          type="submit"
          disabled={isLoading || otpCode.length < 6}
          className="w-full py-3 px-4 rounded-lg font-medium text-white bg-primary-600 hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isLoading ? <LoadingSpinner size="sm" /> : t('auth.verifyOtp', 'Verify OTP')}
        </button>
      </form>
    );
  }

  return (
    <form onSubmit={handleSendOtp} className="space-y-6">
      <div>
        <label htmlFor="phoneNumber" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
          {t('auth.phoneNumber', 'Phone Number')}
        </label>
        <input
          id="phoneNumber"
          type="tel"
          value={phoneNumber}
          onChange={(e) => { setPhoneNumber(e.target.value); setError(''); }}
          className="w-full px-4 py-2 border rounded-lg focus:ring-2 focus:ring-primary-500 bg-white dark:bg-gray-800 text-gray-900 dark:text-white dark:border-gray-600 border-gray-300"
          placeholder="+91 9876543210"
          disabled={isLoading}
        />
      </div>

      {error && (
        <div className="p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg">
          <p className="text-sm text-red-600 dark:text-red-400">{error}</p>
        </div>
      )}

      <button
        type="submit"
        disabled={isLoading}
        className="w-full py-3 px-4 rounded-lg font-medium text-white bg-primary-600 hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {isLoading ? <LoadingSpinner size="sm" /> : t('auth.sendOtp', 'Send OTP')}
      </button>
    </form>
  );
};
