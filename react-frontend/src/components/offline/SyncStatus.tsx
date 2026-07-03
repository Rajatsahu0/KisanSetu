/**
 * Sync Status Component
 * Displays sync progress and status information
 */

import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAppSelector } from '../../hooks/useAppSelector';
import { CheckCircle, XCircle, RefreshCw, Clock } from 'lucide-react';
import { syncService, SyncResult } from '../../services/syncService';

export const SyncStatus: React.FC = () => {
  const { t } = useTranslation();
  const syncStatus = useAppSelector((state) => state.offline.syncStatus);
  const lastSyncTime = useAppSelector((state) => state.offline.lastSyncTime);
  const queuedCount = useAppSelector((state) => state.offline.queuedRequests.length);
  
  const [lastResult, setLastResult] = useState<SyncResult | null>(null);
  const [showDetails, setShowDetails] = useState(false);

  useEffect(() => {
    // Subscribe to sync completion
    const unsubscribe = syncService.onSyncComplete((result) => {
      setLastResult(result);
    });

    return unsubscribe;
  }, []);

  const formatLastSyncTime = (timestamp: number | null): string => {
    if (!timestamp) return t('dateTime.justNow');

    const now = Date.now();
    const diff = now - timestamp;
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 1) return t('dateTime.justNow');
    if (minutes < 60) return t('dateTime.minutesAgo', { count: minutes });
    if (hours < 24) return t('dateTime.hoursAgo', { count: hours });
    return t('dateTime.daysAgo', { count: days });
  };

  if (syncStatus === 'idle' && queuedCount === 0 && !lastResult) {
    return null;
  }

  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-4">
      <div className="flex items-start justify-between">
        <div className="flex items-start gap-3">
          <div className="flex-shrink-0 mt-1">
            {syncStatus === 'syncing' && (
              <RefreshCw className="h-5 w-5 text-blue-600 animate-spin" />
            )}
            {syncStatus === 'idle' && lastResult?.success && (
              <CheckCircle className="h-5 w-5 text-green-600" />
            )}
            {syncStatus === 'error' && (
              <XCircle className="h-5 w-5 text-red-600" />
            )}
            {syncStatus === 'idle' && !lastResult && queuedCount > 0 && (
              <Clock className="h-5 w-5 text-yellow-600" />
            )}
          </div>

          <div className="flex-1">
            <h3 className="text-sm font-medium text-gray-900">
              {syncStatus === 'syncing' && t('offline.syncing')}
              {syncStatus === 'idle' && lastResult?.success && t('offline.syncComplete')}
              {syncStatus === 'error' && t('offline.syncFailed')}
              {syncStatus === 'idle' && !lastResult && queuedCount > 0 && 'Pending Sync'}
            </h3>

            {lastSyncTime && (
              <p className="text-xs text-gray-500 mt-1">
                Last synced {formatLastSyncTime(lastSyncTime)}
              </p>
            )}

            {queuedCount > 0 && (
              <p className="text-sm text-gray-600 mt-1">
                {t('offline.queuedItems', { count: queuedCount })}
              </p>
            )}

            {lastResult && showDetails && (
              <div className="mt-3 space-y-2">
                <div className="text-xs space-y-1">
                  <p className="text-green-600">
                    ✓ {lastResult.syncedCount} items synced successfully
                  </p>
                  {lastResult.failedCount > 0 && (
                    <p className="text-red-600">
                      ✗ {lastResult.failedCount} items failed
                    </p>
                  )}
                  {lastResult.conflicts.length > 0 && (
                    <p className="text-yellow-600">
                      ⚠ {lastResult.conflicts.length} conflicts detected
                    </p>
                  )}
                </div>

                {lastResult.errors.length > 0 && (
                  <div className="mt-2 p-2 bg-red-50 rounded border border-red-200">
                    <p className="text-xs font-medium text-red-800 mb-1">Errors:</p>
                    <ul className="text-xs text-red-700 space-y-1">
                      {lastResult.errors.slice(0, 3).map((error, index) => (
                        <li key={index}>• {error.error}</li>
                      ))}
                      {lastResult.errors.length > 3 && (
                        <li>• And {lastResult.errors.length - 3} more...</li>
                      )}
                    </ul>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>

        {lastResult && (
          <button
            onClick={() => setShowDetails(!showDetails)}
            className="text-xs text-blue-600 hover:text-blue-700 font-medium"
          >
            {showDetails ? t('offline.hideDetails') : t('offline.showDetails')}
          </button>
        )}
      </div>
    </div>
  );
};

export default SyncStatus;
