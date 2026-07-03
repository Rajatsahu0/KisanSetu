import React, { createContext, useContext, useEffect, useCallback, useRef, useState } from 'react';
import { useAppDispatch, useAppSelector } from '../hooks';
import {
  loginStart,
  loginSuccess,
  loginFailure,
  logout as logoutAction,
  updateTokens,
} from '../store/slices/authSlice';
import { setLanguage } from '../store/slices/appSlice';
import { authService } from '../services/authService';
import type { User } from '../store/slices/authSlice';
import { SessionTimeoutWarning } from '../components/auth/SessionTimeoutWarning';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  login: (phoneNumber: string, password: string) => Promise<void>;
  logout: () => void;
  refreshToken: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: React.ReactNode;
}

const INACTIVITY_TIMEOUT = 30 * 60 * 1000; // 30 minutes in milliseconds
const WARNING_BEFORE_TIMEOUT = 2 * 60 * 1000; // Show warning 2 minutes before timeout
const TOKEN_REFRESH_BUFFER = 5 * 60 * 1000; // Refresh 5 minutes before expiry

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const dispatch = useAppDispatch();
  const authState = useAppSelector((state) => state.auth);
  
  // Destructure with fallback values to prevent undefined errors
  const { 
    user = null, 
    isAuthenticated = false, 
    isLoading = false, 
    error = null, 
    tokens = { accessToken: null, refreshToken: null, expiresAt: null }
  } = authState || {};

  const inactivityTimerRef = useRef<NodeJS.Timeout | null>(null);
  const warningTimerRef = useRef<NodeJS.Timeout | null>(null);
  const refreshTimerRef = useRef<NodeJS.Timeout | null>(null);
  const [showTimeoutWarning, setShowTimeoutWarning] = useState(false);
  const [warningSecondsRemaining, setWarningSecondsRemaining] = useState(0);

  // Reset inactivity timer
  const resetInactivityTimer = useCallback(() => {
    if (inactivityTimerRef.current) {
      clearTimeout(inactivityTimerRef.current);
    }
    if (warningTimerRef.current) {
      clearTimeout(warningTimerRef.current);
    }
    setShowTimeoutWarning(false);

    if (isAuthenticated) {
      // Set warning timer
      warningTimerRef.current = setTimeout(() => {
        setShowTimeoutWarning(true);
        setWarningSecondsRemaining(WARNING_BEFORE_TIMEOUT / 1000);
      }, INACTIVITY_TIMEOUT - WARNING_BEFORE_TIMEOUT);

      // Set logout timer
      inactivityTimerRef.current = setTimeout(() => {

        logout();
      }, INACTIVITY_TIMEOUT);
    }
  }, [isAuthenticated]);

  // Setup activity listeners
  useEffect(() => {
    if (!isAuthenticated) return;

    const events = ['mousedown', 'keydown', 'scroll', 'touchstart', 'click'];
    
    events.forEach((event) => {
      document.addEventListener(event, resetInactivityTimer);
    });

    resetInactivityTimer();

    return () => {
      events.forEach((event) => {
        document.removeEventListener(event, resetInactivityTimer);
      });
      if (inactivityTimerRef.current) {
        clearTimeout(inactivityTimerRef.current);
      }
      if (warningTimerRef.current) {
        clearTimeout(warningTimerRef.current);
      }
    };
  }, [isAuthenticated, resetInactivityTimer]);

  // Automatic token refresh
  const scheduleTokenRefresh = useCallback(() => {
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current);
    }

    const expiresAt = tokens.expiresAt;
    if (!expiresAt || !tokens.refreshToken) return;

    const timeUntilRefresh = expiresAt - Date.now() - TOKEN_REFRESH_BUFFER;

    if (timeUntilRefresh > 0) {
      refreshTimerRef.current = setTimeout(async () => {
        try {
          await refreshToken();
        } catch (error) {
          console.error('Failed to refresh token:', error);
          logout();
        }
      }, timeUntilRefresh);
    } else {
      // Token already expired or about to expire, refresh immediately
      refreshToken().catch(() => logout());
    }
  }, [tokens.expiresAt, tokens.refreshToken]);

  useEffect(() => {
    if (isAuthenticated && tokens.expiresAt) {
      scheduleTokenRefresh();
    }

    return () => {
      if (refreshTimerRef.current) {
        clearTimeout(refreshTimerRef.current);
      }
    };
  }, [isAuthenticated, tokens.expiresAt, scheduleTokenRefresh]);

  // Initialize auth state from storage on mount
  useEffect(() => {
    const initializeAuth = async () => {
      const storedUser = authService.getUser();
      const accessToken = authService.getAccessToken();
      const refreshTokenValue = authService.getRefreshToken();
      const expiresAt = authService.getTokenExpiry();

      if (storedUser && accessToken && refreshTokenValue && expiresAt) {
        // Check if token is expired
        if (authService.isTokenExpired()) {
          // Try to refresh the token
          try {
            dispatch(loginStart());
            const response = await authService.refreshToken(refreshTokenValue);
            const newExpiresAt = Date.now() + response.expiresIn * 1000;
            
            authService.setTokens(response.accessToken, refreshTokenValue, response.expiresIn);
            
            dispatch(
              loginSuccess({
                user: storedUser,
                tokens: {
                  accessToken: response.accessToken,
                  refreshToken: refreshTokenValue,
                  expiresAt: newExpiresAt,
                },
              })
            );
          } catch (error) {
            // Refresh failed, clear everything including persisted state
            authService.clearTokens();
            authService.clearUser();
            localStorage.removeItem('persist:root');
            dispatch(logoutAction());
          }
        } else {
          // Token still valid, restore session
          dispatch(
            loginSuccess({
              user: storedUser,
              tokens: {
                accessToken,
                refreshToken: refreshTokenValue,
                expiresAt,
              },
            })
          );
        }
      }
    };

    initializeAuth();
  }, [dispatch]);

  const login = async (phoneNumber: string, password: string): Promise<void> => {
    dispatch(loginStart());

    try {
      // Use Firebase phone authentication
      const response = await authService.login(phoneNumber, password);
      
      // Firebase login just sends OTP — tokens come after confirmation
      // Don't store empty tokens
      if (!response.accessToken) {
        // OTP sent, waiting for confirmation — don't dispatch loginSuccess yet
        return;
      }
      
      // Store tokens (only if we have real ones)
      authService.setTokens(response.accessToken, response.refreshToken, response.expiresIn);
      
      // Decode ID token to get user claims (name, etc.)
      const idTokenPayload = authService.decodeIdToken(response.idToken);
      
      // Validate access token to get user info
      const validateResponse = await authService.validateToken(response.accessToken);
      
      const user: User = {
        id: validateResponse.userId,
        phoneNumber: validateResponse.phoneNumber || phoneNumber,
        name: idTokenPayload?.name || validateResponse.claims?.name || '',
        preferredLanguage: idTokenPayload?.['custom:preferred_language'] || validateResponse.claims?.['custom:preferred_language'] || 'en',
      };

      authService.setUser(user);

      const expiresAt = Date.now() + response.expiresIn * 1000;

      // Sync Cognito language preference to app language (drives i18n + UI)
      if (user.preferredLanguage && user.preferredLanguage !== 'en') {
        dispatch(setLanguage(user.preferredLanguage));
      }

      dispatch(
        loginSuccess({
          user,
          tokens: {
            accessToken: response.accessToken,
            refreshToken: response.refreshToken,
            expiresAt,
          },
        })
      );
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Login failed';
      dispatch(loginFailure(errorMessage));
      throw error;
    }
  };

  const logout = useCallback(() => {
    authService.clearTokens();
    authService.clearUser();
    dispatch(logoutAction());
    setShowTimeoutWarning(false);
    
    if (inactivityTimerRef.current) {
      clearTimeout(inactivityTimerRef.current);
    }
    if (warningTimerRef.current) {
      clearTimeout(warningTimerRef.current);
    }
    if (refreshTimerRef.current) {
      clearTimeout(refreshTimerRef.current);
    }
  }, [dispatch]);

  const handleExtendSession = useCallback(() => {
    setShowTimeoutWarning(false);
    resetInactivityTimer();
  }, [resetInactivityTimer]);

  const refreshToken = async (): Promise<void> => {
    const refreshTokenValue = authService.getRefreshToken();
    
    if (!refreshTokenValue) {
      throw new Error('No refresh token available');
    }

    try {
      const response = await authService.refreshToken(refreshTokenValue);
      const newExpiresAt = Date.now() + response.expiresIn * 1000;
      
      authService.setTokens(response.accessToken, refreshTokenValue, response.expiresIn);
      
      dispatch(
        updateTokens({
          accessToken: response.accessToken,
          refreshToken: refreshTokenValue,
          expiresAt: newExpiresAt,
        })
      );
    } catch (error) {
      console.error('Token refresh failed:', error);
      throw error;
    }
  };

  const value: AuthContextType = {
    user,
    isAuthenticated,
    isLoading,
    error,
    login,
    logout,
    refreshToken,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
      {showTimeoutWarning && (
        <SessionTimeoutWarning
          remainingSeconds={warningSecondsRemaining}
          onExtendSession={handleExtendSession}
          onLogout={logout}
        />
      )}
    </AuthContext.Provider>
  );
};
