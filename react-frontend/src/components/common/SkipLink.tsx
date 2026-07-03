import React from 'react';

interface SkipLinkProps {
  targetId: string;
  children: React.ReactNode;
}

/**
 * SkipLink Component
 * 
 * Provides a keyboard-accessible skip link for users to bypass navigation
 * and jump directly to main content. Visible only when focused.
 * 
 * WCAG 2.1 Success Criterion 2.4.1 (Bypass Blocks)
 */
export const SkipLink: React.FC<SkipLinkProps> = ({ targetId, children }) => {
  const handleClick = (event: React.MouseEvent<HTMLAnchorElement>) => {
    event.preventDefault();
    const target = document.getElementById(targetId);
    
    if (target) {
      // Set tabindex to make it focusable
      target.setAttribute('tabindex', '-1');
      target.focus();
      target.scrollIntoView({ behavior: 'smooth', block: 'start' });
      
      // Remove tabindex after focus to restore natural tab order
      target.addEventListener('blur', () => {
        target.removeAttribute('tabindex');
      }, { once: true });
    }
  };

  return (
    <a
      href={`#${targetId}`}
      onClick={handleClick}
      className="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 focus:z-50 focus:px-4 focus:py-2 focus:bg-primary-600 focus:text-white focus:rounded-md focus:shadow-lg focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2"
    >
      {children}
    </a>
  );
};

export default SkipLink;
