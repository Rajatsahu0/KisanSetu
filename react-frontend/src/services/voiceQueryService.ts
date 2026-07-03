/**
 * Voice Query Service for processing voice queries and managing dialects
 */

import { apiClient } from './apiClient';
import { authService } from './authService';
import { VoiceQueryResponse, Dialect } from '@/types';

export interface VoiceQueryHistoryItem {
  queryId: string;
  farmerId: string;
  transcription: string;
  responseText: string;
  dialect: string;
  confidence: number;
  audioS3Key: string;
  responseAudioS3Key: string;
  timestamp: string;
  isFavorite: boolean;
  prices: any[];
}

class VoiceQueryService {
  /**
   * Process a voice query using TEXT endpoint (client-side transcription).
   * Uses browser's Web Speech API for instant transcription, then sends text to backend.
   * Response time: 1-3 seconds instead of 30-43 seconds.
   */
  async processTextQuery(
    text: string,
    dialect: string,
    confidence?: number
  ): Promise<VoiceQueryResponse> {
    // Use raw fetch to match processVoiceQuery behavior — add auth if available, don't fail if not
    const baseURL = apiClient.getBaseURL();
    const token = authService.getAccessToken();
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    if (token) headers['Authorization'] = `Bearer ${token}`;

    const response = await fetch(`${baseURL}/api/v1/VoiceQuery/text-query`, {
      method: 'POST',
      headers,
      body: JSON.stringify({ text, dialect, confidence: confidence ?? 0.9 })
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Query failed' }));
      throw new Error(error.userFriendlyMessage || error.message || 'Query failed');
    }

    return response.json();
  }

  /**
   * Process a voice query using AUDIO endpoint (server-side Transcribe).
   * Fallback when Web Speech API is not available.
   * Response time: 15-30 seconds.
   */
  async processVoiceQuery(
    audioFile: File,
    dialect: string
  ): Promise<VoiceQueryResponse> {
    const formData = new FormData();
    formData.append('audioFile', audioFile);
    formData.append('dialect', dialect);

    return new Promise((resolve, reject) => {
      const xhr = new XMLHttpRequest();

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
            const error = new Error(errorResponse.userFriendlyMessage || errorResponse.message || 'Voice query failed') as any;
            error.errorCode = errorResponse.errorCode;
            error.suggestedActions = errorResponse.suggestedActions;
            reject(error);
          } catch {
            reject(new Error(`Voice query failed with status ${xhr.status}`));
          }
        }
      });

      // Handle errors
      xhr.addEventListener('error', () => {
        reject(new Error('Network error during voice query processing'));
      });

      xhr.addEventListener('abort', () => {
        reject(new Error('Voice query cancelled'));
      });

      // Send request
      const baseURL = apiClient.getBaseURL();
      xhr.open('POST', `${baseURL}/api/v1/VoiceQuery/query`);

      // Add auth token
      const token = authService.getAccessToken();
      if (token) {
        xhr.setRequestHeader('Authorization', `Bearer ${token}`);
      }
      
