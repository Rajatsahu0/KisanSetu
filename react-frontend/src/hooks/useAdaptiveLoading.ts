import { useState, useEffect } from 'react';
import {
  getAdaptiveLoadingConfig,
  getNetworkSpeed,
  onNetworkSpeedChange,
  type AdaptiveLoadingConfig,
  type NetworkSpeed,
} from '@/utils/networkSpeed';

/**
 * Hook for adaptive loading based on network conditions
 */
export const useAdaptiveLoading = () => {
  const [config, setConfig] = useState<AdaptiveLoadingConfig>(getAdaptiveLoadingConfig());
  const [networkSpeed, setNetworkSpeed] = useState<NetworkSpeed>(getNetworkSpeed());

  useEffect(() => {
    // Update config when network speed changes
    const cleanup = onNetworkSpeedChange((speed) => {
      setNetworkSpeed(speed);
      setConfig(getAdaptiveLoadingConfig());
    });

    return cleanup;
  }, []);

  return {
    config,
    networkSpeed,
    isSlowConnection: networkSpeed === 'slow-2g' || networkSpeed === '2g',
    isFastConnection: networkSpeed === '4g',
  };
};

/**
 * Hook for adaptive image loading
 */
export const useAdaptiveImage = (src: string) => {
  const { config } = useAdaptiveLoading();
  const [optimizedSrc, setOptimizedSrc] = useState(src);

  useEffect(() => {
    // In a real implementation, this would request different image sizes
    // based on network conditions from a CDN or image service
    // For now, we just return the original src
    setOptimizedSrc(src);
  }, [src, config]);

  return {
    src: optimizedSrc,
    quality: config.imageQuality,
    shouldLazyLoad: config.shouldLazyLoad,
  };
};
