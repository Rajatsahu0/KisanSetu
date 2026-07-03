/**
 * Security utilities for data protection and validation
 */

/**
 * Content Security Policy configuration
 */
export const CSP_DIRECTIVES = {
  'default-src': ["'self'"],
  'script-src': ["'self'", "'unsafe-inline'", "'unsafe-eval'", 'https://www.google.com', 'https://www.gstatic.com', 'https://*.my.site.com'],
  'style-src': ["'self'", "'unsafe-inline'", 'https://*.my.site.com'],
  'img-src': ["'self'", 'data:', 'https:', 'blob:'],
  'font-src': ["'self'", 'data:'],
  'connect-src': [
    "'self'",
    'https://*.googleapis.com',
    'https://*.firebaseapp.com',
    'https://*.firebase.google.com',
    'https://securetoken.google.com',
    'https://identitytoolkit.googleapis.com',
    'https://*.cloudfunctions.net',
    'https://www.google.com',
    'https://*.my.site.com',
    'https://*.salesforce-scrt.com',
    import.meta.env.VITE_API_BASE_URL || '',
  ],
  'media-src': ["'self'", 'blob:', 'https://*.googleapis.com'],
  'object-src': ["'none'"],
  'base-uri': ["'self'"],
  'form-action': ["'self'"],
  'frame-src': ['https://www.google.com', 'https://*.firebaseapp.com', 'https://*.my.site.com', 'https://*.salesforce-scrt.com'],
  'frame-ancestors': ["'none'"],
  'upgrade-insecure-requests': [],
};

/**
 * Generate CSP header string
 */
export function generateCSPHeader(): string {
  return Object.entries(CSP_DIRECTIVES)
    .map(([directive, values]) => {
      if (values.length === 0) return directive;
      return `${directive} ${values.filter(Boolean).join(' ')}`;
    })
    .join('; ');
}

/**
 * Allowed file types for uploads
 */
export const ALLOWED_FILE_TYPES = {
  images: ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'],
  documents: ['application/pdf'],
  audio: ['audio/mpeg', 'audio/mp3', 'audio/wav', 'audio/ogg', 'audio/webm'],
};

/**
 * Maximum file sizes (in bytes)
 */
export const MAX_FILE_SIZES = {
  image: 10 * 1024 * 1024, // 10MB
  document: 10 * 1024 * 1024, // 10MB
  audio: 5 * 1024 * 1024, // 5MB
};

/**
 * Validate file type against allowed types
 */
export function validateFileType(file: File, category: keyof typeof ALLOWED_FILE_TYPES): boolean {
  const allowedTypes = ALLOWED_FILE_TYPES[category];
  return allowedTypes.includes(file.type);
}

/**
 * Validate file size against maximum allowed
 */
export function validateFileSize(file: File, category: keyof typeof MAX_FILE_SIZES): boolean {
  const maxSize = MAX_FILE_SIZES[category];
  return file.size <= maxSize;
}

/**
 * Check for potentially malicious file extensions
 */
export function hasSuspiciousExtension(filename: string): boolean {
  const suspiciousExtensions = [
    '.exe', '.bat', '.cmd', '.com', '.pif', '.scr', '.vbs', '.js',
    '.jar', '.zip', '.rar', '.7z', '.tar', '.gz', '.sh', '.ps1',
  ];
  
  const lowerFilename = filename.toLowerCase();
  return suspiciousExtensions.some(ext => lowerFilename.endsWith(ext));
}

/**
 * Sanitize filename to prevent path traversal attacks
 */
export function sanitizeFilename(filename: string): string {
  // Remove path separators and special characters
  return filename
    .replace(/[\/\\]/g, '')
    .replace(/[^\w\s.-]/g, '')
    .replace(/\s+/g, '_')
    .substring(0, 255); // Limit filename length
}

/**
 * Mask sensitive data for display
 */
export function maskPhoneNumber(phoneNumber: string): string {
  if (!phoneNumber || phoneNumber.length < 4) return '****';
  return phoneNumber.slice(0, 2) + '****' + phoneNumber.slice(-2);
}

/**
 * Mask email address
 */
export function maskEmail(email: string): string {
  if (!email || !email.includes('@')) return '****@****.***';
  const [username, domain] = email.split('@');
  const maskedUsername = username.length > 2 
    ? username.slice(0, 2) + '****'
    : '****';
  return `${maskedUsername}@${domain}`;
}

/**
 * Mask Aadhaar number (Indian ID)
 */
export function maskAadhaar(aadhaar: string): string {
  if (!aadhaar || aadhaar.length < 4) return '****';
  return '****-****-' + aadhaar.slice(-4);
}

/**
 * Mask bank account number
 */
