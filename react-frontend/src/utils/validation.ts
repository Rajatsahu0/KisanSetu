/**
 * Validation utilities for forms and file uploads
 */

export interface ValidationResult {
  isValid: boolean;
  error?: string;
}

/**
 * Validate phone number (Indian format)
 */
export function validatePhoneNumber(phoneNumber: string): ValidationResult {
  const cleaned = phoneNumber.replace(/\D/g, '');
  
  if (cleaned.length !== 10) {
    return { isValid: false, error: 'Phone number must be 10 digits' };
  }
  
  if (!cleaned.match(/^[6-9]\d{9}$/)) {
    return { isValid: false, error: 'Invalid Indian phone number format' };
  }
  
  return { isValid: true };
}

/**
 * Validate email address
 */
export function validateEmail(email: string): ValidationResult {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  
  if (!emailRegex.test(email)) {
    return { isValid: false, error: 'Invalid email format' };
  }
  
  return { isValid: true };
}

/**
 * Validate password strength
 */
export function validatePassword(password: string): ValidationResult {
  if (password.length < 8) {
    return { isValid: false, error: 'Password must be at least 8 characters long' };
  }
  
  if (!/(?=.*[a-z])/.test(password)) {
    return { isValid: false, error: 'Password must contain at least one lowercase letter' };
  }
  
  if (!/(?=.*[A-Z])/.test(password)) {
    return { isValid: false, error: 'Password must contain at least one uppercase letter' };
  }
  
  if (!/(?=.*\d)/.test(password)) {
    return { isValid: false, error: 'Password must contain at least one number' };
  }
  
  return { isValid: true };
}

/**
 * Validate file type and size
 */
export interface FileValidationOptions {
  allowedTypes: string[];
  maxSize: number; // in bytes
  minSize?: number; // in bytes
}

export function validateFile(file: File, options: FileValidationOptions): ValidationResult {
  // Check file type
  if (!options.allowedTypes.includes(file.type)) {
    return { 
      isValid: false, 
      error: `File type ${file.type} is not allowed. Allowed types: ${options.allowedTypes.join(', ')}` 
    };
  }
  
  // Check file size
  if (file.size > options.maxSize) {
    return { 
      isValid: false, 
      error: `File size (${formatFileSize(file.size)}) exceeds maximum allowed size (${formatFileSize(options.maxSize)})` 
    };
  }
  
  if (options.minSize && file.size < options.minSize) {
    return { 
      isValid: false, 
      error: `File size (${formatFileSize(file.size)}) is below minimum required size (${formatFileSize(options.minSize)})` 
    };
  }
  
  return { isValid: true };
}

/**
 * Validate image file specifically
 */
export function validateImageFile(file: File): ValidationResult {
  const imageTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
  const maxSize = 10 * 1024 * 1024; // 10MB
  
  return validateFile(file, {
    allowedTypes: imageTypes,
    maxSize,
  });
}

/**
 * Validate document file (PDF, DOC, etc.)
 */
export function validateDocumentFile(file: File): ValidationResult {
  const documentTypes = [
    'application/pdf',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    'image/jpeg',
    'image/jpg',
    'image/png'
  ];
  const maxSize = 10 * 1024 * 1024; // 10MB
  
  return validateFile(file, {
    allowedTypes: documentTypes,
    maxSize,
  });
}

/**
 * Validate audio file
 */
export function validateAudioFile(file: File): ValidationResult {
  const audioTypes = ['audio/mp3', 'audio/wav', 'audio/ogg', 'audio/mpeg', 'audio/webm'];
  const maxSize = 25 * 1024 * 1024; // 25MB
  const minSize = 1024; // 1KB
  
  return validateFile(file, {
    allowedTypes: audioTypes,
    maxSize,
    minSize,
  });
}

/**
 * Helper function to format file size (imported from format.ts)
 */
function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 Bytes';
  
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

/**
 * Validate required field
 */
export function validateRequired(value: any, fieldName: string): ValidationResult {
  if (value === null || value === undefined || value === '') {
    return { isValid: false, error: `${fieldName} is required` };
  }
  
  return { isValid: true };
}

/**
 * Validate numeric range
 */
export function validateNumericRange(
  value: number, 
  min: number, 
  max: number, 
  fieldName: string
): ValidationResult {
  if (value < min || value > max) {
    return { 
      isValid: false, 
      error: `${fieldName} must be between ${min} and ${max}` 
    };
  }
  
  return { isValid: true };
}