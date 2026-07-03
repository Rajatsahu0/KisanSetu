/**
 * Connectivity Service for monitoring online/offline status
 * Handles automatic sync when connectivity returns
 */

import { syncService } from './syncService';
import { store } from '../store';
import { setOnlineStatus } from '../store/slices/appSlice';

export type ConnectivityStatus = 'online' | 'offline' | 'slow';

export interface ConnectivityInfo {
  isOnline: boolean;
  effectiveType?: string;
  downlink?: number;
  rtt?: number;
  saveData?: boolean;
}

class ConnectivityService {
  private listeners: Array<(status: ConnectivityStatus) => void> = [];
  private checkInterval: NodeJS.Timeout | null = null;
  private readonly CHECK_INTERVAL = 30000; // 30 seconds

  /**
   * Initialize connectivity monitoring
   */
  init(): void {
    // Listen for online/offline events
    window.addEventListener('online', this.handleOnline);
    window.addEventListener('offline', this.handleOffline);

    // Listen for connection change events
    if ('connection' in navigator) {
      const connection = (navigator as any).connection;
      connection?.addEventListener('change', this.handleConnectionChange);
    }

    // Set initial status
    this.updateStatus();

    // Start periodic connectivity checks
    this.startPeriodicCheck();
  }

  /**
   * Cleanup event listeners
   */
  cleanup(): void {
    window.removeEventListener('online', this.handleOnline);
    window.removeEventListener('offline', this.handleOffline);

    if ('connection' in navigator) {
      const connection = (navigator as any).connection;
      connection?.removeEventListener('change', this.handleConnectionChange);
    }

    if (this.checkInterval) {
      clearInterval(this.checkInterval);
      this.checkInterval = null;
    }
  }

  /**
   * Handle online event
   */
  private handleOnline = async (): Promise<void> => {

    this.updateStatus();

    // Trigger automatic sync
    try {
      const needsSync = await syncService.needsSync();
      if (needsSync) {

        await syncService.startSync();
      }
    } catch (error) {
      console.error('Auto-sync failed:', error);
    }
  };

  /**
   * Handle offline event
   */
  private handleOffline = (): void => {

    this.updateStatus();
  };

  /**
   * Handle connection change
   */
  private handleConnectionChange = (): void => {

    this.updateStatus();
  };

  /**
   * Update connectivity status
   */
  private updateStatus(): void {
    const status = this.getStatus();
    const isOnline = status !== 'offline';

    // Update Redux store
    store.dispatch(setOnlineStatus(isOnline));

    // Notify listeners
    this.notifyListeners(status);
  }

  /**
   * Get current connectivity status
   */
  getStatus(): ConnectivityStatus {
    if (!navigator.onLine) {
      return 'offline';
    }

    // Check connection quality if available
    if ('connection' in navigator) {
      const connection = (navigator as any).connection;
      const effectiveType = connection?.effectiveType;

      if (effectiveType === 'slow-2g' || effectiveType === '2g') {
        return 'slow';
      }
    }

    return 'online';
  }

  /**
   * Get detailed connectivity information
   */
  getInfo(): ConnectivityInfo {
    const info: ConnectivityInfo = {
      isOnline: navigator.onLine,
    };

    if ('connection' in navigator) {
      const connection = (navigator as any).connection;
      info.effectiveType = connection?.effectiveType;
      info.downlink = connection?.downlink;
      info.rtt = connection?.rtt;
      info.saveData = connection?.saveData;
    }

    return info;
  }

  /**
   * Check connectivity by pinging server
   */
  async checkConnectivity(): Promise<boolean> {
    try {
      const controller = new AbortController();
      const timeoutId = setTimeout(() => controller.abort(), 5000);

      const response = await fetch('/api/health', {
        method: 'HEAD',
        signal: controller.signal,
      });

      clearTimeout(timeoutId);
      return response.ok;
    } catch (error) {
      return false;
    }
  }

  /**
   * Start periodic connectivity check
   */
  private startPeriodicCheck(): void {
    this.checkInterval = setInterval(async () => {
      const isConnected = await this.checkConnectivity();
      const currentStatus = store.getState().app.isOnline;

      // Update if status changed
      if (isConnected !== currentStatus) {
        if (isConnected) {
          this.handleOnline();
        } else {
          this.handleOffline();
        }
      }
    }, this.CHECK_INTERVAL);
  }

  /**
   * Add status change listener
   */
  onStatusChange(listener: (status: ConnectivityStatus) => void): () => void {
    this.listeners.push(listener);

    // Return unsubscribe function
    return () => {
      this.listeners = this.listeners.filter((l) => l !== listener);
    };
  }

  /**
   * Notify all listeners
   */
  private notifyListeners(status: ConnectivityStatus): void {
    this.listeners.forEach((listener) => {
      try {
        listener(status);
      } catch (error) {
        console.error('Error in connectivity listener:', error);
      }
    });
  }

  /**
   * Force sync now
   */
  async forceSync(): Promise<void> {
    if (!navigator.onLine) {
      throw new Error('Cannot sync while offline');
    }

    await syncService.startSync();
  }
}

// Export singleton instance
export const connectivityService = new ConnectivityService();
export default connectivityService;