      xhr.send(formData);
    });
  }

  /**
   * Get supported dialects
   */
  async getSupportedDialects(): Promise<Dialect[]> {
    try {
      // For now, return default dialects since backend endpoint may not be implemented yet
      return this.getDefaultDialects();
    } catch (error) {
      console.error('Failed to fetch dialects:', error);
      return this.getDefaultDialects();
    }
  }

  /**
   * Get default dialects (fallback)
   */
  private getDefaultDialects(): Dialect[] {
    return [
      {
        code: 'hi-IN',
        name: 'Hindi',
        nativeName: 'हिन्दी',
        region: 'India'
      },
      {
        code: 'en-IN',
        name: 'English',
        nativeName: 'English',
        region: 'India'
      },
      {
        code: 'pa-IN',
        name: 'Punjabi',
        nativeName: 'ਪੰਜਾਬੀ',
        region: 'Punjab'
      },
      {
        code: 'mr-IN',
        name: 'Marathi',
        nativeName: 'मराठी',
        region: 'Maharashtra'
      },
      {
        code: 'gu-IN',
        name: 'Gujarati',
        nativeName: 'ગુજરાતી',
        region: 'Gujarat'
      },
      {
        code: 'ta-IN',
        name: 'Tamil',
        nativeName: 'தமிழ்',
        region: 'Tamil Nadu'
      },
      {
        code: 'te-IN',
        name: 'Telugu',
        nativeName: 'తెలుగు',
        region: 'Andhra Pradesh'
      },
      {
        code: 'kn-IN',
        name: 'Kannada',
        nativeName: 'ಕನ್ನಡ',
        region: 'Karnataka'
      },
      {
        code: 'ml-IN',
        name: 'Malayalam',
        nativeName: 'മലയാളം',
        region: 'Kerala'
      },
      {
        code: 'bn-IN',
        name: 'Bengali',
        nativeName: 'বাংলা',
        region: 'West Bengal'
      }
    ];
  }

  /**
   * Convert audio blob to supported format
   */
  async convertAudioFormat(
    audioBlob: Blob,
    targetFormat: 'mp3' | 'wav' | 'ogg' = 'mp3'
  ): Promise<File> {
    // For now, just create a File from the Blob
    // In production, you might want to use a library like lamejs for actual conversion
    const fileName = `voice_query_${Date.now()}.${targetFormat}`;
    return new File([audioBlob], fileName, { type: `audio/${targetFormat}` });
  }

  /**
   * Validate audio file
   */
  validateAudioFile(file: File): { isValid: boolean; error?: string } {
    const maxSize = 10 * 1024 * 1024; // 10MB
    const supportedFormats = ['audio/mp3', 'audio/mpeg', 'audio/wav', 'audio/ogg', 'audio/webm'];

    if (file.size > maxSize) {
      return {
        isValid: false,
        error: 'Audio file size exceeds 10MB limit'
      };
    }

    if (!supportedFormats.some(format => file.type.includes(format.split('/')[1]))) {
      return {
        isValid: false,
        error: 'Unsupported audio format. Please use MP3, WAV, or OGG'
      };
    }

    return { isValid: true };
  }

  /**
   * Get voice query history from backend
   */
  async getHistory(limit: number = 50): Promise<VoiceQueryHistoryItem[]> {
    try {
      const response = await apiClient.get<VoiceQueryHistoryItem[]>(
        `/api/v1/VoiceQuery/history?limit=${limit}`
      );
      return response;
    } catch (error) {
      console.error('Failed to fetch voice query history:', error);
      return [];
    }
  }

  /**
   * Get favorite queries from backend
   */
  async getFavorites(): Promise<VoiceQueryHistoryItem[]> {
    try {
      const response = await apiClient.get<VoiceQueryHistoryItem[]>(
        '/api/v1/VoiceQuery/favorites'
      );
      return response;
    } catch (error) {
      console.error('Failed to fetch favorite queries:', error);
      return [];
    }
  }

  /**
   * Toggle favorite status for a query
   */
  async toggleFavorite(queryId: string, isFavorite: boolean): Promise<void> {
    try {
      await apiClient.put(`/api/v1/VoiceQuery/history/${queryId}/favorite`, {
        isFavorite
      });
    } catch (error) {
      console.error('Failed to toggle favorite:', error);
      throw error;
    }
  }

  /**
   * Delete a query from history
   */
  async deleteQuery(queryId: string): Promise<void> {
    try {
      await apiClient.delete(`/api/v1/VoiceQuery/history/${queryId}`);
    } catch (error) {
      console.error('Failed to delete query:', error);
      throw error;
    }
  }

  /**
   * Get a specific query by ID (with fresh presigned URL)
   */
  async getQueryById(queryId: string): Promise<VoiceQueryHistoryItem | null> {
    try {
      const response = await apiClient.get<VoiceQueryHistoryItem>(
        `/api/v1/VoiceQuery/history/${queryId}`
      );
      return response;
    } catch (error) {
      console.error('Failed to fetch query:', error);
      return null;
    }
  }
}

// Export singleton instance
export const voiceQueryService = new VoiceQueryService();

export default voiceQueryService;
