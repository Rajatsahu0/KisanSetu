import { logApiError } from './privacyCompliantLogging';

export interface ApiError {
  code: string;
  message: string;
  userFriendlyMessage: string;
  details?: any;
  statusCode?: number;
}

export interface ErrorResponse {
  error: ApiError;
  shouldRetry: boolean;
  shouldQueue: boolean;
  shouldRedirect?: string;
  actions: string[];
}

export enum ErrorCodes {
  NETWORK_UNAVAILABLE = 'NETWORK_UNAVAILABLE',
  TOKEN_EXPIRED = 'TOKEN_EXPIRED',
  UNAUTHORIZED = 'UNAUTHORIZED',
  FORBIDDEN = 'FORBIDDEN',
  NOT_FOUND = 'NOT_FOUND',
  VALIDATION_ERROR = 'VALIDATION_ERROR',
  FILE_TOO_LARGE = 'FILE_TOO_LARGE',
  INVALID_FILE_TYPE = 'INVALID_FILE_TYPE',
  SERVER_ERROR = 'SERVER_ERROR',
  RATE_LIMIT_EXCEEDED = 'RATE_LIMIT_EXCEEDED',
  SERVICE_UNAVAILABLE = 'SERVICE_UNAVAILABLE',
  TIMEOUT = 'TIMEOUT',
  UNKNOWN_ERROR = 'UNKNOWN_ERROR',
}

const errorMap: Record<string, Partial<ErrorResponse>> = {
  [ErrorCodes.NETWORK_UNAVAILABLE]: {
    shouldRetry: true,
    shouldQueue: true,
    actions: ['Check your internet connection', 'Try again later'],
  },
  [ErrorCodes.TOKEN_EXPIRED]: {
    shouldRetry: false,
    shouldRedirect: '/login',
    actions: ['Please log in again'],
  },
  [ErrorCodes.UNAUTHORIZED]: {
    shouldRetry: false,
    shouldRedirect: '/login',
    actions: ['Please log in to continue'],
  },
  [ErrorCodes.FORBIDDEN]: {
    shouldRetry: false,
    actions: ['You do not have permission to perform this action'],
  },
  [ErrorCodes.NOT_FOUND]: {
    shouldRetry: false,
    actions: ['The requested resource was not found'],
  },
  [ErrorCodes.VALIDATION_ERROR]: {
    shouldRetry: false,
    actions: ['Please check your input and try again'],
  },
  [ErrorCodes.FILE_TOO_LARGE]: {
    shouldRetry: false,
    actions: ['Reduce file size', 'Use a smaller image or document'],
  },
  [ErrorCodes.INVALID_FILE_TYPE]: {
    shouldRetry: false,
    actions: ['Please upload a valid file type'],
  },
  [ErrorCodes.SERVER_ERROR]: {
    shouldRetry: true,
    actions: ['Try again in a few moments', 'Contact support if the problem persists'],
  },
  [ErrorCodes.RATE_LIMIT_EXCEEDED]: {
    shouldRetry: true,
    actions: ['Please wait a moment before trying again'],
  },
  [ErrorCodes.SERVICE_UNAVAILABLE]: {
    shouldRetry: true,
    shouldQueue: true,
    actions: ['Service is temporarily unavailable', 'Try again later'],
  },
  [ErrorCodes.TIMEOUT]: {
    shouldRetry: true,
    actions: ['Request timed out', 'Try again'],
  },
};

interface FetchError extends Error {
  response?: {
    status: number;
    data?: any;
  };
}

export class ApiErrorHandler {
  static handleError(error: unknown, language: string = 'en'): ErrorResponse {
    // Log error with privacy compliance
    logApiError('API Error', error);
    
    // Handle fetch/network errors
    if (error instanceof TypeError && error.message.includes('fetch')) {
      return this.formatError(
        {
          code: ErrorCodes.NETWORK_UNAVAILABLE,
          message: 'Network connection unavailable',
          userFriendlyMessage: this.getTranslatedMessage(ErrorCodes.NETWORK_UNAVAILABLE, language),
          statusCode: 0,
        },
        ErrorCodes.NETWORK_UNAVAILABLE
      );
    }

    // Handle errors with response property (fetch API errors)
    if (error && typeof error === 'object' && 'response' in error) {
      return this.handleFetchError(error as FetchError, language);
    }

    if (error instanceof Error) {
      return this.handleGenericError(error, language);
    }

    return this.getDefaultError(language);
  }

