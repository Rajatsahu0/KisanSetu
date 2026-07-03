import { createSlice, PayloadAction } from '@reduxjs/toolkit';

export interface QueuedRequest {
  id: string;
  method: 'GET' | 'POST' | 'PUT' | 'DELETE';
  url: string;
  data?: any;
  files?: File[];
  timestamp: number;
  retryCount: number;
  priority: 'high' | 'medium' | 'low';
}

export interface CachedData {
  key: string;
  data: any;
  timestamp: number;
  expiresAt: number;
  version: number;
}

interface OfflineState {
  queuedRequests: QueuedRequest[];
  cachedData: Record<string, CachedData>;
  syncStatus: 'idle' | 'syncing' | 'error';
  lastSyncTime: number | null;
}

const initialState: OfflineState = {
  queuedRequests: [],
  cachedData: {},
  syncStatus: 'idle',
  lastSyncTime: null,
};

const offlineSlice = createSlice({
  name: 'offline',
  initialState,
  reducers: {
    addQueuedRequest: (state, action: PayloadAction<Omit<QueuedRequest, 'id' | 'timestamp' | 'retryCount'>>) => {
      const request: QueuedRequest = {
        ...action.payload,
        id: Date.now().toString(),
        timestamp: Date.now(),
        retryCount: 0,
      };
      state.queuedRequests.push(request);
    },
    removeQueuedRequest: (state, action: PayloadAction<string>) => {
      state.queuedRequests = state.queuedRequests.filter(
        (request) => request.id !== action.payload
      );
    },
    incrementRetryCount: (state, action: PayloadAction<string>) => {
      const request = state.queuedRequests.find((req) => req.id === action.payload);
      if (request) {
        request.retryCount += 1;
      }
    },
    setCachedData: (state, action: PayloadAction<CachedData>) => {
      state.cachedData[action.payload.key] = action.payload;
    },
    removeCachedData: (state, action: PayloadAction<string>) => {
      delete state.cachedData[action.payload];
    },
    clearExpiredCache: (state) => {
      const now = Date.now();
      Object.keys(state.cachedData).forEach((key) => {
        if (state.cachedData[key].expiresAt < now) {
          delete state.cachedData[key];
        }
      });
    },
    setSyncStatus: (state, action: PayloadAction<'idle' | 'syncing' | 'error'>) => {
      state.syncStatus = action.payload;
    },
    setLastSyncTime: (state, action: PayloadAction<number>) => {
      state.lastSyncTime = action.payload;
    },
    clearOfflineData: (state) => {
      state.queuedRequests = [];
      state.cachedData = {};
      state.syncStatus = 'idle';
      state.lastSyncTime = null;
    },
  },
});

export const {
  addQueuedRequest,
  removeQueuedRequest,
  incrementRetryCount,
  setCachedData,
  removeCachedData,
  clearExpiredCache,
  setSyncStatus,
  setLastSyncTime,
  clearOfflineData,
} = offlineSlice.actions;

export default offlineSlice.reducer;