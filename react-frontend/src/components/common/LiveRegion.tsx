import React, { useEffect, useRef } from 'react';

interface LiveRegionProps {
  message: string;
  priority?: 'polite' | 'assertive';
  clearOnUnmount?: boolean;
}

/**
 * LiveRegion Component
 * 
 * ARIA live region for announcing dynamic content changes to screen readers.
 * Use 'polite' for non-urgent updates, 'assertive' for important notifications.
 * 
 * WCAG 2.1 Success Criterion 4.1.3 (Status Messages)
 */
export const LiveRegion: React.FC<LiveRegionProps> = ({ 
  message, 
  priority = 'polite',
  clearOnUnmount = true 
}) => {
  const regionRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    return () => {
      if (clearOnUnmount && regionRef.current) {
        regionRef.current.textContent = '';
      }
    };
  }, [clearOnUnmount]);

  return (
    <div
      ref={regionRef}
      role="status"
      aria-live={priority}
      aria-atomic="true"
      className="sr-only"
    >
      {message}
    </div>
  );
};

export default LiveRegion;
