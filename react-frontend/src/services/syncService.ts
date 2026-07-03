/**
 * Sync Service for offline data synchronization
 * Handles automatic sync when connectivity returns
 */

import { offlineStorageService } from './offlineStorageService';
import { store } from '../store';
import { setSyncStatus, setLastSyncTime, removeQueuedRequest } from '../store/slices/offlineSlice';

export interface SyncConflict {
  localData: any;
  serverData: any;
  conflictType: 'update' | 'delete' | 'create';
  resolution?: 'local' | 'server' | 'merge';
}

export interface SyncResult {
  success: boolean;
  syncedCount: number;
  failedCount: number;
  conflicts: SyncConflict[];
  errors: Array<{ requestId: string; error: string }>;
}

export type ConflictResolver = (conflict: SyncConflict) => Promise<'local' | 'server' | 'merge'>;

let conflictResolver: ConflictResolver | null = null;

export function setConflictResolver(resolver: ConflictResolver): void {
  conflictResolver = resolver;
}

class SyncService {
  private isSyncing = false;
  private readonly MAX_RETRIES = 3;
  private readonly RETRY_DELAY = 2000;
  private syncListeners: Array<(result: SyncResult) => void> = [];

  /**
   * Start automatic sync
   */
  async startSync(): Promise<SyncResult> {
    if (this.isSyncing) {

      return {
        success: false,
        syncedCount: 0,
        failedCount: 0,
        conflicts: [],
        errors: [{ requestId: 'sync', error: 'Sync already in progress' }],
      };
    }

    this.isSyncing = true;
    store.dispatch(setSyncStatus('syncing'));

    const result: SyncResult = {
      success: true,
      syncedCount: 0,
      failedCount: 0,
      conflicts: [],
      errors: [],
    };

    try {
      // Get all queued requests
      const queuedRequests = await offlineStorageService.getQueuedRequests();

      // Process each request
      for (const request of queuedRequests) {
        try {
          await this.syncRequest(request);
          result.syncedCount++;
          
          // Remove from queue
          await offlineStorageService.removeQueuedRequest(request.id);
          store.dispatch(removeQueuedRequest(request.id));
        } catch (error) {
          console.error(`Failed to sync request ${request.id}:`, error);
          result.failedCount++;
          result.errors.push({
            requestId: request.id,
            error: error instanceof Error ? error.message : 'Unknown error',
          });

          // Increment retry count
          await offlineStorageService.incrementRetryCount(request.id);

          // Remove if max retries exceeded
          if (request.retryCount >= this.MAX_RETRIES) {
            await offlineStorageService.removeQueuedRequest(request.id);
            store.dispatch(removeQueuedRequest(request.id));
          }
        }
      }

      // Update sync status
      store.dispatch(setSyncStatus('idle'));
      store.dispatch(setLastSyncTime(Date.now()));

      // Notify listeners
      this.notifyListeners(result);

      return result;
    } catch (error) {
      console.error('Sync failed:', error);
      store.dispatch(setSyncStatus('error'));
      result.success = false;
      return result;
    } finally {
      this.isSyncing = false;
    }
  }

  /**
   * Sync a single request
   */
  private async syncRequest(request: any): Promise<void> {
    const { method, url, data, files } = request;

    // Convert stored file data back to File objects
    const fileObjects = files
      ? files.map(
          (f: any) => new File([f.data], f.name, { type: f.type })
        )
      : undefined;

    // Make the API request
    const response = await this.makeRequest(method, url, data, fileObjects);

    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Make HTTP request
   */
  private async makeRequest(
    method: string,
    url: string,
    data?: any,
    files?: File[]
  ): Promise<Response> {
    const token = this.getAuthToken();
    const headers: HeadersInit = {
      Authorization: `Bearer ${token}`,
    };

    let body: any;

    if (files && files.length > 0) {
      // Use FormData for file uploads
      const formData = new FormData();
      files.forEach((file, index) => {
        formData.append(`file${index}`, file);
      });
      if (data) {
        Object.keys(data).forEach((key) => {
          formData.append(key, data[key]);
        });
      }
      body = formData;
    } else if (data) {
      headers['Content-Type'] = 'application/json';
      body = JSON.stringify(data);
    }

    return fetch(url, {
      method,
      headers,
      body,
    });
  }

  /**
   * Get authentication token
   */
  private getAuthToken(): string | null {
    const state = store.getState();
    return state.auth?.tokens?.accessToken || null;
  }

  /**
   * Detect sync conflicts
   */
  async detectConflicts(localData: any, serverData: any): Promise<SyncConflict | null> {
    // Simple conflict detection based on timestamps
    if (!localData || !serverData) return null;

    const localTimestamp = localData.timestamp || 0;
    const serverTimestamp = serverData.timestamp || 0;

    if (localTimestamp > serverTimestamp) {
      return {
        localData,
        serverData,
        conflictType: 'update',
      };
    }

    return null;
  }

  /**
   * Resolve sync conflict
   */
  async resolveConflict(
    conflict: SyncConflict,
    resolution: 'local' | 'server' | 'merge'
  ): Promise<any> {
    conflict.resolution = resolution;

    switch (resolution) {
      case 'local':
        return conflict.localData;
      case 'server':
        return conflict.serverData;
      case 'merge':
        // Simple merge strategy - prefer local for user data, server for system data
        return {
          ...conflict.serverData,
          ...conflict.localData,
        };
      default:
        return conflict.serverData;
    }
  }

  /**
   * Add sync listener
   */
  onSyncComplete(listener: (result: SyncResult) => void): () => void {
    this.syncListeners.push(listener);
    
    // Return unsubscribe function
    return () => {
      this.syncListeners = this.syncListeners.filter((l) => l !== listener);
    };
  }

  /**
   * Notify all listeners
   */
  private notifyListeners(result: SyncResult): void {
    this.syncListeners.forEach((listener) => {
      try {
        listener(result);
      } catch (error) {
        console.error('Error in sync listener:', error);
      }
    });
  }

  /**
   * Check if sync is needed
   */
  async needsSync(): Promise<boolean> {
    const stats = await offlineStorageService.getStorageStats();
    return stats.queuedRequests > 0;
  }

  /**
   * Get sync status
   */
  getSyncStatus(): {
    isSyncing: boolean;
    lastSyncTime: number | null;
  } {
    const state = store.getState();
    return {
      isSyncing: this.isSyncing,
      lastSyncTime: state.offline.lastSyncTime,
    };
  }
}

// Export singleton instance
export const syncService = new SyncService();
export default syncService;