  private static handleFetchError(error: FetchError, language: string): ErrorResponse {
    // Network error
    if (!error.response) {
      return this.formatError(
        {
          code: ErrorCodes.NETWORK_UNAVAILABLE,
          message: 'Network connection unavailable',
          userFriendlyMessage: this.getTranslatedMessage(ErrorCodes.NETWORK_UNAVAILABLE, language),
          statusCode: 0,
        },
        ErrorCodes.NETWORK_UNAVAILABLE
      );
    }

    const statusCode = error.response.status;
    const responseData = error.response.data as any;

    // Map HTTP status codes to error codes
    let errorCode: string;
    switch (statusCode) {
      case 401:
        errorCode = ErrorCodes.UNAUTHORIZED;
        break;
      case 403:
        errorCode = ErrorCodes.FORBIDDEN;
        break;
      case 404:
        errorCode = ErrorCodes.NOT_FOUND;
        break;
      case 400:
        errorCode = ErrorCodes.VALIDATION_ERROR;
        break;
      case 413:
        errorCode = ErrorCodes.FILE_TOO_LARGE;
        break;
      case 429:
        errorCode = ErrorCodes.RATE_LIMIT_EXCEEDED;
        break;
      case 503:
        errorCode = ErrorCodes.SERVICE_UNAVAILABLE;
        break;
      case 504:
        errorCode = ErrorCodes.TIMEOUT;
        break;
      case 500:
      case 502:
        errorCode = ErrorCodes.SERVER_ERROR;
        break;
      default:
        errorCode = ErrorCodes.UNKNOWN_ERROR;
    }

    return this.formatError(
      {
        code: errorCode,
        message: responseData?.message || error.message,
        userFriendlyMessage: this.getTranslatedMessage(errorCode, language),
        details: responseData?.details,
        statusCode,
      },
      errorCode
    );
  }

  private static handleGenericError(error: Error, language: string): ErrorResponse {
    return this.formatError(
      {
        code: ErrorCodes.UNKNOWN_ERROR,
        message: error.message,
        userFriendlyMessage: this.getTranslatedMessage(ErrorCodes.UNKNOWN_ERROR, language),
      },
      ErrorCodes.UNKNOWN_ERROR
    );
  }

  private static formatError(apiError: ApiError, errorCode: string): ErrorResponse {
    const errorConfig = errorMap[errorCode] || {};

    return {
      error: apiError,
      shouldRetry: errorConfig.shouldRetry || false,
      shouldQueue: errorConfig.shouldQueue || false,
      shouldRedirect: errorConfig.shouldRedirect,
      actions: errorConfig.actions || ['Try again later'],
    };
  }

  private static getDefaultError(language: string): ErrorResponse {
    return this.formatError(
      {
        code: ErrorCodes.UNKNOWN_ERROR,
        message: 'An unknown error occurred',
        userFriendlyMessage: this.getTranslatedMessage(ErrorCodes.UNKNOWN_ERROR, language),
      },
      ErrorCodes.UNKNOWN_ERROR
    );
  }

  private static getTranslatedMessage(errorCode: string, language: string): string {
    // Map error codes to translation keys
    const translationKeys: Record<string, string> = {
      [ErrorCodes.NETWORK_UNAVAILABLE]: 'errors.network',
      [ErrorCodes.TOKEN_EXPIRED]: 'auth.sessionExpired',
      [ErrorCodes.UNAUTHORIZED]: 'errors.unauthorized',
      [ErrorCodes.FORBIDDEN]: 'errors.forbidden',
      [ErrorCodes.NOT_FOUND]: 'errors.notFound',
      [ErrorCodes.VALIDATION_ERROR]: 'errors.validationError',
      [ErrorCodes.FILE_TOO_LARGE]: 'errors.fileTooBig',
      [ErrorCodes.INVALID_FILE_TYPE]: 'errors.invalidFileType',
      [ErrorCodes.SERVER_ERROR]: 'errors.serverError',
      [ErrorCodes.RATE_LIMIT_EXCEEDED]: 'errors.timeout',
      [ErrorCodes.SERVICE_UNAVAILABLE]: 'errors.serverError',
      [ErrorCodes.TIMEOUT]: 'errors.timeout',
      [ErrorCodes.UNKNOWN_ERROR]: 'errors.generic',
    };

    // Return the translation key - actual translation will be done by the component using i18n
    return translationKeys[errorCode] || translationKeys[ErrorCodes.UNKNOWN_ERROR];
  }

  static isRetryableError(error: ErrorResponse): boolean {
    return error.shouldRetry;
  }

  static shouldQueueRequest(error: ErrorResponse): boolean {
    return error.shouldQueue;
  }
}
