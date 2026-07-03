/**
 * Offline Storage Service using IndexedDB
 * Provides persistent storage for offline data management
 */

import { openDB, DBSchema, IDBPDatabase } from 'idb';

interface OfflineDB extends DBSchema {
  cachedData: {
    key: string;
    value: {
      key: string;
      data: any;
      timestamp: number;
      expiresAt: number;
      version: number;
    };
  };
  queuedRequests: {
    key: string;
    value: {
      id: string;
      method: 'GET' | 'POST' | 'PUT' | 'DELETE';
      url: string;
      data?: any;
      files?: { name: string; type: string; size: number; data: ArrayBuffer }[];
      timestamp: number;
      retryCount: number;
      priority: 'high' | 'medium' | 'low';
    };
  };
  offlineFiles: {
    key: string;
    value: {
      id: string;
      file: ArrayBuffer;
      fileName: string;
      fileType: string;
      fileSize: number;
      timestamp: number;
    };
  };
}

class OfflineStorageService {
  private db: IDBPDatabase<OfflineDB> | null = null;
  private readonly DB_NAME = 'kisan-mitra-offline';
  private readonly DB_VERSION = 1;

  /**
   * Initialize the database
   */
  async init(): Promise<void> {
    if (this.db) return;

    this.db = await openDB<OfflineDB>(this.DB_NAME, this.DB_VERSION, {
      upgrade(db) {
        // Create object stores
        if (!db.objectStoreNames.contains('cachedData')) {
          db.createObjectStore('cachedData', { keyPath: 'key' });
        }
        if (!db.objectStoreNames.contains('queuedRequests')) {
          const requestStore = db.createObjectStore('queuedRequests', { keyPath: 'id' });
          requestStore.createIndex('priority', 'priority');
          requestStore.createIndex('timestamp', 'timestamp');
        }
        if (!db.objectStoreNames.contains('offlineFiles')) {
          db.createObjectStore('offlineFiles', { keyPath: 'id' });
        }
      },
    });
  }

  /**
   * Cache data for offline access
   */
  async cacheData(key: string, data: any, ttlMinutes: number = 60): Promise<void> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    const now = Date.now();
    const expiresAt = now + ttlMinutes * 60 * 1000;

    await this.db.put('cachedData', {
      key,
      data,
      timestamp: now,
      expiresAt,
      version: 1,
    });
  }

  /**
   * Get cached data
   */
  async getCachedData<T>(key: string): Promise<T | null> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    const cached = await this.db.get('cachedData', key);
    
    if (!cached) return null;

    // Check if expired
    if (cached.expiresAt < Date.now()) {
      await this.db.delete('cachedData', key);
      return null;
    }

    return cached.data as T;
  }

  /**
   * Clear expired cache entries
   */
  async clearExpiredCache(): Promise<void> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    const now = Date.now();
    const allCached = await this.db.getAll('cachedData');

    for (const cached of allCached) {
      if (cached.expiresAt < now) {
        await this.db.delete('cachedData', cached.key);
      }
    }
  }

  /**
   * Add request to offline queue
   */
  async queueRequest(
    method: 'GET' | 'POST' | 'PUT' | 'DELETE',
    url: string,
    data?: any,
    files?: File[],
    priority: 'high' | 'medium' | 'low' = 'medium'
  ): Promise<string> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    const id = `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;

    // Convert files to ArrayBuffer for storage
    const fileData = files ? await Promise.all(
      files.map(async (file) => ({
        name: file.name,
        type: file.type,
        size: file.size,
        data: await file.arrayBuffer(),
      }))
    ) : undefined;

    await this.db.add('queuedRequests', {
      id,
      method,
      url,
      data,
      files: fileData,
      timestamp: Date.now(),
      retryCount: 0,
      priority,
    });

    return id;
  }

  /**
   * Get all queued requests
   */
  async getQueuedRequests(): Promise<any[]> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    const requests = await this.db.getAll('queuedRequests');
    
    // Sort by priority and timestamp
    return requests.sort((a, b) => {
      const priorityOrder = { high: 0, medium: 1, low: 2 };
      const priorityDiff = priorityOrder[a.priority] - priorityOrder[b.priority];
      return priorityDiff !== 0 ? priorityDiff : a.timestamp - b.timestamp;
    });
  }

  /**
   * Remove request from queue
   */
  async removeQueuedRequest(id: string): Promise<void> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    await this.db.delete('queuedRequests', id);
  }

  /**
   * Increment retry count for a request
   */
  async incrementRetryCount(id: string): Promise<void> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    const request = await this.db.get('queuedRequests', id);
    if (request) {
      request.retryCount += 1;
      await this.db.put('queuedRequests', request);
    }
  }

  /**
   * Store file for offline upload
   */
  async storeFile(file: File): Promise<string> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    const id = `file_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    const arrayBuffer = await file.arrayBuffer();

    await this.db.add('offlineFiles', {
      id,
      file: arrayBuffer,
      fileName: file.name,
      fileType: file.type,
      fileSize: file.size,
      timestamp: Date.now(),
    });

    return id;
  }

  /**
   * Get stored file
   */
  async getFile(id: string): Promise<File | null> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    const stored = await this.db.get('offlineFiles', id);
    if (!stored) return null;

    return new File([stored.file], stored.fileName, { type: stored.fileType });
  }

  /**
   * Remove stored file
   */
  async removeFile(id: string): Promise<void> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    await this.db.delete('offlineFiles', id);
  }

  /**
   * Get storage usage statistics
   */
  async getStorageStats(): Promise<{
    cachedItems: number;
    queuedRequests: number;
    storedFiles: number;
  }> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    const [cachedItems, queuedRequests, storedFiles] = await Promise.all([
      this.db.count('cachedData'),
      this.db.count('queuedRequests'),
      this.db.count('offlineFiles'),
    ]);

    return { cachedItems, queuedRequests, storedFiles };
  }

  /**
   * Clear all offline data
   */
  async clearAll(): Promise<void> {
    await this.init();
    if (!this.db) throw new Error('Database not initialized');

    await Promise.all([
      this.db.clear('cachedData'),
      this.db.clear('queuedRequests'),
      this.db.clear('offlineFiles'),
    ]);
  }
}

// Export singleton instance
export const offlineStorageService = new OfflineStorageService();
export default offlineStorageService;
