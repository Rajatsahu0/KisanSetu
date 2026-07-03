/**
 * Offline Indicator Component
 * Displays connectivity status and sync information
 */

import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppSelector } from '../../hooks/useAppSelector';
import { WifiOff, Wifi, RefreshCw, AlertCircle } from 'lucide-react';
import { connectivityService, ConnectivityStatus } from '../../services/connectivityService';
import { syncService } from '../../services/syncService';

export const OfflineIndicator: React.FC = () => {
  const { t } = useTranslation();
  const isOnline = useAppSelector((state) => state.app.isOnline);
  const syncStatus = useAppSelector((state) => state.offline.syncStatus);
  const queuedRequests = useAppSelector((state) => state.offline.queuedRequests);
  
  const [connectivityStatus, setConnectivityStatus] = useState<ConnectivityStatus>('online');
  const [showDetails, setShowDetails] = useState(false);

  useEffect(() => {
    // Subscribe to connectivity changes
    const unsubscribe = connectivityService.onStatusChange((status) => {
      setConnectivityStatus(status);
    });

    // Set initial status
    setConnectivityStatus(connectivityService.getStatus());

    return unsubscribe;
  }, []);

  const handleForceSync = async () => {
    try {
      await connectivityService.forceSync();
    } catch (error) {
      console.error('Force sync failed:', error);
    }
  };

  // Don't show indicator if online and no queued requests
  if (isOnline && queuedRequests.length === 0 && syncStatus === 'idle') {
    return null;
  }

  return (
    <div className="fixed bottom-4 right-4 z-50">
      <div
        className={`rounded-lg shadow-lg p-4 max-w-sm ${
          isOnline
            ? 'bg-blue-50 border border-blue-200'
            : 'bg-yellow-50 border border-yellow-200'
        }`}
      >
        <div className="flex items-start gap-3">
          <div className="flex-shrink-0">
            {isOnline ? (
              syncStatus === 'syncing' ? (
                <RefreshCw className="h-5 w-5 text-blue-600 animate-spin" />
              ) : (
                <Wifi className="h-5 w-5 text-blue-600" />
              )
            ) : (
              <WifiOff className="h-5 w-5 text-yellow-600" />
            )}
          </div>

          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-gray-900">
              {isOnline
                ? syncStatus === 'syncing'
                  ? t('offline.syncing')
                  : t('offline.online')
                : t('offline.offline')}
            </p>

            {!isOnline && (
              <p className="mt-1 text-sm text-gray-600">
                {t('offline.offlineMessage')}
              </p>
            )}

            {queuedRequests.length > 0 && (
              <p className="mt-1 text-sm text-gray-600">
                {t('offline.queuedItems', { count: queuedRequests.length })}
              </p>
            )}

            {syncStatus === 'error' && (
              <div className="mt-2 flex items-center gap-2 text-sm text-red-600">
                <AlertCircle className="h-4 w-4" />
                <span>{t('offline.syncError')}</span>
              </div>
            )}

            {showDetails && (
              <div className="mt-3 space-y-2">
                <div className="text-xs text-gray-500">
                  <p>
                    {t('offline.status')}:{' '}
                    <span className="font-medium">{connectivityStatus}</span>
                  </p>
                  {connectivityStatus === 'slow' && (
                    <p className="text-yellow-600">
                      {t('offline.slowConnection')}
                    </p>
                  )}
                </div>

                {isOnline && queuedRequests.length > 0 && (
                  <button
                    onClick={handleForceSync}
                    disabled={syncStatus === 'syncing'}
                    className="text-xs text-blue-600 hover:text-blue-700 font-medium disabled:opacity-50"
                  >
                    {t('offline.syncNow')}
                  </button>
                )}
              </div>
            )}

            <button
              onClick={() => setShowDetails(!showDetails)}
              className="mt-2 text-xs text-gray-500 hover:text-gray-700"
            >
              {showDetails ? t('offline.hideDetails') : t('offline.showDetails')}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default OfflineIndicator;
