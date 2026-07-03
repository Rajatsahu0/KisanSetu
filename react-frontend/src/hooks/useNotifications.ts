import { useCallback } from 'react';
import { useSelector } from 'react-redux';
import { useAppDispatch } from './useAppDispatch';
import { addNotification, removeNotification, clearNotifications } from '@/store/slices/appSlice';
import type { Notification } from '@/store/slices/appSlice';

/**
 * Hook for managing notifications
 */
export const useNotifications = () => {
  const dispatch = useAppDispatch();
  const notifications = useSelector((state: { app: { notifications: Notification[] } }) => 
    state.app.notifications
  );

  const showNotification = useCallback((
    notification: Omit<Notification, 'id' | 'timestamp'>
  ) => {
    // Auto-dismiss after duration (default: 4s for success/info, 6s for warning, 8s for error)
    const defaultDuration = notification.duration ?? (
      notification.type === 'error' ? 8000 :
      notification.type === 'warning' ? 6000 : 4000
    );
    dispatch(addNotification({ ...notification, duration: defaultDuration }));
  }, [dispatch]);

  const showSuccess = useCallback((title: string, message: string, duration?: number) => {
    showNotification({ type: 'success', title, message, duration });
  }, [showNotification]);

  const showError = useCallback((title: string, message: string, duration?: number) => {
    showNotification({ type: 'error', title, message, duration });
  }, [showNotification]);

  const showWarning = useCallback((title: string, message: string, duration?: number) => {
    showNotification({ type: 'warning', title, message, duration });
  }, [showNotification]);

  const showInfo = useCallback((title: string, message: string, duration?: number) => {
    showNotification({ type: 'info', title, message, duration });
  }, [showNotification]);

  const dismissNotification = useCallback((id: string) => {
    dispatch(removeNotification(id));
  }, [dispatch]);

  const clearAll = useCallback(() => {
    dispatch(clearNotifications());
  }, [dispatch]);

  return {
    notifications,
    showNotification,
    showSuccess,
    showError,
    showWarning,
    showInfo,
    dismissNotification,
    clearAll,
  };
};