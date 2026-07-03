/**
 * Firebase Phone Authentication Service
 * Replaces Cognito-based auth with Firebase phone OTP.
 * 
 * Flow:
 * 1. User enters phone number → sendOtp() → Firebase sends SMS
 * 2. User enters OTP code → verifyOtp() → Firebase returns ID token
 * 3. ID token is sent to backend in Authorization header
 */

import { auth, RecaptchaVerifier, signInWithPhoneNumber, type ConfirmationResult } from '@/config/firebase';
import { signOut, onAuthStateChanged, type User } from 'firebase/auth';

class FirebaseAuthService {
  private confirmationResult: ConfirmationResult | null = null;
  private recaptchaVerifier: RecaptchaVerifier | null = null;
  private currentUser: User | null = null;

  constructor() {
    // Listen for auth state changes
    onAuthStateChanged(auth, (user) => {
      this.currentUser = user;
      if (user) {
        // Store token for API calls
        user.getIdToken().then(token => {
          localStorage.setItem('access_token', token);
        });
      } else {
        localStorage.removeItem('access_token');
      }
    });
  }

  /**
   * Initialize reCAPTCHA verifier (required for phone auth)
   * Call this before sendOtp — attaches to an invisible reCAPTCHA element
   */
  initRecaptcha(elementId: string = 'recaptcha-container'): void {
    if (this.recaptchaVerifier) return;
    
    this.recaptchaVerifier = new RecaptchaVerifier(auth, elementId, {
      size: 'invisible',
      callback: () => {
        // reCAPTCHA solved
      }
    });
  }

  /**
   * Send OTP to phone number
   * Phone must be in E.164 format: +919876543210
   */
  async sendOtp(phoneNumber: string): Promise<void> {
    if (!this.recaptchaVerifier) {
      this.initRecaptcha();
    }

    try {
      this.confirmationResult = await signInWithPhoneNumber(
        auth, phoneNumber, this.recaptchaVerifier!
      );
    } catch (error: any) {
      console.error('Firebase sendOtp error:', error);
      throw new Error(error.message || 'Failed to send OTP');
    }
  }

  /**
   * Verify OTP code entered by user
   * Returns the Firebase ID token (used as Bearer token for backend)
   */
  async verifyOtp(code: string): Promise<{ accessToken: string; user: User }> {
    if (!this.confirmationResult) {
      throw new Error('No pending OTP verification. Call sendOtp first.');
    }

    try {
      const result = await this.confirmationResult.confirm(code);
      const user = result.user;
      const token = await user.getIdToken();

      localStorage.setItem('access_token', token);
      localStorage.setItem('user_phone', user.phoneNumber || '');
      localStorage.setItem('user_id', user.uid);

      return { accessToken: token, user };
    } catch (error: any) {
      console.error('Firebase verifyOtp error:', error);
      throw new Error(error.code === 'auth/invalid-verification-code' 
        ? 'Invalid OTP code. Please try again.'
        : error.message || 'OTP verification failed');
    }
  }

  /**
   * Get current access token (Firebase ID token)
   * Auto-refreshes if expired
   */
  getAccessToken(): string | null {
    return localStorage.getItem('access_token');
  }

  /**
   * Refresh the ID token (Firebase handles this automatically)
   */
  async refreshToken(): Promise<string | null> {
    if (this.currentUser) {
      const token = await this.currentUser.getIdToken(true); // force refresh
      localStorage.setItem('access_token', token);
      return token;
    }
    return null;
  }

  /**
   * Check if token is expired
   */
  isTokenExpired(): boolean {
    // Firebase tokens expire after 1 hour
    // The SDK auto-refreshes, but check if we have a user
    return !this.currentUser;
  }

  /**
   * Get current user info
   */
  getUser() {
    return this.currentUser ? {
      uid: this.currentUser.uid,
      phoneNumber: this.currentUser.phoneNumber,
      displayName: this.currentUser.displayName
    } : null;
  }

  /**
   * Logout
   */
  async logout(): Promise<void> {
    await signOut(auth);
    localStorage.removeItem('access_token');
    localStorage.removeItem('user_phone');
    localStorage.removeItem('user_id');
    localStorage.removeItem('refresh_token');
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    return !!this.currentUser || !!localStorage.getItem('access_token');
  }

  // ─── Compatibility methods (matching old authService interface) ───

  getRefreshToken(): string | null {
    return 'firebase-auto-refresh'; // Firebase handles refresh internally
  }

  setTokens(accessToken: string, _refreshToken: string, _expiresIn: number): void {
    localStorage.setItem('access_token', accessToken);
  }

  clearTokens(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  }

  clearUser(): void {
    localStorage.removeItem('user_phone');
    localStorage.removeItem('user_id');
  }
}

export const firebaseAuthService = new FirebaseAuthService();
