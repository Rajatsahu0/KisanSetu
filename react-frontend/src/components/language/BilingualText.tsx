import React from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import { clsx } from 'clsx';

interface BilingualTextProps {
  localText: string;
  englishText?: string;
  className?: string;
  showEnglish?: boolean;
  layout?: 'stacked' | 'inline';
}

/**
 * BilingualText component displays text in both local language and English
 * Used for API responses to ensure clarity for all users
 */
export const BilingualText: React.FC<BilingualTextProps> = ({
  localText,
  englishText,
  className,
  showEnglish = true,
  layout = 'stacked'
}) => {
  const { currentLanguage } = useLanguage();

  // Don't show English translation if current language is English
  const shouldShowEnglish = showEnglish && englishText && currentLanguage.code !== 'en' && localText !== englishText;

  if (!shouldShowEnglish) {
    return <span className={className}>{localText}</span>;
  }

  if (layout === 'inline') {
    return (
      <span className={clsx('inline-flex items-center gap-2', className)}>
        <span className="font-medium">{localText}</span>
        <span className="text-sm text-gray-500 dark:text-gray-400">({englishText})</span>
      </span>
    );
  }

  // Stacked layout
  return (
    <div className={clsx('space-y-1', className)}>
      <div className="font-medium">{localText}</div>
      <div className="text-sm text-gray-500 dark:text-gray-400">{englishText}</div>
    </div>
  );
};

interface BilingualLabelProps {
  label: string;
  value: string;
  englishValue?: string;
  className?: string;
}

/**
 * BilingualLabel component for displaying labeled bilingual data
 * Commonly used in forms and data displays
 */
export const BilingualLabel: React.FC<BilingualLabelProps> = ({
  label,
  value,
  englishValue,
  className
}) => {
  return (
    <div className={clsx('space-y-1', className)}>
      <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">{label}</dt>
      <dd className="text-base text-gray-900 dark:text-gray-100">
        <BilingualText localText={value} englishText={englishValue} layout="stacked" />
      </dd>
    </div>
  );
};
