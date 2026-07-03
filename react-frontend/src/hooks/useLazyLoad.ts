import { useEffect, useRef, useState } from 'react';

interface UseLazyLoadOptions {
  threshold?: number;
  rootMargin?: string;
  triggerOnce?: boolean;
}

/**
 * Hook for lazy loading elements when they enter the viewport
 */
export const useLazyLoad = <T extends HTMLElement>(
  options: UseLazyLoadOptions = {}
) => {
  const { threshold = 0.01, rootMargin = '50px', triggerOnce = true } = options;
  const [isInView, setIsInView] = useState(false);
  const elementRef = useRef<T>(null);

  useEffect(() => {
    const element = elementRef.current;
    if (!element) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setIsInView(true);
            if (triggerOnce) {
              observer.disconnect();
            }
          } else if (!triggerOnce) {
            setIsInView(false);
          }
        });
      },
      {
        threshold,
        rootMargin,
      }
    );

    observer.observe(element);

    return () => {
      observer.disconnect();
    };
  }, [threshold, rootMargin, triggerOnce]);

  return { elementRef, isInView };
};

/**
 * Hook for preloading images
 */
export const useImagePreload = (src: string) => {
  const [isLoaded, setIsLoaded] = useState(false);
  const [hasError, setHasError] = useState(false);

  useEffect(() => {
    const img = new Image();
    
    img.onload = () => {
      setIsLoaded(true);
    };
    
    img.onerror = () => {
      setHasError(true);
    };
    
    img.src = src;

    return () => {
      img.onload = null;
      img.onerror = null;
    };
  }, [src]);

  return { isLoaded, hasError };
};

/**
 * Hook for progressive image loading
 */
export const useProgressiveImage = (lowQualitySrc: string, highQualitySrc: string) => {
  const [currentSrc, setCurrentSrc] = useState(lowQualitySrc);
  const [isHighQualityLoaded, setIsHighQualityLoaded] = useState(false);

  useEffect(() => {
    // Load low quality first
    const lowQualityImg = new Image();
    lowQualityImg.src = lowQualitySrc;
    
    lowQualityImg.onload = () => {
      setCurrentSrc(lowQualitySrc);
      
      // Then load high quality
      const highQualityImg = new Image();
      highQualityImg.src = highQualitySrc;
      
      highQualityImg.onload = () => {
        setCurrentSrc(highQualitySrc);
        setIsHighQualityLoaded(true);
      };
    };
  }, [lowQualitySrc, highQualitySrc]);

  return { src: currentSrc, isHighQualityLoaded };
};
