/**
 * File validation utilities for upload system
 */

import { 
  validateFileType, 
  validateFileSize, 
  hasSuspiciousExtension, 
  sanitizeFilename,
  MAX_FILE_SIZES 
} from './security';
import { performSecurityScan } from './malwareScanning';

export interface FileValidationResult {
  isValid: boolean;
  error?: string;
  errorCode?: string;
  securityIssues?: string[];
}

export interface FileValidationOptions {
  maxSize?: number; // in bytes
  allowedTypes?: string[];
  allowedExtensions?: string[];
}

// Default validation options
export const DEFAULT_IMAGE_OPTIONS: FileValidationOptions = {
  maxSize: 10 * 1024 * 1024, // 10MB
  allowedTypes: ['image/jpeg', 'image/jpg', 'image/png'],
  allowedExtensions: ['.jpg', '.jpeg', '.png']
};

export const DEFAULT_DOCUMENT_OPTIONS: FileValidationOptions = {
  maxSize: 10 * 1024 * 1024, // 10MB
  allowedTypes: ['image/jpeg', 'image/jpg', 'image/png', 'application/pdf', 'text/plain'],
  allowedExtensions: ['.jpg', '.jpeg', '.png', '.pdf', '.txt']
};

export const DEFAULT_AUDIO_OPTIONS: FileValidationOptions = {
  maxSize: 10 * 1024 * 1024, // 10MB
  allowedTypes: ['audio/mpeg', 'audio/mp3', 'audio/wav', 'audio/ogg'],
  allowedExtensions: ['.mp3', '.wav', '.ogg']
};

/**
 * Validate a file against specified options with security checks
 */
export async function validateFile(
  file: File,
  options: FileValidationOptions = DEFAULT_IMAGE_OPTIONS
): Promise<FileValidationResult> {
  // Check if file exists
  if (!file) {
    return {
      isValid: false,
      error: 'No file provided',
      errorCode: 'FILE_REQUIRED'
    };
  }

  // Security check: Suspicious file extension
  if (hasSuspiciousExtension(file.name)) {
    return {
      isValid: false,
      error: 'File type not allowed for security reasons',
      errorCode: 'FILE_TYPE_SUSPICIOUS'
    };
  }

  // Check file size
  const maxSize = options.maxSize || 10 * 1024 * 1024;
  if (file.size > maxSize) {
    return {
      isValid: false,
      error: `File size (${formatFileSize(file.size)}) exceeds maximum allowed size (${formatFileSize(maxSize)})`,
      errorCode: 'FILE_TOO_LARGE'
    };
  }

  // Check file type
  if (options.allowedTypes && options.allowedTypes.length > 0) {
    const fileType = (file.type || '').toLowerCase();
    const isTypeAllowed = options.allowedTypes.some(type => 
      fileType === type.toLowerCase() || fileType.startsWith(type.toLowerCase())
    );
    
    if (!isTypeAllowed) {
      return {
        isValid: false,
        error: `File type "${file.type || 'unknown'}" is not allowed. Allowed types: ${options.allowedTypes.join(', ')}`,
        errorCode: 'FILE_TYPE_INVALID'
      };
    }
  }

  // Check file extension
  if (options.allowedExtensions && options.allowedExtensions.length > 0) {
    const fileName = (file.name || '').toLowerCase();
    const hasValidExtension = options.allowedExtensions.some(ext => 
      fileName.endsWith(ext.toLowerCase())
    );
    
    if (!hasValidExtension) {
      return {
        isValid: false,
        error: `File extension is not allowed. Allowed extensions: ${options.allowedExtensions.join(', ')}`,
        errorCode: 'FILE_EXTENSION_INVALID'
      };
    }
  }

  // Perform security scan
  const securityScan = await performSecurityScan(file);
  if (!securityScan.isSafe) {
    return {
      isValid: false,
      error: 'File failed security scan',
      errorCode: 'FILE_SECURITY_SCAN_FAILED',
      securityIssues: securityScan.issues
    };
  }

  return { isValid: true };
}

/**
 * Validate multiple files with security checks
 */
export async function validateFiles(
  files: File[],
  options: FileValidationOptions = DEFAULT_IMAGE_OPTIONS,
  maxFiles?: number
): Promise<FileValidationResult> {
  if (!files || files.length === 0) {
    return {
      isValid: false,
      error: 'No files provided',
      errorCode: 'FILES_REQUIRED'
    };
  }

  if (maxFiles && files.length > maxFiles) {
    return {
      isValid: false,
      error: `Too many files. Maximum ${maxFiles} files allowed`,
      errorCode: 'TOO_MANY_FILES'
    };
  }

  // Validate each file
  for (let i = 0; i < files.length; i++) {
    const result = await validateFile(files[i], options);
    if (!result.isValid) {
      return {
        isValid: false,
        error: `File ${i + 1} (${files[i].name}): ${result.error}`,
        errorCode: result.errorCode,
        securityIssues: result.securityIssues
      };
    }
  }

  return { isValid: true };
}

/**
 * Format file size for display
 */
export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 Bytes';
  
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  
  return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
}

/**
 * Check if file is an image
 */
export function isImageFile(file: File): boolean {
  return file.type.startsWith('image/');
}

/**
 * Check if file is a document
 */
export function isDocumentFile(file: File): boolean {
  return file.type.includes('pdf') || file.type.includes('document');
}

/**
 * Check if file is audio
 */
export function isAudioFile(file: File): boolean {
  return file.type.startsWith('audio/');
}

/**
 * Get file extension
 */
export function getFileExtension(fileName: string): string {
  const parts = fileName.split('.');
  return parts.length > 1 ? `.${parts[parts.length - 1].toLowerCase()}` : '';
}

/**
 * Sanitize file name for secure storage
 */
export function sanitizeFileName(fileName: string): string {
  // Use security utility for comprehensive sanitization
  return sanitizeFilename(fileName);
}
