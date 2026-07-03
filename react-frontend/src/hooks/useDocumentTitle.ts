import { useEffect, useRef } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';

/**
 * Hook to manage document title for accessibility
 * Updates the page title and announces changes to screen readers
 */
export const useDocumentTitle = (title: string, announce: boolean = true) => {
  const { t } = useLanguage();
  const prevTitleRef = useRef<string>('');

  useEffect(() => {
    const appName = t('app.name', 'KisanMitra AI');
    const fullTitle = title ? `${title} | ${appName}` : appName;
    
    // Update document title
    document.title = fullTitle;
    
    // Announce page change to screen readers
    if (announce && prevTitleRef.current !== fullTitle) {
      const announcement = document.createElement('div');
      announcement.setAttribute('role', 'status');
      announcement.setAttribute('aria-live', 'polite');
      announcement.setAttribute('aria-atomic', 'true');
      announcement.className = 'sr-only';
      announcement.textContent = t('a11y.pageChanged', `Navigated to ${title}`);
      
      document.body.appendChild(announcement);
      
      // Remove announcement after it's been read
      setTimeout(() => {
        document.body.removeChild(announcement);
      }, 1000);
    }
    
    prevTitleRef.current = fullTitle;
  }, [title, announce, t]);
};

export default useDocumentTitle;
