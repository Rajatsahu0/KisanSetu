/**
 * Quality Grading Service
 * Handles API calls for produce quality grading and history
 */

import { apiClient } from './apiClient';
import type {
  GradingResult,
  BatchGradingResult,
  QualityGrade
} from '@/types';

export interface GradingRequest {
  produceType: string;
  location: string;
}

export interface GradingRecord {
  recordId: string;
  produceType: string;
  grade: QualityGrade;
  certifiedPrice: number;
  timestamp: string;
  imageUrl?: string;
  location: string;
}

class QualityGradingService {
  /**
   * Grade a single produce image
   */
  async gradeProduct(
    image: File,
    produceType: string,
    location: string,
    onProgress?: (progress: number) => void
  ): Promise<GradingResult> {
    const formData = new FormData();
    formData.append('image', image);
    formData.append('produceType', produceType);
    formData.append('location', location);

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
      xhr.addEventListener('load', () => {
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
            const error = new Error(errorResponse.userFriendlyMessage || errorResponse.message || 'Grading failed') as any;
            error.errorCode = errorResponse.errorCode;
            error.suggestedActions = errorResponse.suggestedActions;
            reject(error);
          } catch {
            reject(new Error(`Grading failed with status ${xhr.status}`));
          }
        }
      });

      // Handle errors
      xhr.addEventListener('error', () => {
        reject(new Error('Network error during grading'));
      });

      xhr.addEventListener('abort', () => {
        reject(new Error('Grading cancelled'));
      });

      // Send request
      const baseURL = apiClient.getBaseURL();
      xhr.open('POST', `${baseURL}/api/v1/QualityGrading/grade`);

      // Add auth token
      import('./authService').then(({ authService }) => {
        const token = authService.getAccessToken();
        if (token) {
          xhr.setRequestHeader('Authorization', `Bearer ${token}`);
        }
        xhr.send(formData);
      });
    });
  }

  /**
   * Grade multiple produce images in batch
   */
  async gradeBatch(
    images: File[],
    produceType: string,
    location: string,
    onProgress?: (progress: number) => void
  ): Promise<BatchGradingResult> {
    const formData = new FormData();
    
    images.forEach((image) => {
      formData.append('images', image);
    });
    formData.append('produceType', produceType);
    formData.append('location', location);

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
      xhr.addEventListener('load', () => {
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
            const error = new Error(errorResponse.userFriendlyMessage || errorResponse.message || 'Batch grading failed') as any;
            error.errorCode = errorResponse.errorCode;
            error.suggestedActions = errorResponse.suggestedActions;
            reject(error);
          } catch {
            reject(new Error(`Batch grading failed with status ${xhr.status}`));
          }
        }
      });

      // Handle errors
      xhr.addEventListener('error', () => {
        reject(new Error('Network error during batch grading'));
      });

      xhr.addEventListener('abort', () => {
        reject(new Error('Batch grading cancelled'));
      });

      // Send request
      const baseURL = apiClient.getBaseURL();
      xhr.open('POST', `${baseURL}/api/v1/QualityGrading/grade-batch`);

      // Add auth token
      import('./authService').then(({ authService }) => {
        const token = authService.getAccessToken();
        if (token) {
          xhr.setRequestHeader('Authorization', `Bearer ${token}`);
        }
        xhr.send(formData);
      });
    });
  }

  /**
   * Get grading history
   */
  async getGradingHistory(
    startDate?: Date,
    endDate?: Date,
    produceType?: string
  ): Promise<GradingRecord[]> {
    const params = new URLSearchParams();
    
    if (startDate) {
      params.append('startDate', startDate.toISOString());
    }
    if (endDate) {
      params.append('endDate', endDate.toISOString());
    }
    if (produceType) {
      params.append('produceType', produceType);
    }

    const queryString = params.toString();
    const endpoint = `/api/v1/QualityGrading/history${queryString ? `?${queryString}` : ''}`;
    
    return apiClient.get<GradingRecord[]>(endpoint);
  }

  /**
   * Save a grading result locally
   */
  saveGradingLocally(result: GradingResult, imageDataUrl?: string): void {
    try {
      const savedGradings = this.getSavedGradings();
      savedGradings.push({
        ...result,
        imageDataUrl,
        savedAt: new Date().toISOString()
      });
      localStorage.setItem('savedGradings', JSON.stringify(savedGradings));
    } catch (error) {
      console.error('Failed to save grading locally:', error);
      throw new Error('Failed to save grading');
    }
  }

  /**
   * Get saved gradings from local storage
   */
  getSavedGradings(): (GradingResult & { savedAt: string; imageDataUrl?: string })[] {
    try {
      const saved = localStorage.getItem('savedGradings');
      return saved ? JSON.parse(saved) : [];
    } catch (error) {
      console.error('Failed to retrieve saved gradings:', error);
      return [];
    }
  }

  /**
   * Delete a saved grading
   */
  deleteSavedGrading(recordId: string): void {
    try {
      const savedGradings = this.getSavedGradings();
      const filtered = savedGradings.filter(g => g.recordId !== recordId);
      localStorage.setItem('savedGradings', JSON.stringify(filtered));
    } catch (error) {
      console.error('Failed to delete grading:', error);
      throw new Error('Failed to delete grading');
    }
  }
}

// Export singleton instance
export const qualityGradingService = new QualityGradingService();

export default qualityGradingService;
