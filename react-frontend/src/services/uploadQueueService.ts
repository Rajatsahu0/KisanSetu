/**
 * Upload Queue Service for offline upload management
 */

import { store } from '../store';
import { addQueuedRequest, removeQueuedRequest, incrementRetryCount, setSyncStatus, setLastSyncTime } from '../store/slices/offlineSlice';
import { s3UploadService } from './s3UploadService';

export interface QueuedUpload {
  id: string;
  file: File;
  endpoint: string;
  farmerId: string;
  timestamp: number;
  retryCount: number;
  status: 'pending' | 'uploading' | 'completed' | 'failed';
  error?: string;
  progress?: number;
}

export interface UploadHistoryItem {
  id: string;
  fileName: string;
  fileSize: number;
  uploadedAt: string;
  status: 'success' | 'failed';
  s3Key?: string;
  error?: string;
}

class UploadQueueService {
  private queue: QueuedUpload[] = [];
  private history: UploadHistoryItem[] = [];
  private isProcessing = false;
  private readonly MAX_RETRIES = 3;
  private readonly STORAGE_KEY = 'upload_history';

  constructor() {
    this.loadHistory();
  }

  /**
   * Add upload to queue
   */
  addToQueue(
    file: File,
    endpoint: string,
    farmerId: string,
    priority: 'high' | 'medium' | 'low' = 'medium'
  ): string {
    const uploadId = `upload_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

    const queuedUpload: QueuedUpload = {
      id: uploadId,
      file,
      endpoint,
      farmerId,
      timestamp: Date.now(),
      retryCount: 0,
      status: 'pending',
      progress: 0
    };

    this.queue.push(queuedUpload);

    // Also add to Redux offline queue
    store.dispatch(addQueuedRequest({
      method: 'POST',
      url: endpoint,
      files: [file],
      priority
    }));

    // Start processing if not already processing
    if (!this.isProcessing) {
      this.processQueue();
    }

    return uploadId;
  }

  /**
   * Process upload queue
   */
  private async processQueue(): Promise<void> {
    if (this.isProcessing || this.queue.length === 0) {
      return;
    }

    this.isProcessing = true;
    store.dispatch(setSyncStatus('syncing'));

    while (this.queue.length > 0) {
      const upload = this.queue[0];

      // Skip if already completed or max retries exceeded
      if (upload.status === 'completed' || upload.retryCount >= this.MAX_RETRIES) {
        this.queue.shift();
        continue;
      }

      try {
        // Update status
        upload.status = 'uploading';

        // Attempt upload
        const result = await s3UploadService.uploadFile(
          upload.file,
          upload.farmerId,
          {
            onProgress: (progress) => {
              upload.progress = progress;
            },
            onError: (error) => {
              upload.error = error.message;
            }
          }
        );

        // Mark as completed
        upload.status = 'completed';

        // Add to history
        this.addToHistory({
          id: upload.id,
          fileName: upload.file.name,
          fileSize: upload.file.size,
          uploadedAt: new Date().toISOString(),
          status: 'success',
          s3Key: result.s3Key
        });

        // Remove from queue
        this.queue.shift();

        // Remove from Redux queue
        store.dispatch(removeQueuedRequest(upload.id));
      } catch (error) {
        // Handle failure
        upload.status = 'failed';
        upload.retryCount++;
        upload.error = error instanceof Error ? error.message : 'Upload failed';

        // Increment retry count in Redux
        store.dispatch(incrementRetryCount(upload.id));

        if (upload.retryCount >= this.MAX_RETRIES) {
          // Add to history as failed
          this.addToHistory({
            id: upload.id,
            fileName: upload.file.name,
            fileSize: upload.file.size,
            uploadedAt: new Date().toISOString(),
            status: 'failed',
            error: upload.error
          });

          // Remove from queue
          this.queue.shift();
          store.dispatch(removeQueuedRequest(upload.id));
        } else {
          // Move to end of queue for retry
          this.queue.push(this.queue.shift()!);
          
          // Wait before retry (exponential backoff)
          const delay = Math.pow(2, upload.retryCount) * 1000;
          await this.sleep(delay);
        }
      }
    }

    this.isProcessing = false;
    store.dispatch(setSyncStatus('idle'));
    store.dispatch(setLastSyncTime(Date.now()));
  }

  /**
   * Get queue status
   */
  getQueueStatus(): {
    pending: number;
    uploading: number;
    completed: number;
    failed: number;
    total: number;
  } {
    return {
      pending: this.queue.filter(u => u.status === 'pending').length,
      uploading: this.queue.filter(u => u.status === 'uploading').length,
      completed: this.queue.filter(u => u.status === 'completed').length,
      failed: this.queue.filter(u => u.status === 'failed' && u.retryCount >= this.MAX_RETRIES).length,
      total: this.queue.length
    };
  }

  /**
   * Get upload by ID
   */
  getUpload(uploadId: string): QueuedUpload | undefined {
    return this.queue.find(u => u.id === uploadId);
  }

  /**
   * Cancel upload
   */
  cancelUpload(uploadId: string): boolean {
    const index = this.queue.findIndex(u => u.id === uploadId);
    if (index !== -1) {
      const upload = this.queue[index];
      
      // Only cancel if not uploading
      if (upload.status !== 'uploading') {
        this.queue.splice(index, 1);
        store.dispatch(removeQueuedRequest(uploadId));
        return true;
      }
    }
    return false;
  }

  /**
   * Retry failed upload
   */
  retryUpload(uploadId: string): boolean {
    const upload = this.queue.find(u => u.id === uploadId);
    if (upload && upload.status === 'failed') {
      upload.status = 'pending';
      upload.retryCount = 0;
      upload.error = undefined;
      
      if (!this.isProcessing) {
        this.processQueue();
      }
      return true;
    }
    return false;
  }

  /**
   * Clear completed uploads from queue
   */
  clearCompleted(): void {
    this.queue = this.queue.filter(u => u.status !== 'completed');
  }

  /**
   * Add to upload history
   */
  private addToHistory(item: UploadHistoryItem): void {
    this.history.unshift(item);
    
    // Keep only last 100 items
    if (this.history.length > 100) {
      this.history = this.history.slice(0, 100);
    }
    
    this.saveHistory();
  }

  /**
   * Get upload history
   */
  getHistory(limit?: number): UploadHistoryItem[] {
    return limit ? this.history.slice(0, limit) : this.history;
  }

  /**
   * Clear upload history
   */
  clearHistory(): void {
    this.history = [];
    this.saveHistory();
  }

  /**
   * Save history to localStorage
   */
  private saveHistory(): void {
    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.history));
    } catch (error) {
      console.error('Failed to save upload history:', error);
    }
  }

  /**
   * Load history from localStorage
   */
  private loadHistory(): void {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      if (stored) {
        this.history = JSON.parse(stored);
      }
    } catch (error) {
      console.error('Failed to load upload history:', error);
      this.history = [];
    }
  }

  /**
   * Sleep utility
   */
  private sleep(ms: number): Promise<void> {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

// Export singleton instance
export const uploadQueueService = new UploadQueueService();

export default uploadQueueService;
