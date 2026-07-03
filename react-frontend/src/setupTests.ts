import '@testing-library/jest-dom';
import { vi } from 'vitest';

// Mock PWA virtual module
vi.mock('virtual:pwa-register', () => ({
  registerSW: () => ({
    updateServiceWorker: () => Promise.resolve(),
  }),
}));

