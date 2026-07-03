/**
 * Content Security Policy configuration for the application
 */

import { generateCSPHeader } from '../utils/security';

/**
 * Apply CSP meta tag to document
 * This is a fallback for when server headers are not available
 */
export function applyCSPMetaTag(): void {
  const cspContent = generateCSPHeader();
  
  // Check if CSP meta tag already exists
  let metaTag = document.querySelector('meta[http-equiv="Content-Security-Policy"]');
  
  if (!metaTag) {
    metaTag = document.createElement('meta');
    metaTag.setAttribute('http-equiv', 'Content-Security-Policy');
    document.head.appendChild(metaTag);
  }
  
  metaTag.setAttribute('content', cspContent);
}

/**
 * Initialize security headers and policies
 */
export function initializeSecurity(): void {
  // Apply CSP
  applyCSPMetaTag();
  
  // Disable right-click in production (optional)
  if (import.meta.env.PROD) {
    document.addEventListener('contextmenu', (e) => {
      // Allow right-click on input fields for usability
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) {
        return;
      }
      // Uncomment to disable right-click
      // e.preventDefault();
    });
  }
  
  // Prevent iframe embedding
  if (window.self !== window.top) {
    // Page is in an iframe, redirect to break out
    window.top!.location.href = window.self.location.href;
  }
  
  // Log security initialization

}

/**
 * Validate that the application is running in a secure context
 */
export function validateSecureContext(): boolean {
  // Check if running over HTTPS (or localhost)
  const isSecure = window.isSecureContext;
  
  if (!isSecure && import.meta.env.PROD) {
    console.error('Application must be served over HTTPS in production');
    return false;
  }
  
  return true;
}

/**
 * Security configuration for different environments
 */
export const securityConfig = {
  development: {
    enforceHTTPS: false,
    enableCSP: true,
    strictMode: false,
  },
  production: {
    enforceHTTPS: true,
    enableCSP: true,
    strictMode: true,
  },
};

/**
 * Get current security configuration
 */
export function getSecurityConfig() {
  return import.meta.env.PROD 
    ? securityConfig.production 
    : securityConfig.development;
}
