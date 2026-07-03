/**
 * Upload service with progress tracking, resumable uploads, error recovery, and image optimization
 */

import { optimizeImage, needsOptimization } from '@/utils/imageOptimization';
import { getAdaptiveLoadingConfig } from '@/utils/networkSpeed';

export interface UploadOptions {
  onProgress?: (progress: number) => void;
  onError?: (error: Error) => void;
  onComplete?: (result: UploadResult) => void;
  signal?: AbortSignal;
  retryAttempts?: number;
  retryDelay?: number;
  optimizeImages?: boolean;
}

export interface UploadResult {
  fileId: string;
  fileName: string;
  fileSize: number;
  uploadUrl: string;
  s3Key?: string;
}

export interface BatchUploadResult {
  successful: UploadResult[];
  failed: Array<{ file: File; error: Error }>;
  totalFiles: number;
  successCount: number;
  failureCount: number;
}

export interface ResumableUploadState {
  uploadId: string;
  file: File;
  uploadedParts: number[];
  totalParts: number;
  partSize: number;
}

class UploadService {
  private baseUrl: string;
  private resumableUploads: Map<string, ResumableUploadState> = new Map();
  private readonly CHUNK_SIZE = 5 * 1024 * 1024; // 5MB chunks for multipart upload
  private readonly MAX_RETRIES = 3;
  private readonly RETRY_DELAY = 1000; // 1 second

  constructor(baseUrl: string = '/api/v1') {
    this.baseUrl = baseUrl;
  }

  /**
   * Upload a single file with progress tracking
   */
  async uploadFile(
    file: File,
    endpoint: string,
    options: UploadOptions = {}
  ): Promise<UploadResult> {
    const {
      onProgress,
      onError,
      onComplete,
      signal,
      retryAttempts = this.MAX_RETRIES,
      retryDelay = this.RETRY_DELAY,
      optimizeImages = true
    } = options;

    // Optimize image if needed
    let fileToUpload: File | Blob = file;
    if (optimizeImages && file.type.startsWith('image/')) {
      try {
        const adaptiveConfig = getAdaptiveLoadingConfig();
        
        if (needsOptimization(file)) {
          const optimizedBlob = await optimizeImage(file, {
            maxWidth: adaptiveConfig.maxImageWidth,
            maxHeight: adaptiveConfig.maxImageHeight,
            quality: adaptiveConfig.imageQuality,
            format: 'jpeg'
          });
          
          // Create a new File from the optimized blob
          fileToUpload = new File([optimizedBlob], file.name, {
            type: 'image/jpeg',
            lastModified: Date.now()
          });
        }
      } catch (error) {

        // Continue with original file if optimization fails
      }
    }

    let attempt = 0;
    let lastError: Error | null = null;

    while (attempt < retryAttempts) {
      try {
        const result = await this.performUpload(fileToUpload, endpoint, {
          onProgress,
          signal
        });

        onComplete?.(result);
        return result;
      } catch (error) {
        lastError = error instanceof Error ? error : new Error('Upload failed');
        attempt++;

        if (attempt < retryAttempts) {
          // Exponential backoff
          const delay = retryDelay * Math.pow(2, attempt - 1);
          await this.sleep(delay);
        } else {
          onError?.(lastError);
          throw lastError;
        }
      }
    }

    throw lastError || new Error('Upload failed after retries');
  }

