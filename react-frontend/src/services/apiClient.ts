/**
 * Centralized API Client with authentication, retry logic, and error handling
 * Provides a consistent interface for all backend API calls
 */

import { authService } from './authService';

export interface ApiError {
  errorCode: string;
  message: string;
  userFriendlyMessage: string;
  suggestedActions: string[];
  timestamp: string;
  requestId?: string;
}

export interface ApiResponse<T> {
  data: T;
  status: number;
  headers: Headers;
}

export interface RequestConfig extends RequestInit {
  skipAuth?: boolean;
  skipRetry?: boolean;
  timeout?: number;
  onUploadProgress?: (progress: number) => void;
}

export interface RetryConfig {
  maxAttempts: number;
  backoffMultiplier: number;
  initialDelay: number;
  maxDelay: number;
  retryableStatusCodes: number[];
}

const DEFAULT_RETRY_CONFIG: RetryConfig = {
  maxAttempts: 3,
  backoffMultiplier: 2,
  initialDelay: 1000,
  maxDelay: 10000,
  retryableStatusCodes: [408, 429, 500, 502, 503, 504]
};

class ApiClient {
  private baseURL: string;
  private retryConfig: RetryConfig;
  private refreshPromise: Promise<void> | null = null;

  constructor(baseURL?: string, retryConfig?: Partial<RetryConfig>) {
    this.baseURL = baseURL || import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';
    this.retryConfig = { ...DEFAULT_RETRY_CONFIG, ...retryConfig };
  }

  /**
   * Make an HTTP request with authentication and retry logic
   */
  async request<T>(
    endpoint: string,
    config: RequestConfig = {}
  ): Promise<ApiResponse<T>> {
    const url = `${this.baseURL}${endpoint}`;
    const { skipAuth, skipRetry, timeout, onUploadProgress, ...fetchConfig } = config;

    // Add authentication header
    if (!skipAuth) {
      const token = await this.getValidToken();
      if (token) {
        fetchConfig.headers = {
          ...fetchConfig.headers,
          Authorization: `Bearer ${token}`
        };
      }
    }

    // Add default headers
    if (!fetchConfig.headers) {
      fetchConfig.headers = {};
    }

    // Execute request with retry logic
    if (skipRetry) {
      return this.executeRequest<T>(url, fetchConfig, timeout);
    } else {
      return this.executeRequestWithRetry<T>(url, fetchConfig, timeout);
    }
  }

