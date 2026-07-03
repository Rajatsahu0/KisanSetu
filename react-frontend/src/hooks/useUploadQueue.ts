import { useState, useEffect, useCallback } from 'react';
import { uploadQueueService, QueuedUpload, UploadHistoryItem } from '../services/uploadQueueService';
import { useOnlineStatus } from './useOnlineStatus';

export interface UseUploadQueueResult {
  addToQueue: (file: File, endpoint: string, farmerId: string, priority?: 'high' | 'medium' | 'low') => string;
  cancelUpload: (uploadId: string) => boolean;
  retryUpload: (uploadId: string) => boolean;
  getUpload: (uploadId: string) => QueuedUpload | undefined;
  queueStatus: {
    pending: number;
    uploading: number;
    completed: number;
    failed: number;
    total: number;
  };
  history: UploadHistoryItem[];
  clearHistory: () => void;
  isOnline: boolean;
}

export function useUploadQueue(): UseUploadQueueResult {
  const isOnline = useOnlineStatus();
  const [queueStatus, setQueueStatus] = useState(uploadQueueService.getQueueStatus());
  const [history, setHistory] = useState(uploadQueueService.getHistory());

  // Update queue status periodically
  useEffect(() => {
    const interval = setInterval(() => {
      setQueueStatus(uploadQueueService.getQueueStatus());
      setHistory(uploadQueueService.getHistory());
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  const addToQueue = useCallback(
    (file: File, endpoint: string, farmerId: string, priority: 'high' | 'medium' | 'low' = 'medium') => {
      const uploadId = uploadQueueService.addToQueue(file, endpoint, farmerId, priority);
      setQueueStatus(uploadQueueService.getQueueStatus());
      return uploadId;
    },
    []
  );

  const cancelUpload = useCallback((uploadId: string) => {
    const result = uploadQueueService.cancelUpload(uploadId);
    setQueueStatus(uploadQueueService.getQueueStatus());
    return result;
  }, []);

  const retryUpload = useCallback((uploadId: string) => {
    const result = uploadQueueService.retryUpload(uploadId);
    setQueueStatus(uploadQueueService.getQueueStatus());
    return result;
  }, []);

  const getUpload = useCallback((uploadId: string) => {
    return uploadQueueService.getUpload(uploadId);
  }, []);

  const clearHistory = useCallback(() => {
    uploadQueueService.clearHistory();
    setHistory([]);
  }, []);

  return {
    addToQueue,
    cancelUpload,
    retryUpload,
    getUpload,
    queueStatus,
    history,
    clearHistory,
    isOnline
  };
}
