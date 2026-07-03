/**
 * Network speed detection and adaptive loading utilities
 */

export type NetworkSpeed = 'slow-2g' | '2g' | '3g' | '4g' | 'unknown';
export type ConnectionType = 'cellular' | 'wifi' | 'ethernet' | 'unknown';

interface NetworkInformation extends EventTarget {
  effectiveType?: NetworkSpeed;
  downlink?: number;
  rtt?: number;
  saveData?: boolean;
  type?: string;
  addEventListener(type: 'change', listener: () => void): void;
  removeEventListener(type: 'change', listener: () => void): void;
}

interface NavigatorWithConnection extends Navigator {
  connection?: NetworkInformation;
  mozConnection?: NetworkInformation;
  webkitConnection?: NetworkInformation;
}

/**
 * Get network information from browser API
 */
const getNetworkInfo = (): NetworkInformation | null => {
  const nav = navigator as NavigatorWithConnection;
  return nav.connection || nav.mozConnection || nav.webkitConnection || null;
};

/**
 * Get current network speed
 */
export const getNetworkSpeed = (): NetworkSpeed => {
  const connection = getNetworkInfo();
  return connection?.effectiveType || 'unknown';
};

/**
 * Check if connection is slow (2G or slow-2G)
 */
export const isSlowConnection = (): boolean => {
  const speed = getNetworkSpeed();
  return speed === 'slow-2g' || speed === '2g';
};

/**
 * Check if connection is fast (4G)
 */
export const isFastConnection = (): boolean => {
  const speed = getNetworkSpeed();
  return speed === '4g';
};

/**
 * Check if user has enabled data saver mode
 */
export const isDataSaverEnabled = (): boolean => {
  const connection = getNetworkInfo();
  return connection?.saveData || false;
};

/**
 * Get connection type (cellular, wifi, etc.)
 */
export const getConnectionType = (): ConnectionType => {
  const connection = getNetworkInfo();
  const type = connection?.type;
  
  if (!type) return 'unknown';
  
  if (type.includes('cellular')) return 'cellular';
  if (type.includes('wifi')) return 'wifi';
  if (type.includes('ethernet')) return 'ethernet';
  
  return 'unknown';
};

/**
 * Get download speed in Mbps
 */
export const getDownloadSpeed = (): number | null => {
  const connection = getNetworkInfo();
  return connection?.downlink || null;
};

/**
 * Get round-trip time (latency) in ms
 */
export const getRTT = (): number | null => {
  const connection = getNetworkInfo();
  return connection?.rtt || null;
};

/**
 * Listen for network speed changes
 */
export const onNetworkSpeedChange = (callback: (speed: NetworkSpeed) => void): (() => void) => {
  const connection = getNetworkInfo();
  
  if (!connection) {
    return () => {}; // No-op cleanup
  }
  
  const handler = () => {
    callback(getNetworkSpeed());
  };
  
  connection.addEventListener('change', handler);
  
  return () => {
    connection.removeEventListener('change', handler);
  };
};

/**
 * Get recommended image quality based on network speed
 */
export const getRecommendedImageQuality = (): number => {
  const speed = getNetworkSpeed();
  const dataSaver = isDataSaverEnabled();
  
  if (dataSaver) return 0.5;
  
  switch (speed) {
    case 'slow-2g':
    case '2g':
      return 0.5;
    case '3g':
      return 0.7;
    case '4g':
      return 0.85;
    default:
      return 0.85;
  }
};

/**
 * Get recommended max image dimensions based on network speed
 */
export const getRecommendedImageDimensions = (): { maxWidth: number; maxHeight: number } => {
  const speed = getNetworkSpeed();
  const dataSaver = isDataSaverEnabled();
  
  if (dataSaver) {
    return { maxWidth: 800, maxHeight: 600 };
  }
  
  switch (speed) {
    case 'slow-2g':
    case '2g':
      return { maxWidth: 800, maxHeight: 600 };
    case '3g':
      return { maxWidth: 1280, maxHeight: 720 };
    case '4g':
      return { maxWidth: 1920, maxHeight: 1080 };
    default:
      return { maxWidth: 1920, maxHeight: 1080 };
  }
};

/**
 * Check if should preload resources based on network conditions
 */
export const shouldPreloadResources = (): boolean => {
  const speed = getNetworkSpeed();
  const dataSaver = isDataSaverEnabled();
  
  if (dataSaver) return false;
  
  return speed === '4g' || speed === 'unknown';
};

/**
 * Check if should lazy load images based on network conditions
 */
export const shouldLazyLoadImages = (): boolean => {
  const speed = getNetworkSpeed();
  const dataSaver = isDataSaverEnabled();
  
  if (dataSaver) return true;
  
  return speed === 'slow-2g' || speed === '2g' || speed === '3g';
};

/**
 * Get adaptive loading configuration
 */
export interface AdaptiveLoadingConfig {
  imageQuality: number;
  maxImageWidth: number;
  maxImageHeight: number;
  shouldPreload: boolean;
  shouldLazyLoad: boolean;
  enableAnimations: boolean;
  enableVideoAutoplay: boolean;
}

export const getAdaptiveLoadingConfig = (): AdaptiveLoadingConfig => {
  const speed = getNetworkSpeed();
  const dataSaver = isDataSaverEnabled();
  const dimensions = getRecommendedImageDimensions();
  
  return {
    imageQuality: getRecommendedImageQuality(),
    maxImageWidth: dimensions.maxWidth,
    maxImageHeight: dimensions.maxHeight,
    shouldPreload: shouldPreloadResources(),
    shouldLazyLoad: shouldLazyLoadImages(),
    enableAnimations: !dataSaver && speed !== 'slow-2g' && speed !== '2g',
    enableVideoAutoplay: !dataSaver && (speed === '4g' || speed === 'unknown'),
  };
};

/**
 * Format network speed for display
 */
export const formatNetworkSpeed = (speed: NetworkSpeed): string => {
  const speedMap: Record<NetworkSpeed, string> = {
    'slow-2g': 'Slow 2G',
    '2g': '2G',
    '3g': '3G',
    '4g': '4G',
    'unknown': 'Unknown',
  };
  
  return speedMap[speed];
};

/**
 * Get network quality indicator
 */
export const getNetworkQuality = (): 'poor' | 'moderate' | 'good' | 'excellent' => {
  const speed = getNetworkSpeed();
  
  switch (speed) {
    case 'slow-2g':
      return 'poor';
    case '2g':
      return 'moderate';
    case '3g':
      return 'good';
    case '4g':
      return 'excellent';
    default:
      return 'good';
  }
};
