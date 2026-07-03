/**
 * Data retention and cleanup policies
 * Ensures compliance with data protection regulations
 */

import { secureLocalStorage } from './security';

/**
 * Data retention periods (in days)
 */
export const RETENTION_PERIODS = {
  userPreferences: 365, // 1 year
  cachedData: 30, // 30 days
  offlineQueue: 7, // 7 days
  errorLogs: 7, // 7 days
  sessionData: 1, // 1 day
  temporaryFiles: 1, // 1 day
};

/**
 * Data item with expiration
 */
interface DataItem {
  value: any;
  timestamp: number;
  expiresAt: number;
}

/**
 * Check if data item has expired
 */
function isExpired(item: DataItem): boolean {
  return Date.now() > item.expiresAt;
}

/**
 * Calculate expiration timestamp
 */
function calculateExpiration(retentionDays: number): number {
  return Date.now() + (retentionDays * 24 * 60 * 60 * 1000);
}

/**
 * Data retention manager
 */
export class DataRetentionManager {
  private storage = secureLocalStorage();
  
  /**
   * Store data with retention policy
   */
  setWithRetention(
    key: string,
    value: any,
    retentionDays: number
  ): void {
    const item: DataItem = {
      value,
      timestamp: Date.now(),
      expiresAt: calculateExpiration(retentionDays),
    };
    
    this.storage.setItem(key, JSON.stringify(item));
  }
  
  /**
   * Get data if not expired
   */
  getWithRetention(key: string): any | null {
    const itemStr = this.storage.getItem(key);
    if (!itemStr) return null;
    
    try {
      const item: DataItem = JSON.parse(itemStr);
      
      if (isExpired(item)) {
        // Data has expired, remove it
        this.storage.removeItem(key);
        return null;
      }
      
      return item.value;
    } catch (e) {
      // Invalid data, remove it
      this.storage.removeItem(key);
      return null;
    }
  }
  
  /**
   * Clean up expired data
   */
  cleanupExpired(): number {
    let cleanedCount = 0;
    
    try {
      const keys = Object.keys(localStorage);
      
      keys.forEach(key => {
        if (key.startsWith('kisan_secure_')) {
          const itemStr = this.storage.getItem(key.replace('kisan_secure_', ''));
          if (itemStr) {
            try {
              const item: DataItem = JSON.parse(itemStr);
              if (isExpired(item)) {
                this.storage.removeItem(key.replace('kisan_secure_', ''));
                cleanedCount++;
              }
            } catch (e) {
              // Invalid data, remove it
              this.storage.removeItem(key.replace('kisan_secure_', ''));
              cleanedCount++;
            }
          }
        }
      });
    } catch (e) {
      console.error('Failed to cleanup expired data:', e);
    }
    
    return cleanedCount;
  }
  
  /**
   * Get data age in days
   */
  getDataAge(key: string): number | null {
    const itemStr = this.storage.getItem(key);
    if (!itemStr) return null;
    
    try {
      const item: DataItem = JSON.parse(itemStr);
      const ageMs = Date.now() - item.timestamp;
      return Math.floor(ageMs / (24 * 60 * 60 * 1000));
    } catch (e) {
      return null;
    }
  }
  
  /**
   * Extend retention period for existing data
   */
  extendRetention(key: string, additionalDays: number): boolean {
    const itemStr = this.storage.getItem(key);
    if (!itemStr) return false;
    
    try {
      const item: DataItem = JSON.parse(itemStr);
      item.expiresAt = calculateExpiration(additionalDays);
      this.storage.setItem(key, JSON.stringify(item));
      return true;
    } catch (e) {
      return false;
    }
  }
}

/**
 * Global retention manager instance
 */
export const retentionManager = new DataRetentionManager();

/**
 * Automatic cleanup scheduler
 */
export class CleanupScheduler {
  private intervalId: number | null = null;
  
  /**
   * Start automatic cleanup
   */
  start(intervalHours: number = 24): void {
    if (this.intervalId !== null) {
      return; // Already running
    }
    
    // Run cleanup immediately
    this.runCleanup();
    
    // Schedule periodic cleanup
    this.intervalId = window.setInterval(
      () => this.runCleanup(),
      intervalHours * 60 * 60 * 1000
    );
  }
  
  /**
   * Stop automatic cleanup
   */
  stop(): void {
    if (this.intervalId !== null) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
  }
  
  /**
   * Run cleanup process
   */
  private runCleanup(): void {

    // Clean expired data
    const cleanedCount = retentionManager.cleanupExpired();
    
    // Clean IndexedDB (if applicable)
    this.cleanupIndexedDB();
    
    // Clean session storage
    this.cleanupSessionStorage();

  }
  
  /**
   * Clean up IndexedDB
   */
  private async cleanupIndexedDB(): Promise<void> {
    try {
      // This would integrate with offline storage service
      // For now, just log

    } catch (e) {
      console.error('IndexedDB cleanup failed:', e);
    }
  }
  
  /**
   * Clean up session storage
   */
  private cleanupSessionStorage(): void {
    try {
      // Remove old error logs
      const logs = sessionStorage.getItem('app_logs');
      if (logs) {
        const parsed = JSON.parse(logs);
        const cutoff = Date.now() - (RETENTION_PERIODS.errorLogs * 24 * 60 * 60 * 1000);
        
        const filtered = parsed.filter((log: any) => {
          const logTime = new Date(log.timestamp).getTime();
          return logTime > cutoff;
        });
        
        sessionStorage.setItem('app_logs', JSON.stringify(filtered));
      }
    } catch (e) {
      console.error('Session storage cleanup failed:', e);
    }
  }
}

/**
 * Global cleanup scheduler instance
 */
export const cleanupScheduler = new CleanupScheduler();

/**
 * User data deletion (GDPR compliance)
 */
export async function deleteAllUserData(): Promise<void> {
  try {
    // Clear local storage
    secureLocalStorage().clear();
    
    // Clear session storage
    sessionStorage.clear();
    
    // Clear IndexedDB
    const databases = await indexedDB.databases();
    for (const db of databases) {
      if (db.name) {
        indexedDB.deleteDatabase(db.name);
      }
    }
    
    // Clear cookies (if any)
    document.cookie.split(';').forEach(cookie => {
      const name = cookie.split('=')[0].trim();
      document.cookie = `${name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;`;
    });

  } catch (error) {
    console.error('Failed to delete user data:', error);
    throw new Error('Failed to delete user data');
  }
}

/**
 * Export user data (GDPR compliance)
 */
export function exportUserData(): string {
  const data: Record<string, any> = {};
  
  try {
    // Export local storage
    const keys = Object.keys(localStorage);
    keys.forEach(key => {
      if (key.startsWith('kisan_secure_')) {
        const value = secureLocalStorage().getItem(key.replace('kisan_secure_', ''));
        if (value) {
          data[key] = value;
        }
      }
    });
    
    // Export session storage
    const sessionKeys = Object.keys(sessionStorage);
    sessionKeys.forEach(key => {
      data[`session_${key}`] = sessionStorage.getItem(key);
    });
    
    return JSON.stringify(data, null, 2);
  } catch (error) {
    console.error('Failed to export user data:', error);
    throw new Error('Failed to export user data');
  }
}

/**
 * Initialize data retention policies
 */
export function initializeDataRetention(): void {
  // Start automatic cleanup
  cleanupScheduler.start(24); // Run every 24 hours
  
  // Run initial cleanup
  retentionManager.cleanupExpired();

}
