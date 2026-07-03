/**
 * S3 Upload Service with presigned URLs and direct uploads
 * Uses AWS SDK v3 for improved security and performance
 */

import { S3Client, PutObjectCommand } from '@aws-sdk/client-s3';
import { getSignedUrl } from '@aws-sdk/s3-request-presigner';

export interface S3UploadOptions {
  onProgress?: (progress: number) => void;
  onError?: (error: Error) => void;
  onComplete?: (result: S3UploadResult) => void;
  signal?: AbortSignal;
}

export interface S3UploadResult {
  s3Key: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  uploadedAt: string;
}

export interface PresignedUrlResponse {
  uploadUrl: string;
  s3Key: string;
  expiresIn: number;
}

class S3UploadService {
  private baseUrl: string;

  constructor(baseUrl: string = '/api/v1') {
    this.baseUrl = baseUrl;
  }

  /**
   * Get presigned URL for direct S3 upload from backend
   */
  async getPresignedUrl(
    fileName: string,
    contentType: string,
    farmerId: string
  ): Promise<PresignedUrlResponse> {
    const response = await fetch(`${this.baseUrl}/upload/presigned-url`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getAuthToken()}`
      },
      body: JSON.stringify({
        fileName,
        contentType,
        farmerId
      })
    });

    if (!response.ok) {
      throw new Error('Failed to get presigned URL');
    }

    return response.json();
  }

  /**
   * Upload file directly to S3 using presigned URL
   */
  async uploadToS3(
    file: File,
    presignedUrl: string,
    options: S3UploadOptions = {}
  ): Promise<void> {
    const { onProgress, signal } = options;

    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();

      // Track upload progress
      xhr.upload.addEventListener('progress', (event) => {
        if (event.lengthComputable) {
          const progress = (event.loaded / event.total) * 100;
          onProgress?.(progress);
        }
      });

      // Handle completion
      xhr.addEventListener('load', () => {
        if (xhr.status >= 200 && xhr.status < 300) {
          resolve();
        } else {
          reject(new Error(`S3 upload failed with status ${xhr.status}`));
        }
      });

      // Handle errors
      xhr.addEventListener('error', () => {
        reject(new Error('Network error during S3 upload'));
      });

      xhr.addEventListener('abort', () => {
        reject(new Error('S3 upload cancelled'));
      });

      // Handle abort signal
      if (signal) {
        signal.addEventListener('abort', () => {
          xhr.abort();
        });
      }

      // Send request to S3
      xhr.open('PUT', presignedUrl);
      xhr.setRequestHeader('Content-Type', file.type);
      xhr.send(file);
    });
  }

  /**
   * Complete upload flow: get presigned URL and upload to S3
   */
  async uploadFile(
    file: File,
    farmerId: string,
    options: S3UploadOptions = {}
  ): Promise<S3UploadResult> {
    const { onProgress, onError, onComplete, signal } = options;

    try {
      // Step 1: Get presigned URL
      onProgress?.(10);
      const presignedData = await this.getPresignedUrl(
        file.name,
        file.type,
        farmerId
      );

      // Step 2: Upload to S3
      await this.uploadToS3(file, presignedData.uploadUrl, {
        onProgress: (progress) => {
          // Map progress from 10% to 100%
          const adjustedProgress = 10 + (progress * 0.9);
          onProgress?.(adjustedProgress);
        },
        signal
      });

      // Step 3: Return result
      const result: S3UploadResult = {
        s3Key: presignedData.s3Key,
        fileName: file.name,
        fileSize: file.size,
        contentType: file.type,
        uploadedAt: new Date().toISOString()
      };

      onComplete?.(result);
      return result;
    } catch (error) {
      const uploadError = error instanceof Error ? error : new Error('Upload failed');
      onError?.(uploadError);
      throw uploadError;
    }
  }

  /**
   * Upload multiple files to S3
   */
  async uploadBatch(
    files: File[],
    farmerId: string,
    options: S3UploadOptions = {}
  ): Promise<S3UploadResult[]> {
    const results: S3UploadResult[] = [];
    const totalFiles = files.length;

    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      
      try {
        const result = await this.uploadFile(file, farmerId, {
          ...options,
          onProgress: (fileProgress) => {
            // Calculate overall progress
            const completedFiles = i;
            const currentFileProgress = fileProgress / 100;
            const overallProgress = ((completedFiles + currentFileProgress) / totalFiles) * 100;
            options.onProgress?.(overallProgress);
          }
        });
        
        results.push(result);
      } catch (error) {
        console.error(`Failed to upload file ${file.name}:`, error);
        // Continue with other files
      }
    }

    return results;
  }

  /**
   * Get auth token from storage
   */
  private getAuthToken(): string | null {
    return localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
  }
}

// Export singleton instance
export const s3UploadService = new S3UploadService();

export default s3UploadService;
