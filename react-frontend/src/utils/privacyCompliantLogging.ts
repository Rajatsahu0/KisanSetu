/**
 * Privacy-compliant error logging utilities
 * Ensures sensitive data is not logged while maintaining debugging capability
 */

import { containsSensitiveData } from './security';

/**
 * Log levels
 */
export enum LogLevel {
  DEBUG = 'debug',
  INFO = 'info',
  WARN = 'warn',
  ERROR = 'error',
}

/**
 * Sensitive field patterns to redact
 */
const SENSITIVE_FIELDS = [
  'password',
  'token',
  'accessToken',
  'refreshToken',
  'idToken',
  'secret',
  'apiKey',
  'phoneNumber',
  'email',
  'aadhaar',
  'accountNumber',
  'cardNumber',
  'cvv',
  'pin',
  'otp',
];

/**
 * Check if a field name is sensitive
 */
function isSensitiveField(fieldName: string): boolean {
  const lowerField = fieldName.toLowerCase();
  return SENSITIVE_FIELDS.some(sensitive => 
    lowerField.includes(sensitive.toLowerCase())
  );
}

/**
 * Redact sensitive data from objects
 */
function redactSensitiveData(data: any): any {
  if (data === null || data === undefined) {
    return data;
  }
  
  if (typeof data === 'string') {
    // Check if string contains sensitive patterns
    if (containsSensitiveData(data)) {
      return '[REDACTED]';
    }
    return data;
  }
  
  if (Array.isArray(data)) {
    return data.map(item => redactSensitiveData(item));
  }
  
  if (typeof data === 'object') {
    const redacted: any = {};
    
    for (const [key, value] of Object.entries(data)) {
      if (isSensitiveField(key)) {
        redacted[key] = '[REDACTED]';
      } else {
        redacted[key] = redactSensitiveData(value);
      }
    }
    
    return redacted;
  }
  
  return data;
}

/**
 * Sanitize error for logging
 */
function sanitizeError(error: any): any {
  if (error instanceof Error) {
    return {
      name: error.name,
      message: error.message,
      stack: import.meta.env.DEV ? error.stack : undefined,
    };
  }
  
  return redactSensitiveData(error);
}

/**
 * Log entry structure
 */
interface LogEntry {
  timestamp: string;
  level: LogLevel;
  message: string;
  data?: any;
  error?: any;
  context?: {
    url?: string;
    userAgent?: string;
    userId?: string;
  };
}

/**
 * Privacy-compliant logger
 */
export class PrivacyLogger {
  private logs: LogEntry[] = [];
  private maxLogs: number = 100;
  
  constructor(
    private enableConsole: boolean = true,
    private enableStorage: boolean = false
  ) {}
  
  private createLogEntry(
    level: LogLevel,
    message: string,
    data?: any,
    error?: any
  ): LogEntry {
    return {
      timestamp: new Date().toISOString(),
      level,
      message,
      data: data ? redactSensitiveData(data) : undefined,
      error: error ? sanitizeError(error) : undefined,
      context: {
        url: window.location.pathname,
        userAgent: navigator.userAgent,
      },
    };
  }
  
  private log(entry: LogEntry): void {
    // Add to in-memory logs
    this.logs.push(entry);
    
    // Maintain max logs limit
    if (this.logs.length > this.maxLogs) {
      this.logs.shift();
    }
    
    // Console output
    if (this.enableConsole) {
      const consoleMethod = console[entry.level] || console.log;
      consoleMethod(
        `[${entry.timestamp}] ${entry.level.toUpperCase()}: ${entry.message}`,
        entry.data || '',
        entry.error || ''
      );
    }
    
    // Storage (optional, for debugging)
    if (this.enableStorage && import.meta.env.DEV) {
      try {
        const storedLogs = JSON.parse(
          sessionStorage.getItem('app_logs') || '[]'
        );
        storedLogs.push(entry);
        
        // Keep only last 50 logs in storage
        if (storedLogs.length > 50) {
          storedLogs.shift();
        }
        
        sessionStorage.setItem('app_logs', JSON.stringify(storedLogs));
      } catch (e) {
        // Ignore storage errors
      }
    }
  }
  
  debug(message: string, data?: any): void {
    if (import.meta.env.DEV) {
      this.log(this.createLogEntry(LogLevel.DEBUG, message, data));
    }
  }
  
  info(message: string, data?: any): void {
    this.log(this.createLogEntry(LogLevel.INFO, message, data));
  }
  
  warn(message: string, data?: any): void {
    this.log(this.createLogEntry(LogLevel.WARN, message, data));
  }
  
  error(message: string, error?: any, data?: any): void {
    this.log(this.createLogEntry(LogLevel.ERROR, message, data, error));
  }
  
  getLogs(): LogEntry[] {
    return [...this.logs];
  }
  
  clearLogs(): void {
    this.logs = [];
    if (this.enableStorage) {
      try {
        sessionStorage.removeItem('app_logs');
      } catch (e) {
        // Ignore storage errors
      }
    }
  }
  
  exportLogs(): string {
    return JSON.stringify(this.logs, null, 2);
  }
}

/**
 * Global logger instance
 */
export const logger = new PrivacyLogger(
  true, // Enable console in all environments
  import.meta.env.DEV // Enable storage only in development
);

/**
 * Log API errors with privacy compliance
 */
export function logApiError(
  endpoint: string,
  error: any,
  requestData?: any
): void {
  logger.error(
    `API Error: ${endpoint}`,
    error,
    {
      endpoint,
      requestData: requestData ? redactSensitiveData(requestData) : undefined,
    }
  );
}

/**
 * Log user actions (without sensitive data)
 */
export function logUserAction(
  action: string,
  details?: Record<string, any>
): void {
  logger.info(
    `User Action: ${action}`,
    details ? redactSensitiveData(details) : undefined
  );
}

/**
 * Log performance metrics
 */
export function logPerformance(
  metric: string,
  value: number,
  unit: string = 'ms'
): void {
  logger.debug(`Performance: ${metric}`, { value, unit });
}

/**
 * Log security events
 */
export function logSecurityEvent(
  event: string,
  severity: 'low' | 'medium' | 'high',
  details?: Record<string, any>
): void {
  logger.warn(
    `Security Event: ${event}`,
    {
      severity,
      details: details ? redactSensitiveData(details) : undefined,
    }
  );
}

/**
 * Create error report for support (privacy-compliant)
 */
export function createErrorReport(): string {
  const logs = logger.getLogs();
  const recentErrors = logs.filter(log => log.level === LogLevel.ERROR);
  
  return JSON.stringify({
    timestamp: new Date().toISOString(),
    userAgent: navigator.userAgent,
    url: window.location.href,
    errors: recentErrors.slice(-10), // Last 10 errors
    environment: import.meta.env.MODE,
  }, null, 2);
}