  /**
   * Execute a single request with timeout
   */
  private async executeRequest<T>(
    url: string,
    config: RequestInit,
    timeout?: number
  ): Promise<ApiResponse<T>> {
    const controller = new AbortController();
    const timeoutId = timeout
      ? setTimeout(() => controller.abort(), timeout)
      : null;

    try {
      const response = await fetch(url, {
        ...config,
        credentials: 'omit', // Don't send credentials (we use JWT in Authorization header)
        signal: controller.signal
      });

      if (timeoutId) clearTimeout(timeoutId);

      // Handle non-2xx responses
      if (!response.ok) {
        await this.handleErrorResponse(response);
      }

      // Handle 204 No Content - don't try to parse JSON
      let data;
      if (response.status === 204 || response.headers.get('content-length') === '0') {
        data = null;
      } else {
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
          data = await response.json();
        } else {
          data = null;
        }
      }

      return {
        data,
        status: response.status,
        headers: response.headers
      };
    } catch (error) {
      if (timeoutId) clearTimeout(timeoutId);

      if (error instanceof Error && error.name === 'AbortError') {
        throw new Error('Request timeout');
      }

      throw error;
    }
  }

  /**
   * Execute request with exponential backoff retry
   */
  private async executeRequestWithRetry<T>(
    url: string,
    config: RequestInit,
    timeout?: number,
    attempt: number = 1
  ): Promise<ApiResponse<T>> {
    try {
      return await this.executeRequest<T>(url, config, timeout);
    } catch (error) {
      // Check if we should retry
      const shouldRetry = this.shouldRetryRequest(error, attempt);

      if (!shouldRetry) {
        throw error;
      }

      // Calculate delay with exponential backoff
      const delay = Math.min(
        this.retryConfig.initialDelay * Math.pow(this.retryConfig.backoffMultiplier, attempt - 1),
        this.retryConfig.maxDelay
      );

      // Wait before retrying
      await this.sleep(delay);

      // Retry the request
      return this.executeRequestWithRetry<T>(url, config, timeout, attempt + 1);
    }
  }

  /**
   * Determine if a request should be retried
   */
  private shouldRetryRequest(error: any, attempt: number): boolean {
    if (attempt >= this.retryConfig.maxAttempts) {
      return false;
    }

    // Retry on network errors
    if (error instanceof TypeError && error.message.includes('fetch')) {
      return true;
    }

    // Retry on timeout
    if (error.message === 'Request timeout') {
      return true;
    }

    // Retry on specific status codes
    if (error.status && this.retryConfig.retryableStatusCodes.includes(error.status)) {
      return true;
    }

    return false;
  }

  /**
   * Handle error responses from the API
   */
  private async handleErrorResponse(response: Response): Promise<never> {
    let errorData: ApiError;

    try {
      errorData = await response.json();
    } catch {
      // If response is not JSON, create a generic error
      errorData = {
        errorCode: 'UNKNOWN_ERROR',
        message: `HTTP ${response.status}: ${response.statusText}`,
        userFriendlyMessage: 'An unexpected error occurred',
        suggestedActions: ['Try again later', 'Contact support if the problem persists'],
        timestamp: new Date().toISOString()
      };
    }

    const error = new Error(errorData.userFriendlyMessage || errorData.message) as any;
    error.status = response.status;
    error.errorCode = errorData.errorCode;
    error.userFriendlyMessage = errorData.userFriendlyMessage;
    error.suggestedActions = errorData.suggestedActions;
    error.timestamp = errorData.timestamp;
    error.requestId = errorData.requestId;

    throw error;
  }

  /**
   * Get a valid access token, refreshing if necessary
   */
  private async getValidToken(): Promise<string | null> {
    const token = authService.getAccessToken();
    if (!token) return null;
    
    // Firebase handles token refresh internally via onAuthStateChanged
    // Just return whatever token we have — it's valid for 1 hour
    return token;
  }

  /**
   * Refresh the access token
   */
  private async refreshToken(): Promise<void> {
    const refreshToken = authService.getRefreshToken();

    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    try {
      const response = await authService.refreshToken(refreshToken);
      authService.setTokens(response.accessToken, refreshToken, response.expiresIn);
    } catch (error) {
      // If refresh fails, clear tokens and redirect to login
      authService.clearTokens();
      authService.clearUser();
      window.location.href = '/login';
      throw error;
    }
  }

  /**
   * Upload a file with progress tracking
   */
  async uploadFile<T>(
    endpoint: string,
    file: File,
    additionalData?: Record<string, string>,
    onProgress?: (progress: number) => void,
    fieldName: string = 'file'
  ): Promise<T> {
    const url = `${this.baseURL}${endpoint}`;
    const formData = new FormData();
    formData.append(fieldName, file);

    if (additionalData) {
      Object.entries(additionalData).forEach(([key, value]) => {
        formData.append(key, value);
      });
    }

    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();

      // Track upload progress
      xhr.upload.addEventListener('progress', (event) => {
        if (event.lengthComputable && onProgress) {
          const progress = (event.loaded / event.total) * 100;
          onProgress(progress);
        }
      });

      // Handle completion
      xhr.addEventListener('load', async () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          try {
            const response = JSON.parse(xhr.responseText);
            resolve(response);
          } catch (error) {
            reject(new Error('Invalid response format'));
          }
        } else {
          try {
            const errorResponse = JSON.parse(xhr.responseText);
            const error = new Error(errorResponse.userFriendlyMessage || errorResponse.message) as any;
            error.status = xhr.status;
            error.errorCode = errorResponse.errorCode;
            error.userFriendlyMessage = errorResponse.userFriendlyMessage;
            error.suggestedActions = errorResponse.suggestedActions;
            reject(error);
          } catch {
            reject(new Error(`Upload failed with status ${xhr.status}`));
          }
        }
      });

      // Handle errors
      xhr.addEventListener('error', () => {
        reject(new Error('Network error during upload'));
      });

      xhr.addEventListener('abort', () => {
        reject(new Error('Upload cancelled'));
      });

      // Send request
      xhr.open('POST', url);

      // Add auth token
      this.getValidToken().then(token => {
        if (token) {
          xhr.setRequestHeader('Authorization', `Bearer ${token}`);
        }
        xhr.send(formData);
      }).catch(reject);
    });
  }

  /**
   * Helper method for GET requests
   */
  async get<T>(endpoint: string, config?: RequestConfig): Promise<T> {
    const response = await this.request<T>(endpoint, {
      ...config,
      method: 'GET'
    });
    return response.data;
  }

  /**
   * Helper method for POST requests
   */
  async post<T>(endpoint: string, data?: any, config?: RequestConfig): Promise<T> {
    const response = await this.request<T>(endpoint, {
      ...config,
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...config?.headers
      },
      body: data ? JSON.stringify(data) : undefined
    });
    return response.data;
  }

  /**
   * Helper method for PUT requests
   */
  async put<T>(endpoint: string, data?: any, config?: RequestConfig): Promise<T> {
    const response = await this.request<T>(endpoint, {
      ...config,
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        ...config?.headers
      },
      body: data ? JSON.stringify(data) : undefined
    });
    return response.data;
  }

  /**
   * Helper method for DELETE requests
   */
  async delete<T>(endpoint: string, config?: RequestConfig): Promise<T> {
    const response = await this.request<T>(endpoint, {
      ...config,
      method: 'DELETE'
    });
    return response.data;
  }

  /**
   * Sleep utility for retry delays
   */
  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  /**
   * Set the base URL
   */
  setBaseURL(baseURL: string): void {
    this.baseURL = baseURL;
  }

  /**
   * Get the current base URL
   */
  getBaseURL(): string {
    return this.baseURL;
  }
}

// Export singleton instance
export const apiClient = new ApiClient();

export default apiClient;
