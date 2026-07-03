import { useLanguage } from '@/contexts/LanguageContext';
import {
  formatDate,
  formatDateTime,
  formatRelativeTime,
  formatNumber,
  formatCurrency,
  formatPercentage,
  formatDecimal,
  formatFileSize,
} from '@/utils/localeFormatters';

/**
 * Hook for locale-aware formatting
 * Provides formatting functions that respect the current language/locale
 */
export const useLocaleFormat = () => {
  const { currentLanguage, t } = useLanguage();
  const locale = currentLanguage.code;

  return {
    /**
     * Format a date according to the current locale
     */
    formatDate: (date: Date | string, options?: Intl.DateTimeFormatOptions) =>
      formatDate(date, locale, options),

    /**
     * Format a date and time according to the current locale
     */
    formatDateTime: (date: Date | string, options?: Intl.DateTimeFormatOptions) =>
      formatDateTime(date, locale, options),

    /**
     * Format a relative time (e.g., "2 hours ago")
     */
    formatRelativeTime: (date: Date | string) =>
      formatRelativeTime(date, locale, t),

    /**
     * Format a number according to the current locale
     */
    formatNumber: (value: number, options?: Intl.NumberFormatOptions) =>
      formatNumber(value, locale, options),

    /**
     * Format currency (defaults to INR)
     */
    formatCurrency: (value: number, currency?: string, options?: Intl.NumberFormatOptions) =>
      formatCurrency(value, locale, currency, options),

    /**
     * Format a percentage
     */
    formatPercentage: (value: number, options?: Intl.NumberFormatOptions) =>
      formatPercentage(value, locale, options),

    /**
     * Format a decimal number with specific precision
     */
    formatDecimal: (value: number, decimals?: number) =>
      formatDecimal(value, locale, decimals),

    /**
     * Format file size in a human-readable format
     */
    formatFileSize: (bytes: number) =>
      formatFileSize(bytes, locale, t),

    /**
     * Get the current locale code
     */
    locale,

    /**
     * Get the current language
     */
    language: currentLanguage,
  };
};