export function maskBankAccount(accountNumber: string): string {
  if (!accountNumber || accountNumber.length < 4) return '****';
  return '****' + accountNumber.slice(-4);
}

/**
 * Validate that data doesn't contain sensitive patterns
 */
export function containsSensitiveData(text: string): boolean {
  // Check for patterns that might be sensitive
  const sensitivePatterns = [
    /\b\d{12}\b/, // Aadhaar-like numbers
    /\b\d{16}\b/, // Credit card-like numbers
    /\b\d{10,15}\b/, // Bank account-like numbers
    /password/i,
    /secret/i,
    /token/i,
    /api[_-]?key/i,
  ];
  
  return sensitivePatterns.some(pattern => pattern.test(text));
}

/**
 * Secure data storage key prefix
 */
const STORAGE_PREFIX = 'kisan_secure_';

/**
 * Check if storage is available
 */
function isStorageAvailable(type: 'localStorage' | 'sessionStorage'): boolean {
  try {
    const storage = window[type];
    const test = '__storage_test__';
    storage.setItem(test, test);
    storage.removeItem(test);
    return true;
  } catch (e) {
    return false;
  }
}

/**
 * Securely store non-sensitive data in localStorage
 * Note: Never store sensitive data like tokens in localStorage
 */
export function secureLocalStorage() {
  const isAvailable = isStorageAvailable('localStorage');
  
  return {
    setItem(key: string, value: string): void {
      if (!isAvailable) {

        return;
      }
      
      // Check if value contains sensitive data
      if (containsSensitiveData(value)) {
        console.error('Attempted to store sensitive data in localStorage');
        return;
      }
      
      try {
        localStorage.setItem(STORAGE_PREFIX + key, value);
      } catch (e) {
        console.error('Failed to store data:', e);
      }
    },
    
    getItem(key: string): string | null {
      if (!isAvailable) return null;
      
      try {
        return localStorage.getItem(STORAGE_PREFIX + key);
      } catch (e) {
        console.error('Failed to retrieve data:', e);
        return null;
      }
    },
    
    removeItem(key: string): void {
      if (!isAvailable) return;
      
      try {
        localStorage.removeItem(STORAGE_PREFIX + key);
      } catch (e) {
        console.error('Failed to remove data:', e);
      }
    },
    
    clear(): void {
      if (!isAvailable) return;
      
      try {
        // Only clear items with our prefix
        const keys = Object.keys(localStorage);
        keys.forEach(key => {
          if (key.startsWith(STORAGE_PREFIX)) {
            localStorage.removeItem(key);
          }
        });
      } catch (e) {
        console.error('Failed to clear storage:', e);
      }
    },
  };
}

/**
 * XSS prevention: Sanitize HTML content
 */
export function sanitizeHTML(html: string): string {
  const div = document.createElement('div');
  div.textContent = html;
  return div.innerHTML;
}

/**
 * Validate URL to prevent open redirect vulnerabilities
 */
export function isValidRedirectURL(url: string): boolean {
  try {
    const parsedURL = new URL(url, window.location.origin);
    // Only allow same-origin redirects
    return parsedURL.origin === window.location.origin;
  } catch {
    return false;
  }
}

/**
 * Generate a secure random string for CSRF tokens
 */
export function generateSecureToken(length: number = 32): string {
  const array = new Uint8Array(length);
  crypto.getRandomValues(array);
  return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
}

/**
 * Rate limiting helper for client-side operations
 */
export class RateLimiter {
  private attempts: Map<string, number[]> = new Map();
  
  constructor(
    private maxAttempts: number = 5,
    private windowMs: number = 60000 // 1 minute
  ) {}
  
  isAllowed(key: string): boolean {
    const now = Date.now();
    const attempts = this.attempts.get(key) || [];
    
    // Remove old attempts outside the window
    const recentAttempts = attempts.filter(time => now - time < this.windowMs);
    
    if (recentAttempts.length >= this.maxAttempts) {
      return false;
    }
    
    recentAttempts.push(now);
    this.attempts.set(key, recentAttempts);
    return true;
  }
  
  reset(key: string): void {
    this.attempts.delete(key);
  }
  
  clear(): void {
    this.attempts.clear();
  }
}

/**
 * Security headers to check in responses
 */
export const EXPECTED_SECURITY_HEADERS = [
  'X-Content-Type-Options',
  'X-Frame-Options',
  'X-XSS-Protection',
  'Strict-Transport-Security',
];

/**
 * Validate security headers in API responses
 */
export function validateSecurityHeaders(headers: Headers): string[] {
  const missingHeaders: string[] = [];
  
  EXPECTED_SECURITY_HEADERS.forEach(header => {
    if (!headers.has(header)) {
      missingHeaders.push(header);
    }
  });
  
  return missingHeaders;
}
