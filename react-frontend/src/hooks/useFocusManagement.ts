import { useEffect, useRef, RefObject } from 'react';

/**
 * Hook to manage focus on component mount
 */
export const useFocusOnMount = (shouldFocus: boolean = true) => {
  const ref = useRef<HTMLElement>(null);

  useEffect(() => {
    if (shouldFocus && ref.current) {
      // Small delay to ensure element is rendered
      setTimeout(() => {
        ref.current?.focus();
      }, 100);
    }
  }, [shouldFocus]);

  return ref;
};

/**
 * Hook to trap focus within a container (for modals, dialogs)
 */
export const useFocusTrap = (
  isActive: boolean,
  containerRef: RefObject<HTMLElement>
) => {
  useEffect(() => {
    if (!isActive || !containerRef.current) return;

    const container = containerRef.current;
    const focusableSelectors = [
      'a[href]',
      'button:not([disabled])',
      'textarea:not([disabled])',
      'input:not([disabled])',
      'select:not([disabled])',
      '[tabindex]:not([tabindex="-1"])'
    ].join(', ');

    const getFocusableElements = (): HTMLElement[] => {
      return Array.from(
        container.querySelectorAll(focusableSelectors)
      ) as HTMLElement[];
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Tab') return;

      const focusableElements = getFocusableElements();
      if (focusableElements.length === 0) return;

      const firstElement = focusableElements[0];
      const lastElement = focusableElements[focusableElements.length - 1];

      if (event.shiftKey && document.activeElement === firstElement) {
        event.preventDefault();
        lastElement.focus();
      } else if (!event.shiftKey && document.activeElement === lastElement) {
        event.preventDefault();
        firstElement.focus();
      }
    };

    // Store the previously focused element
    const previouslyFocusedElement = document.activeElement as HTMLElement;

    // Focus the first focusable element
    const focusableElements = getFocusableElements();
    if (focusableElements.length > 0) {
      focusableElements[0].focus();
    }

    // Add event listener
    container.addEventListener('keydown', handleKeyDown);

    // Cleanup
    return () => {
      container.removeEventListener('keydown', handleKeyDown);
      // Restore focus to previously focused element
      if (previouslyFocusedElement) {
        previouslyFocusedElement.focus();
      }
    };
  }, [isActive, containerRef]);
};

/**
 * Hook to restore focus to a trigger element
 */
export const useFocusReturn = (
  isOpen: boolean,
  triggerRef: RefObject<HTMLElement>
) => {
  const previousFocusRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    if (isOpen) {
      // Store the currently focused element
      previousFocusRef.current = document.activeElement as HTMLElement;
    } else if (previousFocusRef.current) {
      // Restore focus when closed
      previousFocusRef.current.focus();
      previousFocusRef.current = null;
    }
  }, [isOpen]);
};

/**
 * Hook to manage focus for roving tabindex pattern
 * Useful for keyboard navigation in lists, menus, toolbars
 */
export const useRovingTabIndex = (
  items: RefObject<HTMLElement>[],
  activeIndex: number,
  setActiveIndex: (index: number) => void
) => {
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      let newIndex = activeIndex;

      switch (event.key) {
        case 'ArrowDown':
        case 'ArrowRight':
          event.preventDefault();
          newIndex = (activeIndex + 1) % items.length;
          break;
        case 'ArrowUp':
        case 'ArrowLeft':
          event.preventDefault();
          newIndex = activeIndex === 0 ? items.length - 1 : activeIndex - 1;
          break;
        case 'Home':
          event.preventDefault();
          newIndex = 0;
          break;
        case 'End':
          event.preventDefault();
          newIndex = items.length - 1;
          break;
        default:
          return;
      }

      setActiveIndex(newIndex);
      items[newIndex]?.current?.focus();
    };

    // Add event listener to all items
    items.forEach(itemRef => {
      itemRef.current?.addEventListener('keydown', handleKeyDown);
    });

    // Cleanup
    return () => {
      items.forEach(itemRef => {
        itemRef.current?.removeEventListener('keydown', handleKeyDown);
      });
    };
  }, [items, activeIndex, setActiveIndex]);

  // Update tabindex for all items
  useEffect(() => {
    items.forEach((itemRef, index) => {
      if (itemRef.current) {
        itemRef.current.setAttribute(
          'tabindex',
          index === activeIndex ? '0' : '-1'
        );
      }
    });
  }, [items, activeIndex]);
};

export default {
  useFocusOnMount,
  useFocusTrap,
  useFocusReturn,
  useRovingTabIndex
};
