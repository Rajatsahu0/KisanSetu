import { useEffect } from 'react';
import { useAppDispatch } from './useAppDispatch';
import { setOnlineStatus } from '@/store/slices/appSlice';

/**
 * Hook to monitor online/offline status and update Redux store
 */
export const useOnlineStatus = () => {
  const dispatch = useAppDispatch();

  useEffect(() => {
    const handleOnline = () => dispatch(setOnlineStatus(true));
    const handleOffline = () => dispatch(setOnlineStatus(false));

    // Set initial status
    dispatch(setOnlineStatus(navigator.onLine));

    // Listen for online/offline events
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, [dispatch]);
};