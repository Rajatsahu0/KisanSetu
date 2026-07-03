import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.tsx'
import './index.css'
import './i18n/config'
import { initPerformanceMonitoring } from './utils/performanceMonitoring'
import { initializeSecurity, validateSecureContext } from './config/csp'
import { initializeDataRetention } from './utils/dataRetention'

// Validate secure context (only warn in development, don't block)
if (!validateSecureContext()) {
  // Running in insecure context - some features may be limited
}

// Initialize security features (wrapped in try-catch to prevent blocking)
try {
  initializeSecurity();
  initializeDataRetention();
} catch (error) {
  // Security initialization failed - continue anyway
}

// Initialize performance monitoring in development
if (import.meta.env.DEV) {
  initPerformanceMonitoring();
}

// Recovery: if SW serves stale assets causing chunk load errors, unregister and reload
window.addEventListener('error', (event) => {
  const msg = event.message || '';
  if (msg.includes('Failed to fetch dynamically imported module') ||
      msg.includes('ChunkLoadError') ||
      msg.includes('Loading chunk') ||
      msg.includes('Importing a module script failed')) {
    // Stale SW cache — unregister and hard reload
    if ('serviceWorker' in navigator) {
      navigator.serviceWorker.getRegistrations().then(registrations => {
        registrations.forEach(r => r.unregister());
      });
    }
    // Clear caches
    if ('caches' in window) {
      caches.keys().then(names => names.forEach(name => caches.delete(name)));
    }
    // Reload after cleanup
    setTimeout(() => window.location.reload(), 100);
  }
});

const root = document.getElementById('root');
if (root) {
  ReactDOM.createRoot(root).render(
    <React.StrictMode>
      <App />
    </React.StrictMode>,
  );
} else {
  document.body.innerHTML = '<div style="padding:2rem;text-align:center"><h2>Loading failed. Please refresh.</h2></div>';
}