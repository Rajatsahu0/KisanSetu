/**
 * Locale-aware formatting utilities for dates, numbers, and currency
 * Provides culturally appropriate formatting for different languages
 */

/**
 * Format a date according to the current locale
 */
export const formatDate = (
  date: Date | string,
  locale: string,
  options?: Intl.DateTimeFormatOptions
): string => {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  
  const defaultOptions: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    ...options,
  };

  try {
    return new Intl.DateTimeFormat(locale, defaultOptions).format(dateObj);
  } catch (error) {
    // Fallback to English if locale is not supported
    return new Intl.DateTimeFormat('en', defaultOptions).format(dateObj);
  }
};

/**
 * Format a date and time according to the current locale
 */
export const formatDateTime = (
  date: Date | string,
  locale: string,
  options?: Intl.DateTimeFormatOptions
): string => {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  
  const defaultOptions: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    ...options,
  };

  try {
    return new Intl.DateTimeFormat(locale, defaultOptions).format(dateObj);
  } catch (error) {
    return new Intl.DateTimeFormat('en', defaultOptions).format(dateObj);
  }
};

/**
 * Format a relative time (e.g., "2 hours ago")
 */
export const formatRelativeTime = (
  date: Date | string,
  locale: string,
  t: (key: string, options?: any) => string
): string => {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - dateObj.getTime()) / 1000);

  if (diffInSeconds < 60) {
    return t('dateTime.justNow');
  }

  const diffInMinutes = Math.floor(diffInSeconds / 60);
  if (diffInMinutes < 60) {
    return t('dateTime.minutesAgo', { count: diffInMinutes });
  }

  const diffInHours = Math.floor(diffInMinutes / 60);
  if (diffInHours < 24) {
    return t('dateTime.hoursAgo', { count: diffInHours });
  }

  const diffInDays = Math.floor(diffInHours / 24);
  if (diffInDays === 1) {
    return t('dateTime.yesterday');
  }

  if (diffInDays < 7) {
    return t('dateTime.daysAgo', { count: diffInDays });
  }

  // For older dates, show the formatted date
  return formatDate(dateObj, locale, { month: 'short', day: 'numeric', year: 'numeric' });
};

/**
 * Format a number according to the current locale
 */
export const formatNumber = (
  value: number,
  locale: string,
  options?: Intl.NumberFormatOptions
): string => {
  try {
    return new Intl.NumberFormat(locale, options).format(value);
  } catch (error) {
    return new Intl.NumberFormat('en', options).format(value);
  }
};

/**
 * Format currency according to the current locale
 * Defaults to Indian Rupees (INR)
 */
export const formatCurrency = (
  value: number,
  locale: string,
  currency: string = 'INR',
  options?: Intl.NumberFormatOptions
): string => {
  const defaultOptions: Intl.NumberFormatOptions = {
    style: 'currency',
    currency,
    ...options,
  };

  try {
    return new Intl.NumberFormat(locale, defaultOptions).format(value);
  } catch (error) {
    return new Intl.NumberFormat('en-IN', defaultOptions).format(value);
  }
};

/**
 * Format a percentage according to the current locale
 */
export const formatPercentage = (
  value: number,
  locale: string,
  options?: Intl.NumberFormatOptions
): string => {
  const defaultOptions: Intl.NumberFormatOptions = {
    style: 'percent',
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
    ...options,
  };

  try {
    return new Intl.NumberFormat(locale, defaultOptions).format(value / 100);
  } catch (error) {
    return new Intl.NumberFormat('en', defaultOptions).format(value / 100);
  }
};

/**
 * Format a decimal number with specific precision
 */
export const formatDecimal = (
  value: number,
  locale: string,
  decimals: number = 2
): string => {
  return formatNumber(value, locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  });
};

/**
 * Format file size in a human-readable format
 */
export const formatFileSize = (
  bytes: number,
  locale: string,
  t: (key: string) => string
): string => {
  if (bytes === 0) return '0 B';

  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  const value = bytes / Math.pow(k, i);

  return `${formatDecimal(value, locale, 2)} ${sizes[i]}`;
};

/**
 * Get locale-specific number separators
 */
export const getNumberSeparators = (locale: string): { decimal: string; thousand: string } => {
  const numberWithDecimal = 1234.5;
  const formatted = new Intl.NumberFormat(locale).format(numberWithDecimal);
  
  // Extract decimal separator
  const decimalMatch = formatted.match(/[.,]/);
  const decimal = decimalMatch ? decimalMatch[0] : '.';
  
  // Extract thousand separator
  const numberWithThousand = 12345;
  const formattedThousand = new Intl.NumberFormat(locale).format(numberWithThousand);
  const thousandMatch = formattedThousand.match(/[., ]/);
  const thousand = thousandMatch ? thousandMatch[0] : ',';
  
  return { decimal, thousand };
};
