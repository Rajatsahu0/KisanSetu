/**
 * Client-side encryption utilities for sensitive data
 * Note: This provides basic client-side encryption. Server-side encryption is the primary security layer.
 */

/**
 * Generate a cryptographic key from a password
 */
async function deriveKey(password: string, salt: Uint8Array): Promise<CryptoKey> {
  const encoder = new TextEncoder();
  const passwordBuffer = encoder.encode(password);
  
  // Import password as key material
  const keyMaterial = await crypto.subtle.importKey(
    'raw',
    passwordBuffer,
    'PBKDF2',
    false,
    ['deriveBits', 'deriveKey']
  );
  
  // Derive key using PBKDF2
  return crypto.subtle.deriveKey(
    {
      name: 'PBKDF2',
      salt,
      iterations: 100000,
      hash: 'SHA-256',
    },
    keyMaterial,
    { name: 'AES-GCM', length: 256 },
    false,
    ['encrypt', 'decrypt']
  );
}

/**
 * Encrypt data using AES-GCM
 */
export async function encryptData(
  data: string,
  password: string
): Promise<{ encrypted: string; salt: string; iv: string }> {
  try {
    // Generate random salt and IV
    const salt = crypto.getRandomValues(new Uint8Array(16));
    const iv = crypto.getRandomValues(new Uint8Array(12));
    
    // Derive encryption key
    const key = await deriveKey(password, salt);
    
    // Encrypt data
    const encoder = new TextEncoder();
    const dataBuffer = encoder.encode(data);
    
    const encryptedBuffer = await crypto.subtle.encrypt(
      { name: 'AES-GCM', iv },
      key,
      dataBuffer
    );
    
    // Convert to base64 for storage
    const encryptedArray = new Uint8Array(encryptedBuffer);
    const encrypted = btoa(String.fromCharCode(...encryptedArray));
    const saltBase64 = btoa(String.fromCharCode(...salt));
    const ivBase64 = btoa(String.fromCharCode(...iv));
    
    return { encrypted, salt: saltBase64, iv: ivBase64 };
  } catch (error) {
    console.error('Encryption failed:', error);
    throw new Error('Failed to encrypt data');
  }
}

/**
 * Decrypt data using AES-GCM
 */
export async function decryptData(
  encrypted: string,
  password: string,
  saltBase64: string,
  ivBase64: string
): Promise<string> {
  try {
    // Convert from base64
    const encryptedArray = Uint8Array.from(atob(encrypted), c => c.charCodeAt(0));
    const salt = Uint8Array.from(atob(saltBase64), c => c.charCodeAt(0));
    const iv = Uint8Array.from(atob(ivBase64), c => c.charCodeAt(0));
    
    // Derive decryption key
    const key = await deriveKey(password, salt);
    
    // Decrypt data
    const decryptedBuffer = await crypto.subtle.decrypt(
      { name: 'AES-GCM', iv },
      key,
      encryptedArray
    );
    
    // Convert to string
    const decoder = new TextDecoder();
    return decoder.decode(decryptedBuffer);
  } catch (error) {
    console.error('Decryption failed:', error);
    throw new Error('Failed to decrypt data');
  }
}

/**
 * Hash data using SHA-256
 */
export async function hashData(data: string): Promise<string> {
  try {
    const encoder = new TextEncoder();
    const dataBuffer = encoder.encode(data);
    
    const hashBuffer = await crypto.subtle.digest('SHA-256', dataBuffer);
    const hashArray = new Uint8Array(hashBuffer);
    
    // Convert to hex string
    return Array.from(hashArray)
      .map(b => b.toString(16).padStart(2, '0'))
      .join('');
  } catch (error) {
    console.error('Hashing failed:', error);
    throw new Error('Failed to hash data');
  }
}

/**
 * Generate a secure random token
 */
export function generateSecureToken(length: number = 32): string {
  const array = new Uint8Array(length);
  crypto.getRandomValues(array);
  return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
}

/**
 * Encrypt sensitive form data before transmission
 */
export async function encryptFormData(
  formData: Record<string, any>,
  sessionKey: string
): Promise<string> {
  const jsonData = JSON.stringify(formData);
  const { encrypted, salt, iv } = await encryptData(jsonData, sessionKey);
  
  // Combine encrypted data with metadata
  return JSON.stringify({ encrypted, salt, iv });
}

/**
 * Decrypt form data received from server
 */
export async function decryptFormData(
  encryptedPayload: string,
  sessionKey: string
): Promise<Record<string, any>> {
  const { encrypted, salt, iv } = JSON.parse(encryptedPayload);
  const decrypted = await decryptData(encrypted, sessionKey, salt, iv);
  return JSON.parse(decrypted);
}

/**
 * Secure data wrapper for sensitive information
 */
export class SecureData<T> {
  private encryptedData: string | null = null;
  private salt: string | null = null;
  private iv: string | null = null;
  
  constructor(private sessionKey: string) {}
  
  async set(data: T): Promise<void> {
    const jsonData = JSON.stringify(data);
    const result = await encryptData(jsonData, this.sessionKey);
    this.encryptedData = result.encrypted;
    this.salt = result.salt;
    this.iv = result.iv;
  }
  
  async get(): Promise<T | null> {
    if (!this.encryptedData || !this.salt || !this.iv) {
      return null;
    }
    
    try {
      const decrypted = await decryptData(
        this.encryptedData,
        this.sessionKey,
        this.salt,
        this.iv
      );
      return JSON.parse(decrypted);
    } catch {
      return null;
    }
  }
  
  clear(): void {
    this.encryptedData = null;
    this.salt = null;
    this.iv = null;
  }
  
  hasData(): boolean {
    return this.encryptedData !== null;
  }
}

/**
 * Check if Web Crypto API is available
 */
export function isCryptoAvailable(): boolean {
  return typeof crypto !== 'undefined' && 
         typeof crypto.subtle !== 'undefined';
}

/**
 * Validate encryption capability
 */
export async function validateEncryptionCapability(): Promise<boolean> {
  if (!isCryptoAvailable()) {

    return false;
  }
  
  try {
    // Test encryption/decryption
    const testData = 'test';
    const testPassword = 'password';
    const { encrypted, salt, iv } = await encryptData(testData, testPassword);
    const decrypted = await decryptData(encrypted, testPassword, salt, iv);
    
    return decrypted === testData;
  } catch (error) {
    console.error('Encryption capability test failed:', error);
    return false;
  }
}
