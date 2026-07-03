import { User } from '../store/slices/authSlice';
import { apiClient } from './apiClient';
import { auth, RecaptchaVerifier, signInWithPhoneNumber, type ConfirmationResult } from '@/config/firebase';
import { signOut, onAuthStateChanged } from 'firebase/auth';

// Response DTOs (kept for interface compatibility)
export interface LoginRequest { phoneNumber: string; password: string; }
export interface LoginResponse { accessToken: string; refreshToken: string; idToken: string; expiresIn: number; tokenType: string; }
export interface RefreshRequest { refreshToken: string; }
export interface RefreshResponse { accessToken: string; idToken: string; expiresIn: number; tokenType: string; }
export interface ValidateResponse { isValid: boolean; userId: string; phoneNumber?: string; claims?: Record<string, string>; }
export interface RegisterRequest { phoneNumber: string; password: string; name: string; }
export interface RegisterResponse { message: string; userId?: string; requiresConfirmation: boolean; }
export interface ConfirmRequest { phoneNumber: string; confirmationCode: string; password?: string; }
export interface ConfirmResponse { message: string; accessToken?: string; refreshToken?: string; idToken?: string; expiresIn?: number; }

class AuthService {
  private confirmationResult: ConfirmationResult | null = null;
  private recaptchaVerifier: RecaptchaVerifier | null = null;

  constructor() {
    // Auto-refresh token on auth state change
    onAuthStateChanged(auth, async (user) => {
      if (user) {
        const token = await user.getIdToken();
        this.setTokens(token, 'firebase-managed', 3600);
      }
    });
  }

  /**
   * Initialize invisible reCAPTCHA (required before sending OTP)
   */
  initRecaptcha(elementId: string = 'recaptcha-container'): void {
    // Only create once — reuse existing verifier
    if (this.recaptchaVerifier) return;

    this.recaptchaVerifier = new RecaptchaVerifier(auth, elementId, { size: 'invisible' });
  }

  /**
   * Register = Send OTP to phone (Firebase phone auth is registration + login in one)
   */
  async register(phoneNumber: string, _password: string, _name: string): Promise<RegisterResponse> {
    await this.sendOtp(phoneNumber);
    return { message: 'OTP sent to your phone', requiresConfirmation: true, userId: undefined };
  }

  /**
   * Login = Send OTP to phone
   */
  async login(phoneNumber: string, _password: string): Promise<LoginResponse> {
    await this.sendOtp(phoneNumber);
    // Return a placeholder — actual tokens come after OTP verification
    return {
      accessToken: '',
      refreshToken: '',
      idToken: '',
      expiresIn: 3600,
      tokenType: 'Bearer'
    };
  }

  /**
   * Send OTP via Firebase (used by both register and login)
   */
  private async sendOtp(phoneNumber: string): Promise<void> {
    if (!this.recaptchaVerifier) {
      this.initRecaptcha();
    }
    this.confirmationResult = await signInWithPhoneNumber(auth, phoneNumber, this.recaptchaVerifier!);
  }

  /**
   * Confirm OTP code — this completes the sign-in and returns tokens
   */
  async confirmRegistration(phoneNumber: string, confirmationCode: string, _password?: string): Promise<ConfirmResponse> {
    if (!this.confirmationResult) {
      throw new Error('No pending OTP. Call register or login first.');
    }

    const result = await this.confirmationResult.confirm(confirmationCode);
    const user = result.user;
    const idToken = await user.getIdToken();

    // Store tokens
    this.setTokens(idToken, 'firebase-managed', 3600);
    this.setUser({
      farmerId: user.uid,
      phoneNumber: user.phoneNumber || phoneNumber,
      name: user.displayName || 'Farmer',
      isVerified: true
    } as User);

    // Create profile in backend (Firestore) — fire and forget
    try {
      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${idToken}`
      };
      const baseURL = apiClient.getBaseURL();
      await fetch(`${baseURL}/api/v1/profile`, {
        method: 'PUT',
        headers,
        body: JSON.stringify({
          name: user.displayName || 'Farmer',
          phoneNumber: user.phoneNumber || phoneNumber
        })
      });
    } catch (err) {
      console.warn('Profile creation failed (will retry on next access):', err);
    }

    return {
      message: 'Phone verified successfully',
      accessToken: idToken,
      refreshToken: 'firebase-managed',
      idToken: idToken,
      expiresIn: 3600
    };
  }

  /**
   * Resend OTP
   */
  async resendOtp(phoneNumber: string): Promise<{ message: string }> {
    await this.sendOtp(phoneNumber);
    return { message: 'OTP resent successfully' };
  }

  /**
   * Refresh token — Firebase handles this automatically
   */
  async refreshToken(_refreshToken: string): Promise<RefreshResponse> {
    const user = auth.currentUser;
    if (!user) throw new Error('No authenticated user');
    const token = await user.getIdToken(true); // force refresh
    this.setTokens(token, 'firebase-managed', 3600);
    return { accessToken: token, idToken: token, expiresIn: 3600, tokenType: 'Bearer' };
  }

  /**
   * Validate token
   */
  async validateToken(accessToken: string): Promise<ValidateResponse> {
    const user = auth.currentUser;
    if (user && accessToken) {
      return { isValid: true, userId: user.uid, phoneNumber: user.phoneNumber || undefined };
    }
    return { isValid: false, userId: '' };
  }

  /**
   * Update preferences
   */
  async updatePreferences(preferredLanguage?: string, preferredDialect?: string): Promise<boolean> {
    try {
      const token = this.getAccessToken();
      if (!token) return false;
      await apiClient.put('/api/v1/auth/preferences', { preferredLanguage, preferredDialect });
      return true;
    } catch { return false; }
  }

  /**
   * Decode ID token
   */
  decodeIdToken(idToken: string): Record<string, any> | null {
    try {
      const parts = idToken.split('.');
      if (parts.length !== 3) return null;
      const payload = parts[1];
      const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64.padEnd(base64.length + (4 - base64.length % 4) % 4, '=');
      return JSON.parse(atob(padded));
    } catch { return null; }
  }

  // ─── Token Storage ───

  setTokens(accessToken: string, refreshToken: string, expiresIn: number): void {
    const expiresAt = Date.now() + expiresIn * 1000;
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    localStorage.setItem('expiresAt', expiresAt.toString());
  }

  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  getTokenExpiry(): number | null {
    const expiresAt = localStorage.getItem('expiresAt');
    return expiresAt ? parseInt(expiresAt, 10) : null;
  }

  isTokenExpired(): boolean {
    const expiresAt = this.getTokenExpiry();
    if (!expiresAt) return true;
    return Date.now() >= expiresAt;
  }

  clearTokens(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('expiresAt');
    localStorage.removeItem('user');
    localStorage.removeItem('voiceQueryHistory');
    localStorage.removeItem('savedPlantingRecommendations');
    localStorage.removeItem('savedGradings');
    localStorage.removeItem('savedRegenerativePlans');
  }

  setUser(user: User): void {
    localStorage.setItem('user', JSON.stringify(user));
  }

  getUser(): User | null {
    const userStr = localStorage.getItem('user');
    if (!userStr) return null;
    try { return JSON.parse(userStr); } catch { return null; }
  }

  clearUser(): void {
    localStorage.removeItem('user');
  }

  /**
   * Logout — sign out from Firebase
   */
  async logout(): Promise<void> {
    await signOut(auth);
    this.clearTokens();
    this.clearUser();
  }
}

export const authService = new AuthService();
