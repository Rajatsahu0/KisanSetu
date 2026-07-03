/**
 * Performance monitoring utilities for tracking web vitals and metrics
 */

export interface PerformanceMetrics {
  fcp?: number; // First Contentful Paint
  lcp?: number; // Largest Contentful Paint
  fid?: number; // First Input Delay
  cls?: number; // Cumulative Layout Shift
  ttfb?: number; // Time to First Byte
  tti?: number; // Time to Interactive
}

export interface PerformanceThresholds {
  fcp: { good: number; needsImprovement: number };
  lcp: { good: number; needsImprovement: number };
  fid: { good: number; needsImprovement: number };
  cls: { good: number; needsImprovement: number };
  ttfb: { good: number; needsImprovement: number };
}

// Web Vitals thresholds (in milliseconds, except CLS)
export const PERFORMANCE_THRESHOLDS: PerformanceThresholds = {
  fcp: { good: 1800, needsImprovement: 3000 },
  lcp: { good: 2500, needsImprovement: 4000 },
  fid: { good: 100, needsImprovement: 300 },
  cls: { good: 0.1, needsImprovement: 0.25 },
  ttfb: { good: 800, needsImprovement: 1800 },
};

/**
 * Get performance rating based on thresholds
 */
export const getPerformanceRating = (
  metric: keyof PerformanceThresholds,
  value: number
): 'good' | 'needs-improvement' | 'poor' => {
  const threshold = PERFORMANCE_THRESHOLDS[metric];
  
  if (value <= threshold.good) {
    return 'good';
  } else if (value <= threshold.needsImprovement) {
    return 'needs-improvement';
  } else {
    return 'poor';
  }
};

/**
 * Measure First Contentful Paint (FCP)
 */
export const measureFCP = (): Promise<number | null> => {
  return new Promise((resolve) => {
    if (!('PerformanceObserver' in window)) {
      resolve(null);
      return;
    }

    try {
      const observer = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        const fcpEntry = entries.find((entry) => entry.name === 'first-contentful-paint');
        
        if (fcpEntry) {
          resolve(fcpEntry.startTime);
          observer.disconnect();
        }
      });

      observer.observe({ entryTypes: ['paint'] });
    } catch (error) {
      console.error('Error measuring FCP:', error);
      resolve(null);
    }
  });
};

/**
 * Measure Largest Contentful Paint (LCP)
 */
export const measureLCP = (): Promise<number | null> => {
  return new Promise((resolve) => {
    if (!('PerformanceObserver' in window)) {
      resolve(null);
      return;
    }

    try {
      let lcpValue = 0;
      const observer = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        const lastEntry = entries[entries.length - 1] as PerformanceEntry & { renderTime?: number; loadTime?: number };
        lcpValue = lastEntry.renderTime || lastEntry.loadTime || lastEntry.startTime;
      });

      observer.observe({ entryTypes: ['largest-contentful-paint'] });

      // Resolve after page load
      window.addEventListener('load', () => {
        setTimeout(() => {
          resolve(lcpValue);
          observer.disconnect();
        }, 0);
      });
    } catch (error) {
      console.error('Error measuring LCP:', error);
      resolve(null);
    }
  });
};

/**
 * Measure First Input Delay (FID)
 */
export const measureFID = (): Promise<number | null> => {
  return new Promise((resolve) => {
    if (!('PerformanceObserver' in window)) {
      resolve(null);
      return;
    }

    try {
      const observer = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        const fidEntry = entries[0] as PerformanceEntry & { processingStart?: number };
        
        if (fidEntry) {
          const fid = fidEntry.processingStart ? fidEntry.processingStart - fidEntry.startTime : 0;
          resolve(fid);
          observer.disconnect();
        }
      });

      observer.observe({ entryTypes: ['first-input'] });
    } catch (error) {
      console.error('Error measuring FID:', error);
      resolve(null);
    }
  });
};

/**
 * Measure Cumulative Layout Shift (CLS)
 */
