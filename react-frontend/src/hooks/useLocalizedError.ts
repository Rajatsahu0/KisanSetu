import { useLanguage } from '@/contexts/LanguageContext';
import { ApiError, ErrorResponse } from '@/utils/apiErrorHandler';

/**
 * Hook for handling localized error messages
 * Translates error codes and messages to the current language
 */
export const useLocalizedError = () => {
  const { t, currentLanguage } = useLanguage();

  /**
   * Get a localized error message from an error response
   */
  const getErrorMessage = (error: ErrorResponse | ApiError): string => {
    if ('error' in error) {
      // ErrorResponse
      const translationKey = error.error.userFriendlyMessage;
      return t(translationKey);
    } else {
      // ApiError
      const translationKey = error.userFriendlyMessage;
      return t(translationKey);
    }
  };

  /**
   * Get localized action suggestions for an error
   */
  const getErrorActions = (error: ErrorResponse): string[] => {
    // For now, return the actions as-is
    // In a full implementation, these would also be translation keys
    return error.actions;
  };

  /**
   * Format a validation error message
   */
  const formatValidationError = (field: string, errorType: string, params?: Record<string, any>): string => {
    const key = `validation.${errorType}`;
    return t(key, params);
  };

  /**
   * Format a file upload error message
   */
  const formatFileError = (errorType: 'size' | 'type', params?: Record<string, any>): string => {
    const key = errorType === 'size' ? 'errors.fileTooBig' : 'errors.invalidFileType';
    return t(key, params);
  };

  return {
    getErrorMessage,
    getErrorActions,
    formatValidationError,
    formatFileError,
    currentLanguage: currentLanguage.code,
    t,
  };
};
