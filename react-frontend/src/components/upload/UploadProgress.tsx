import React from 'react';
import { CheckCircle, XCircle, Loader2, RefreshCw } from 'lucide-react';

export type UploadStatus = 'uploading' | 'processing' | 'complete' | 'error';

export interface UploadProgressProps {
  progress: number;
  status: UploadStatus;
  fileName?: string;
  onRetry?: () => void;
  onCancel?: () => void;
  errorMessage?: string;
}

export const UploadProgress: React.FC<UploadProgressProps> = ({
  progress,
  status,
  fileName,
  onRetry,
  onCancel,
  errorMessage
}) => {
  const getStatusColor = () => {
    switch (status) {
      case 'uploading':
      case 'processing':
        return 'bg-blue-500';
      case 'complete':
        return 'bg-green-500';
      case 'error':
        return 'bg-red-500';
      default:
        return 'bg-gray-500';
    }
  };

  const getStatusIcon = () => {
    switch (status) {
      case 'uploading':
      case 'processing':
        return <Loader2 className="w-5 h-5 text-blue-500 animate-spin" />;
      case 'complete':
        return <CheckCircle className="w-5 h-5 text-green-500" />;
      case 'error':
        return <XCircle className="w-5 h-5 text-red-500" />;
      default:
        return null;
    }
  };

  const getStatusText = () => {
    switch (status) {
      case 'uploading':
        return 'Uploading...';
      case 'processing':
        return 'Processing...';
      case 'complete':
        return 'Upload complete';
      case 'error':
        return 'Upload failed';
      default:
        return '';
    }
  };

  return (
    <div className="w-full p-4 bg-white border border-gray-200 rounded-lg shadow-sm">
      {/* Header */}
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center space-x-3">
          {getStatusIcon()}
          <div className="flex-1 min-w-0">
            {fileName && (
              <p className="text-sm font-medium text-gray-900 truncate">
                {fileName}
              </p>
            )}
            <p className="text-xs text-gray-500">{getStatusText()}</p>
          </div>
        </div>

        {/* Progress percentage */}
        {(status === 'uploading' || status === 'processing') && (
          <span className="text-sm font-medium text-gray-700">
            {Math.round(progress)}%
          </span>
        )}
      </div>

      {/* Progress bar */}
      {(status === 'uploading' || status === 'processing') && (
        <div className="w-full bg-gray-200 rounded-full h-2 mb-3">
          <div
            className={`h-2 rounded-full transition-all duration-300 ${getStatusColor()}`}
            style={{ width: `${progress}%` }}
          />
        </div>
      )}

      {/* Error message */}
      {status === 'error' && errorMessage && (
        <div className="mb-3 p-2 bg-red-50 border border-red-200 rounded-md">
          <p className="text-xs text-red-600">{errorMessage}</p>
        </div>
      )}

      {/* Action buttons */}
      <div className="flex items-center justify-end space-x-2">
        {status === 'error' && onRetry && (
          <button
            onClick={onRetry}
            className="inline-flex items-center px-3 py-1.5 text-sm font-medium text-blue-600 bg-blue-50 border border-blue-200 rounded-md hover:bg-blue-100 transition-colors"
          >
            <RefreshCw className="w-4 h-4 mr-1" />
            Retry
          </button>
        )}

        {(status === 'uploading' || status === 'processing') && onCancel && (
          <button
            onClick={onCancel}
            className="inline-flex items-center px-3 py-1.5 text-sm font-medium text-gray-600 bg-gray-50 border border-gray-200 rounded-md hover:bg-gray-100 transition-colors"
          >
            Cancel
          </button>
        )}
      </div>
    </div>
  );
};

export default UploadProgress;