  /**
   * Perform the actual upload
   */
  private async performUpload(
    file: File | Blob,
    endpoint: string,
    options: { onProgress?: (progress: number) => void; signal?: AbortSignal }
  ): Promise<UploadResult> {
    const formData = new FormData();
    formData.append('file', file);

    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();

      // Track upload progress
      xhr.upload.addEventListener('progress', (event) => {
        if (event.lengthComputable) {
          const progress = (event.loaded / event.total) * 100;
          options.onProgress?.(progress);
        }
      });

      // Handle completion
      xhr.addEventListener('load', () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          try {
            const result = JSON.parse(xhr.responseText);
            const fileName = file instanceof File ? file.name : 'file';
            const fileSize = file.size;
            resolve({
              fileId: result.fileId || result.id || '',
              fileName,
              fileSize,
              uploadUrl: result.uploadUrl || result.url || '',
              s3Key: result.s3Key || result.key
            });
          } catch (error) {
            reject(new Error('Invalid response format'));
          }
        } else {
          reject(new Error(`Upload failed with status ${xhr.status}`));
        }
      });

      // Handle errors
      xhr.addEventListener('error', () => {
        reject(new Error('Network error during upload'));
      });

      xhr.addEventListener('abort', () => {
        reject(new Error('Upload cancelled'));
      });

      // Handle abort signal
      if (options.signal) {
        options.signal.addEventListener('abort', () => {
          xhr.abort();
        });
      }

      // Send request
      xhr.open('POST', `${this.baseUrl}${endpoint}`);
      
      // Add auth token if available
      const token = this.getAuthToken();
      if (token) {
        xhr.setRequestHeader('Authorization', `Bearer ${token}`);
      }

      xhr.send(formData);
    });
  }

  /**
   * Upload multiple files in batch
   */
  async uploadBatch(
    files: File[],
    endpoint: string,
    options: UploadOptions = {}
  ): Promise<BatchUploadResult> {
    const results: UploadResult[] = [];
    const failures: Array<{ file: File; error: Error }> = [];

    for (const file of files) {
      try {
        const result = await this.uploadFile(file, endpoint, options);
        results.push(result);
      } catch (error) {
        failures.push({
          file,
          error: error instanceof Error ? error : new Error('Upload failed')
        });
      }
    }

    return {
      successful: results,
      failed: failures,
      totalFiles: files.length,
      successCount: results.length,
      failureCount: failures.length
    };
  }

  /**
   * Upload large file with resumable multipart upload
   */
  async uploadLargeFile(
    file: File,
    endpoint: string,
    options: UploadOptions = {}
  ): Promise<UploadResult> {
    const { onProgress, signal } = options;

    // Check if we have a resumable upload in progress
    const resumableKey = `${file.name}-${file.size}`;
    let uploadState = this.resumableUploads.get(resumableKey);

    if (!uploadState) {
      // Initialize new multipart upload
      uploadState = await this.initializeMultipartUpload(file, endpoint);
      this.resumableUploads.set(resumableKey, uploadState);
    }

    try {
      // Upload parts
      const totalParts = Math.ceil(file.size / this.CHUNK_SIZE);
      
      for (let partNumber = 1; partNumber <= totalParts; partNumber++) {
        // Skip already uploaded parts
        if (uploadState.uploadedParts.includes(partNumber)) {
          continue;
        }

        // Check for abort signal
        if (signal?.aborted) {
          throw new Error('Upload cancelled');
        }

        // Upload part
        const start = (partNumber - 1) * this.CHUNK_SIZE;
        const end = Math.min(start + this.CHUNK_SIZE, file.size);
        const chunk = file.slice(start, end);

        await this.uploadPart(
          chunk,
          uploadState.uploadId,
          partNumber,
          endpoint
        );

        uploadState.uploadedParts.push(partNumber);

        // Update progress
        const progress = (uploadState.uploadedParts.length / totalParts) * 100;
        onProgress?.(progress);
      }

      // Complete multipart upload
      const result = await this.completeMultipartUpload(
        uploadState.uploadId,
        endpoint
      );

      // Clean up
      this.resumableUploads.delete(resumableKey);

      return result;
    } catch (error) {
      // Keep upload state for resume
      throw error;
    }
  }

  /**
   * Initialize multipart upload
   */
  private async initializeMultipartUpload(
    file: File,
    endpoint: string
  ): Promise<ResumableUploadState> {
    const response = await fetch(`${this.baseUrl}${endpoint}/multipart/init`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getAuthToken()}`
      },
      body: JSON.stringify({
        fileName: file.name,
        fileSize: file.size,
        contentType: file.type
      })
    });

    if (!response.ok) {
      throw new Error('Failed to initialize multipart upload');
    }

    const data = await response.json();

    return {
      uploadId: data.uploadId,
      file,
      uploadedParts: [],
      totalParts: Math.ceil(file.size / this.CHUNK_SIZE),
      partSize: this.CHUNK_SIZE
    };
  }

  /**
   * Upload a single part
   */
  private async uploadPart(
    chunk: Blob,
    uploadId: string,
    partNumber: number,
    endpoint: string
  ): Promise<void> {
    const formData = new FormData();
    formData.append('chunk', chunk);
    formData.append('uploadId', uploadId);
    formData.append('partNumber', partNumber.toString());

    const response = await fetch(`${this.baseUrl}${endpoint}/multipart/part`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`
      },
      body: formData
    });

    if (!response.ok) {
      throw new Error(`Failed to upload part ${partNumber}`);
    }
  }

  /**
   * Complete multipart upload
   */
  private async completeMultipartUpload(
    uploadId: string,
    endpoint: string
  ): Promise<UploadResult> {
    const response = await fetch(
      `${this.baseUrl}${endpoint}/multipart/complete`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getAuthToken()}`
        },
        body: JSON.stringify({ uploadId })
      }
    );

    if (!response.ok) {
      throw new Error('Failed to complete multipart upload');
    }

    const data = await response.json();
    return {
      fileId: data.fileId || data.id || '',
      fileName: data.fileName || '',
      fileSize: data.fileSize || 0,
      uploadUrl: data.uploadUrl || data.url || '',
      s3Key: data.s3Key || data.key
    };
  }

  /**
   * Cancel an upload
   */
  cancelUpload(file: File): void {
    const resumableKey = `${file.name}-${file.size}`;
    this.resumableUploads.delete(resumableKey);
  }

  /**
   * Get auth token from storage
   */
  private getAuthToken(): string | null {
    // Try to get token from localStorage or sessionStorage
    return localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
  }

  /**
   * Sleep utility for retry delays
   */
  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

// Export singleton instance
export const uploadService = new UploadService();

export default uploadService;
