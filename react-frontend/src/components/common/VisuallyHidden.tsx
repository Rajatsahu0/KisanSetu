import React from 'react';

interface VisuallyHiddenProps {
  children: React.ReactNode;
  as?: keyof JSX.IntrinsicElements;
}

/**
 * VisuallyHidden Component
 * 
 * Renders content that is visually hidden but accessible to screen readers.
 * Uses the sr-only utility class from Tailwind CSS.
 * 
 * WCAG 2.1 Success Criterion 1.3.1 (Info and Relationships)
 */
export const VisuallyHidden: React.FC<VisuallyHiddenProps> = ({ 
  children, 
  as: Component = 'span' 
}) => {
  return (
    <Component className="sr-only">
      {children}
    </Component>
  );
};

export default VisuallyHidden;