export const measureCLS = (): Promise<number | null> => {
  return new Promise((resolve) => {
    if (!('PerformanceObserver' in window)) {
      resolve(null);
      return;
    }

    try {
      let clsValue = 0;
      const observer = new PerformanceObserver((list) => {
        const entries = list.getEntries();
        entries.forEach((entry: any) => {
          if (!entry.hadRecentInput) {
            clsValue += entry.value;
          }
        });
      });

      observer.observe({ entryTypes: ['layout-shift'] });

      // Resolve after page load
      window.addEventListener('load', () => {
        setTimeout(() => {
          resolve(clsValue);
          observer.disconnect();
        }, 0);
      });
    } catch (error) {
      console.error('Error measuring CLS:', error);
      resolve(null);
    }
  });
};

/**
 * Measure Time to First Byte (TTFB)
 */
export const measureTTFB = (): number | null => {
  if (!('performance' in window) || !performance.timing) {
    return null;
  }

  const { responseStart, requestStart } = performance.timing;
  return responseStart - requestStart;
};

/**
 * Get all performance metrics
 */
export const getAllMetrics = async (): Promise<PerformanceMetrics> => {
  const [fcp, lcp, fid, cls] = await Promise.all([
    measureFCP(),
    measureLCP(),
    measureFID(),
    measureCLS(),
  ]);

  const ttfb = measureTTFB();

  return {
    fcp: fcp ?? undefined,
    lcp: lcp ?? undefined,
    fid: fid ?? undefined,
    cls: cls ?? undefined,
    ttfb: ttfb ?? undefined,
  };
};

/**
 * Log performance metrics to console (development only)
 */
export const logPerformanceMetrics = async (): Promise<void> => {
  if (import.meta.env.PROD) return;

  const metrics = await getAllMetrics();

  if (metrics.fcp) {
    const rating = getPerformanceRating('fcp', metrics.fcp);
    console.log(`FCP: ${metrics.fcp}ms (${rating})`);
  }

  if (metrics.lcp) {
    const rating = getPerformanceRating('lcp', metrics.lcp);
    console.log(`LCP: ${metrics.lcp}ms (${rating})`);
  }

  if (metrics.fid) {
    const rating = getPerformanceRating('fid', metrics.fid);
    console.log(`FID: ${metrics.fid}ms (${rating})`);
  }

  if (metrics.cls) {
    const rating = getPerformanceRating('cls', metrics.cls);
    console.log(`CLS: ${metrics.cls} (${rating})`);
  }

  if (metrics.ttfb) {
    const rating = getPerformanceRating('ttfb', metrics.ttfb);
    console.log(`TTFB: ${metrics.ttfb}ms (${rating})`);
  }
};

/**
 * Send performance metrics to analytics (placeholder)
 */
export const sendPerformanceMetrics = async (
  metrics: PerformanceMetrics
): Promise<void> => {
  // In production, send to analytics service
  if (import.meta.env.PROD) {
    // TODO: Implement analytics integration

  }
};

/**
 * Monitor long tasks (tasks taking > 50ms)
 */
export const monitorLongTasks = (callback: (duration: number) => void): void => {
  if (!('PerformanceObserver' in window)) return;

  try {
    const observer = new PerformanceObserver((list) => {
      const entries = list.getEntries();
      entries.forEach((entry) => {
        if (entry.duration > 50) {
          callback(entry.duration);
        }
      });
    });

    observer.observe({ entryTypes: ['longtask'] });
  } catch (error) {
    console.error('Error monitoring long tasks:', error);
  }
};

/**
 * Get bundle size information
 */
export const getBundleSize = (): { total: number; resources: Array<{ name: string; size: number }> } => {
  if (!('performance' in window)) {
    return { total: 0, resources: [] };
  }

  const resources = performance.getEntriesByType('resource') as PerformanceResourceTiming[];
  const jsResources = resources.filter((r) => r.name.endsWith('.js'));
  
  const total = jsResources.reduce((sum, r) => sum + (r.transferSize || 0), 0);
  const resourceList = jsResources.map((r) => ({
    name: r.name.split('/').pop() || r.name,
    size: r.transferSize || 0,
  }));

  return { total, resources: resourceList };
};

/**
 * Initialize performance monitoring
 */
export const initPerformanceMonitoring = (): void => {
  // Log metrics after page load
  window.addEventListener('load', () => {
    setTimeout(() => {
      logPerformanceMetrics();
    }, 0);
  });

  // Monitor long tasks
  monitorLongTasks((duration) => {
    if (import.meta.env.DEV) {
      console.log(`Long task detected: ${duration}ms`);
    }
  });
};
