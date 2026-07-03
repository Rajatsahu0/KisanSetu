/**
 * Conflict Resolution Dialog
 * Allows users to resolve sync conflicts between local and server data
 */

import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertCircle, Check } from 'lucide-react';
import { SyncConflict } from '../../services/syncService';

interface ConflictResolutionDialogProps {
  conflict: SyncConflict;
  onResolve: (resolution: 'local' | 'server' | 'merge') => void;
  onCancel: () => void;
}

export const ConflictResolutionDialog: React.FC<ConflictResolutionDialogProps> = ({
  conflict,
  onResolve,
  onCancel,
}) => {
  const { t } = useTranslation();
  const [selectedResolution, setSelectedResolution] = useState<'local' | 'server' | 'merge'>('server');

  const handleResolve = () => {
    onResolve(selectedResolution);
  };

  const formatData = (data: any): string => {
    if (typeof data === 'object') {
      return JSON.stringify(data, null, 2);
    }
    return String(data);
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex min-h-screen items-center justify-center p-4">
        {/* Backdrop */}
        <div
          className="fixed inset-0 bg-black bg-opacity-50 transition-opacity"
          onClick={onCancel}
        />

        {/* Dialog */}
        <div className="relative bg-white rounded-lg shadow-xl max-w-4xl w-full max-h-[90vh] overflow-hidden">
          {/* Header */}
          <div className="bg-yellow-50 border-b border-yellow-200 px-6 py-4">
            <div className="flex items-center gap-3">
              <AlertCircle className="h-6 w-6 text-yellow-600" />
              <div>
                <h3 className="text-lg font-semibold text-gray-900">
                  {t('offline.conflictDetected')}
                </h3>
                <p className="text-sm text-gray-600 mt-1">
                  {t('offline.resolveConflict')}
                </p>
              </div>
            </div>
          </div>

          {/* Content */}
          <div className="px-6 py-4 overflow-y-auto max-h-[60vh]">
            <div className="space-y-4">
              {/* Resolution Options */}
              <div className="space-y-3">
                {/* Use Local */}
                <label
                  className={`flex items-start gap-3 p-4 border-2 rounded-lg cursor-pointer transition-colors ${
                    selectedResolution === 'local'
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  <input
                    type="radio"
                    name="resolution"
                    value="local"
                    checked={selectedResolution === 'local'}
                    onChange={() => setSelectedResolution('local')}
                    className="mt-1"
                  />
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-gray-900">
                        {t('offline.useLocal')}
                      </span>
                      {selectedResolution === 'local' && (
                        <Check className="h-4 w-4 text-blue-600" />
                      )}
                    </div>
                    <p className="text-sm text-gray-600 mt-1">
                      Keep your local changes and overwrite server data
                    </p>
                    <div className="mt-3 p-3 bg-gray-50 rounded border border-gray-200">
                      <pre className="text-xs text-gray-700 overflow-x-auto">
                        {formatData(conflict.localData)}
                      </pre>
                    </div>
                  </div>
                </label>

                {/* Use Server */}
                <label
                  className={`flex items-start gap-3 p-4 border-2 rounded-lg cursor-pointer transition-colors ${
                    selectedResolution === 'server'
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  <input
                    type="radio"
                    name="resolution"
                    value="server"
                    checked={selectedResolution === 'server'}
                    onChange={() => setSelectedResolution('server')}
                    className="mt-1"
                  />
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-gray-900">
                        {t('offline.useServer')}
                      </span>
                      {selectedResolution === 'server' && (
                        <Check className="h-4 w-4 text-blue-600" />
                      )}
                    </div>
                    <p className="text-sm text-gray-600 mt-1">
                      Discard your local changes and use server data
                    </p>
                    <div className="mt-3 p-3 bg-gray-50 rounded border border-gray-200">
                      <pre className="text-xs text-gray-700 overflow-x-auto">
                        {formatData(conflict.serverData)}
                      </pre>
                    </div>
                  </div>
                </label>

                {/* Merge */}
                <label
                  className={`flex items-start gap-3 p-4 border-2 rounded-lg cursor-pointer transition-colors ${
                    selectedResolution === 'merge'
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  <input
                    type="radio"
                    name="resolution"
                    value="merge"
                    checked={selectedResolution === 'merge'}
                    onChange={() => setSelectedResolution('merge')}
                    className="mt-1"
                  />
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="font-medium text-gray-900">
                        {t('offline.merge')}
                      </span>
                      {selectedResolution === 'merge' && (
                        <Check className="h-4 w-4 text-blue-600" />
                      )}
                    </div>
                    <p className="text-sm text-gray-600 mt-1">
                      Combine both versions (local data takes precedence)
                    </p>
                  </div>
                </label>
              </div>

              {/* Conflict Type Info */}
              <div className="p-4 bg-gray-50 rounded-lg">
                <p className="text-sm text-gray-600">
                  <span className="font-medium">Conflict Type:</span>{' '}
                  <span className="capitalize">{conflict.conflictType}</span>
                </p>
              </div>
            </div>
          </div>

          {/* Footer */}
          <div className="bg-gray-50 border-t border-gray-200 px-6 py-4 flex justify-end gap-3">
            <button
              onClick={onCancel}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50"
            >
              {t('app.cancel')}
            </button>
            <button
              onClick={handleResolve}
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700"
            >
              {t('app.confirm')}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ConflictResolutionDialog;
