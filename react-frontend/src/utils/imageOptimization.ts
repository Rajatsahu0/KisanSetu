/**
 * Image optimization utilities for performance
 */

export interface ImageOptimizationOptions {
  maxWidth?: number;
  maxHeight?: number;
  quality?: number;
  format?: 'jpeg' | 'png' | 'webp';
}

/**
 * Compress and optimize an image file
 */
export const optimizeImage = async (
  file: File,
  options: ImageOptimizationOptions = {}
): Promise<Blob> => {
  const {
    maxWidth = 1920,
    maxHeight = 1080,
    quality = 0.85,
    format = 'jpeg',
  } = options;

  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    
    reader.onload = (e) => {
      const img = new Image();
      
      img.onload = () => {
        // Calculate new dimensions while maintaining aspect ratio
        let width = img.width;
        let height = img.height;
        
        if (width > maxWidth) {
          height = (height * maxWidth) / width;
          width = maxWidth;
        }
        
        if (height > maxHeight) {
          width = (width * maxHeight) / height;
          height = maxHeight;
        }
        
        // Create canvas and draw resized image
        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        
        const ctx = canvas.getContext('2d');
        if (!ctx) {
          reject(new Error('Failed to get canvas context'));
          return;
        }
        
        // Use better image smoothing
        ctx.imageSmoothingEnabled = true;
        ctx.imageSmoothingQuality = 'high';
        
        ctx.drawImage(img, 0, 0, width, height);
        
        // Convert to blob
        canvas.toBlob(
          (blob) => {
            if (blob) {
              resolve(blob);
            } else {
              reject(new Error('Failed to create blob'));
            }
          },
          `image/${format}`,
          quality
        );
      };
      
      img.onerror = () => reject(new Error('Failed to load image'));
      img.src = e.target?.result as string;
    };
    
    reader.onerror = () => reject(new Error('Failed to read file'));
    reader.readAsDataURL(file);
  });
};

/**
 * Generate a thumbnail from an image file
 */
export const generateThumbnail = async (
  file: File,
  size: number = 200
): Promise<Blob> => {
  return optimizeImage(file, {
    maxWidth: size,
    maxHeight: size,
    quality: 0.7,
    format: 'jpeg',
  });
};

/**
 * Check if image needs optimization
 */
export const needsOptimization = (file: File, maxSize: number = 1024 * 1024): boolean => {
  return file.size > maxSize;
};

/**
 * Get optimized image dimensions
 */
export const getOptimizedDimensions = (
  width: number,
  height: number,
  maxWidth: number = 1920,
  maxHeight: number = 1080
): { width: number; height: number } => {
  let newWidth = width;
  let newHeight = height;
  
  if (newWidth > maxWidth) {
    newHeight = (newHeight * maxWidth) / newWidth;
    newWidth = maxWidth;
  }
  
  if (newHeight > maxHeight) {
    newWidth = (newWidth * maxHeight) / newHeight;
    newHeight = maxHeight;
  }
  
  return { width: Math.round(newWidth), height: Math.round(newHeight) };
};

/**
 * Create a lazy loading image observer
 */
export const createImageObserver = (
  callback: (entry: IntersectionObserverEntry) => void,
  options: IntersectionObserverInit = {}
): IntersectionObserver => {
  const defaultOptions: IntersectionObserverInit = {
    root: null,
    rootMargin: '50px',
    threshold: 0.01,
    ...options,
  };
  
  return new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        callback(entry);
      }
    });
  }, defaultOptions);
};

/**
 * Progressive image loading component helper
 */
export const loadProgressiveImage = async (
  lowQualitySrc: string,
  highQualitySrc: string,
  onLowQualityLoad?: () => void,
  onHighQualityLoad?: () => void
): Promise<void> => {
  // Load low quality first
  const lowQualityImg = new Image();
  lowQualityImg.src = lowQualitySrc;
  
  await new Promise<void>((resolve) => {
    lowQualityImg.onload = () => {
      onLowQualityLoad?.();
      resolve();
    };
  });
  
  // Then load high quality
  const highQualityImg = new Image();
  highQualityImg.src = highQualitySrc;
  
  await new Promise<void>((resolve) => {
    highQualityImg.onload = () => {
      onHighQualityLoad?.();
      resolve();
    };
  });
};

/**
 * Convert image to WebP format if supported
 */
export const convertToWebP = async (file: File, quality: number = 0.85): Promise<Blob | null> => {
  // Check if WebP is supported
  const canvas = document.createElement('canvas');
  const isWebPSupported = canvas.toDataURL('image/webp').indexOf('data:image/webp') === 0;
  
  if (!isWebPSupported) {
    return null;
  }
  
  return optimizeImage(file, {
    quality,
    format: 'webp',
  });
};
