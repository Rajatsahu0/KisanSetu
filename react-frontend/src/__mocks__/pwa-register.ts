// Mock for virtual:pwa-register module used in tests
export function registerSW() {
  return {
    updateServiceWorker: () => Promise.resolve(),
  };
}
