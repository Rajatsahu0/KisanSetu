import { useState, useCallback, useRef } from 'react';
import { uploadService, UploadResult, BatchUploadResult } from '../services/uploadService';

export interface UploadState {
  isUploading: boolean;
  progress: number;
  error: string | null;
  result: UploadResult | null;
}

export interface BatchUploadState {
  isUploading: boolean;
  progress: number;
  error: string | null;
  results: BatchUploadResult | null;
}

export interface UseFileUploadOptions {
  endpoint: string;
  onSuccess?: (result: UploadResult) => void;
  onError?: (error: Error) => void;
  retryAttempts?: number;
  retryDelay?: number;
}

export function useFileUpload(options: UseFileUploadOptions) {
  const { endpoint, onSuccess, onError, retryAttempts, retryDelay } = options;

  const [state, setState] = useState<UploadState>({
    isUploading: false,
    progress: 0,
    error: null,
    result: null
  });

  const abortControllerRef = useRef<AbortController | null>(null);

  const uploadFile = useCallback(
    async (file: File) => {
      // Reset state
      setState({
        isUploading: true,
        progress: 0,
        error: null,
        result: null
      });

      // Create abort controller
      abortControllerRef.current = new AbortController();

      try {
        const result = await uploadService.uploadFile(file, endpoint, {
          onProgress: (progress) => {
            setState(prev => ({ ...prev, progress }));
          },
          onError: (error) => {
            setState(prev => ({
              ...prev,
              isUploading: false,
              error: error.message
            }));
            onError?.(error);
          },
          onComplete: (result) => {
            setState(prev => ({
              ...prev,
              isUploading: false,
              progress: 100,
              result
            }));
            onSuccess?.(result);
          },
          signal: abortControllerRef.current.signal,
          retryAttempts,
          retryDelay
        });

        return result;
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Upload failed';
        setState({
          isUploading: false,
          progress: 0,
          error: errorMessage,
          result: null
        });
        throw error;
      }
    },
    [endpoint, onSuccess, onError, retryAttempts, retryDelay]
  );

  const cancelUpload = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }
    setState({
      isUploading: false,
      progress: 0,
      error: 'Upload cancelled',
      result: null
    });
  }, []);

  const resetState = useCallback(() => {
    setState({
      isUploading: false,
      progress: 0,
      error: null,
      result: null
    });
  }, []);

  const retryUpload = useCallback(
    async (file: File) => {
      return uploadFile(file);
    },
    [uploadFile]
  );

  return {
    uploadFile,
    cancelUpload,
    resetState,
    retryUpload,
    ...state
  };
}

export interface UseBatchUploadOptions {
  endpoint: string;
  onSuccess?: (results: BatchUploadResult) => void;
  onError?: (error: Error) => void;
  onProgress?: (progress: number) => void;
}

export function useBatchUpload(options: UseBatchUploadOptions) {
  const { endpoint, onSuccess, onError, onProgress } = options;

  const [state, setState] = useState<BatchUploadState>({
    isUploading: false,
    progress: 0,
    error: null,
    results: null
  });

  const uploadFiles = useCallback(
    async (files: File[]) => {
      setState({
        isUploading: true,
        progress: 0,
        error: null,
        results: null
      });

      try {
        let completedFiles = 0;
        const totalFiles = files.length;

        const results = await uploadService.uploadBatch(files, endpoint, {
          onProgress: () => {
            completedFiles++;
            const progress = (completedFiles / totalFiles) * 100;
            setState(prev => ({ ...prev, progress }));
            onProgress?.(progress);
          },
          onError: (error) => {
            onError?.(error);
          }
        });

        setState({
          isUploading: false,
          progress: 100,
          error: null,
          results
        });

        onSuccess?.(results);
        return results;
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Batch upload failed';
        setState({
          isUploading: false,
          progress: 0,
          error: errorMessage,
          results: null
        });
        throw error;
      }
    },
    [endpoint, onSuccess, onError, onProgress]
  );

  const resetState = useCallback(() => {
    setState({
      isUploading: false,
      progress: 0,
      error: null,
      results: null
    });
  }, []);

  return {
    uploadFiles,
    resetState,
    ...state
  };
}

export interface UseLargeFileUploadOptions {
  endpoint: string;
  onSuccess?: (result: UploadResult) => void;
  onError?: (error: Error) => void;
}

export function useLargeFileUpload(options: UseLargeFileUploadOptions) {
  const { endpoint, onSuccess, onError } = options;

  const [state, setState] = useState<UploadState>({
    isUploading: false,
    progress: 0,
    error: null,
    result: null
  });

  const abortControllerRef = useRef<AbortController | null>(null);

  const uploadLargeFile = useCallback(
    async (file: File) => {
      setState({
        isUploading: true,
        progress: 0,
        error: null,
        result: null
      });

      abortControllerRef.current = new AbortController();

      try {
        const result = await uploadService.uploadLargeFile(file, endpoint, {
          onProgress: (progress) => {
            setState(prev => ({ ...prev, progress }));
          },
          onError: (error) => {
            setState(prev => ({
              ...prev,
              isUploading: false,
              error: error.message
            }));
            onError?.(error);
          },
          onComplete: (result) => {
            setState(prev => ({
              ...prev,
              isUploading: false,
              progress: 100,
              result
            }));
            onSuccess?.(result);
          },
          signal: abortControllerRef.current.signal
        });

        return result;
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Upload failed';
        setState({
          isUploading: false,
          progress: 0,
          error: errorMessage,
          result: null
        });
        throw error;
      }
    },
    [endpoint, onSuccess, onError]
  );

  const cancelUpload = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
      abortControllerRef.current = null;
    }
    setState({
      isUploading: false,
      progress: 0,
      error: 'Upload cancelled',
      result: null
    });
  }, []);

  const resumeUpload = useCallback(
    async (file: File) => {
      return uploadLargeFile(file);
    },
    [uploadLargeFile]
  );

  return {
    uploadLargeFile,
    cancelUpload,
    resumeUpload,
    ...state
  };
}
