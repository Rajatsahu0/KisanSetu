/**
 * PWA Service for managing service worker and app installation
 */

import { registerSW } from 'virtual:pwa-register';

export interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

class PWAService {
  private deferredPrompt: BeforeInstallPromptEvent | null = null;
  private updateSW: ((reloadPage?: boolean) => Promise<void>) | null = null;
  private installListeners: Array<() => void> = [];
  private updateListeners: Array<() => void> = [];

  /**
   * Initialize PWA service
   */
  init(): void {
    // Register service worker
    this.registerServiceWorker();

    // Listen for install prompt
    this.setupInstallPrompt();

    // Check if already installed
    this.checkInstallStatus();
  }

  /**
   * Register service worker with auto-update
   */
  private registerServiceWorker(): void {
    this.updateSW = registerSW({
      onNeedRefresh: () => {

        this.notifyUpdateListeners();
      },
      onOfflineReady: () => {

      },
      onRegistered: (registration: any) => {

      },
      onRegisterError: (error: any) => {
        console.error('Service Worker registration failed:', error);
      },
    });
  }

  /**
   * Setup install prompt handling
   */
  private setupInstallPrompt(): void {
    window.addEventListener('beforeinstallprompt', (e: Event) => {
      e.preventDefault();
      this.deferredPrompt = e as BeforeInstallPromptEvent;
      this.notifyInstallListeners();
    });

    window.addEventListener('appinstalled', () => {

      this.deferredPrompt = null;
    });
  }

  /**
   * Check if app is installed
   */
  private checkInstallStatus(): void {
    if (window.matchMedia('(display-mode: standalone)').matches) {

    }
  }

  /**
   * Prompt user to install app
   */
  async promptInstall(): Promise<boolean> {
    if (!this.deferredPrompt) {

      return false;
    }

    try {
      await this.deferredPrompt.prompt();
      const { outcome } = await this.deferredPrompt.userChoice;

      this.deferredPrompt = null;
      
      return outcome === 'accepted';
    } catch (error) {
      console.error('Error showing install prompt:', error);
      return false;
    }
  }

  /**
   * Check if install prompt is available
   */
  canInstall(): boolean {
    return this.deferredPrompt !== null;
  }

  /**
   * Check if app is installed
   */
  isInstalled(): boolean {
    return (
      window.matchMedia('(display-mode: standalone)').matches ||
      (window.navigator as any).standalone === true
    );
  }

  /**
   * Update the app
   */
  async update(reloadPage: boolean = true): Promise<void> {
    if (this.updateSW) {
      await this.updateSW(reloadPage);
    }
  }

  /**
   * Add install listener
   */
  onInstallAvailable(listener: () => void): () => void {
    this.installListeners.push(listener);
    
    // Call immediately if prompt is already available
    if (this.deferredPrompt) {
      listener();
    }

    // Return unsubscribe function
    return () => {
      this.installListeners = this.installListeners.filter((l) => l !== listener);
    };
  }

  /**
   * Add update listener
   */
  onUpdateAvailable(listener: () => void): () => void {
    this.updateListeners.push(listener);

    // Return unsubscribe function
    return () => {
      this.updateListeners = this.updateListeners.filter((l) => l !== listener);
    };
  }

  /**
   * Notify install listeners
   */
  private notifyInstallListeners(): void {
    this.installListeners.forEach((listener) => {
      try {
        listener();
      } catch (error) {
        console.error('Error in install listener:', error);
      }
    });
  }

  /**
   * Notify update listeners
   */
  private notifyUpdateListeners(): void {
    this.updateListeners.forEach((listener) => {
      try {
        listener();
      } catch (error) {
        console.error('Error in update listener:', error);
      }
    });
  }

  /**
   * Get app info
   */
  getAppInfo(): {
    isInstalled: boolean;
    canInstall: boolean;
    isStandalone: boolean;
  } {
    return {
      isInstalled: this.isInstalled(),
      canInstall: this.canInstall(),
      isStandalone: window.matchMedia('(display-mode: standalone)').matches,
    };
  }
}

// Export singleton instance
export const pwaService = new PWAService();
export default pwaService;
