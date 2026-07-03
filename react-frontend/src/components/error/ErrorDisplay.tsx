import React from 'react';
import { ExclamationCircleIcon, ArrowPathIcon } from '@heroicons/react/24/outline';
import { ApiError } from '@/utils/apiErrorHandler';
import { useLanguage } from '@/contexts/LanguageContext';

interface ErrorDisplayProps {
  error: ApiError;
  onRetry?: () => void;
  onDismiss?: () => void;
  showDetails?: boolean;
  className?: string;
}

export const ErrorDisplay: React.FC<ErrorDisplayProps> = ({
  error, onRetry, onDismiss, showDetails = false, className = '',
}) => {
  const { t } = useLanguage();
  return (
    <div className={`bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-4 ${className}`}>
      <div className="flex items-start">
        <div className="flex-shrink-0">
          <ExclamationCircleIcon className="h-6 w-6 text-red-600 dark:text-red-400" />
        </div>
        <div className="ml-3 flex-1">
          <h3 className="text-sm font-medium text-red-800 dark:text-red-300">
            {error.userFriendlyMessage}
          </h3>
          {showDetails && error.message && (
            <div className="mt-2 text-sm text-red-700 dark:text-red-400">
              <p>{error.message}</p>
            </div>
          )}
          {showDetails && error.details && (
            <div className="mt-2 text-xs text-red-600 dark:text-red-500 font-mono bg-red-100 dark:bg-red-900/40 p-2 rounded">
              <pre className="whitespace-pre-wrap">{JSON.stringify(error.details, null, 2)}</pre>
            </div>
          )}
          {(onRetry || onDismiss) && (
            <div className="mt-4 flex gap-3">
              {onRetry && (
                <button onClick={onRetry}
                  className="inline-flex items-center px-3 py-1.5 border border-transparent text-sm font-medium rounded-md text-red-700 dark:text-red-300 bg-red-100 dark:bg-red-900/40 hover:bg-red-200 dark:hover:bg-red-900/60 transition-colors">
                  <ArrowPathIcon className="h-4 w-4 mr-1.5" />
                  {t('app.tryAgain', 'Try Again')}
                </button>
              )}
              {onDismiss && (
                <button onClick={onDismiss}
                  className="inline-flex items-center px-3 py-1.5 border border-red-300 dark:border-red-700 text-sm font-medium rounded-md text-red-700 dark:text-red-300 bg-white dark:bg-gray-800 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors">
                  {t('app.dismiss', 'Dismiss')}
                </button>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
