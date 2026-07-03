/**
 * Bug Condition Exploration Test for Language Selector Position Fix
 * 
 * **Validates: Requirements 1.1, 1.2, 1.3**
 * 
 * This test verifies the BUG CONDITION - that the language selector is NOT rendered
 * anywhere in the application UI, making language switching inaccessible to users.
 * 
 * IMPORTANT: This test is EXPECTED TO FAIL on unfixed code (confirming the bug exists).
 * After the fix is implemented, this test should PASS.
 */

import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { Provider } from 'react-redux';
import { QueryClientProvider } from '@tanstack/react-query';
import { store } from '@/store';
import { queryClient } from '@/lib/queryClient';
import { LanguageProvider } from '@/contexts/LanguageContext';
import App from '@/App';

/**
 * Helper function to render the App with all required providers
 */
const renderApp = () => {
  return render(
    <Provider store={store}>
      <QueryClientProvider client={queryClient}>
        <LanguageProvider>
          <BrowserRouter>
            <App />
          </BrowserRouter>
        </LanguageProvider>
      </QueryClientProvider>
    </Provider>
  );
};

describe('Bug Condition Exploration: Language Selector Not Rendered', () => {
  /**
   * Property 1: Fault Condition - Language Selector Not Rendered
   * 
   * This test verifies that the language toggle button EXISTS in the DOM
   * when navigating to any page. On UNFIXED code, this test will FAIL
   * because the language selector is not rendered anywhere.
   * 
   * After the fix, this test should PASS, confirming the language selector
   * is now visible and accessible.
   */
  it('should render a language toggle button accessible to users', () => {
    renderApp();

    // Look for language selector by role and accessible name
    // The language selector should be a button with an accessible label
    const languageButton = screen.queryByRole('button', { 
      name: /language/i 
    });

    // EXPECTED TO FAIL on unfixed code: languageButton will be null
    // EXPECTED TO PASS after fix: languageButton will exist
    expect(languageButton).toBeInTheDocument();
  });

  /**
   * Property 1 (Alternative): Verify language selector icon is visible
   * 
   * Tests that a language/globe icon is present in the UI, which should
   * be part of the floating language toggle button.
   */
  it('should display a language selector icon in the UI', () => {
    renderApp();

    // Look for any element with language-related ARIA labels or test IDs
    // The LanguageSelector component uses LanguageIcon from heroicons
    const languageElements = screen.queryAllByRole('button').filter(button => {
      const ariaLabel = button.getAttribute('aria-label');
      return ariaLabel && /language|भाषा/i.test(ariaLabel);
    });

    // EXPECTED TO FAIL on unfixed code: no language buttons found
    // EXPECTED TO PASS after fix: at least one language button exists
    expect(languageElements.length).toBeGreaterThan(0);
  });

  /**
   * Property 1 (Comprehensive): Verify users can access language switching functionality
   * 
   * Tests that the language switching UI is accessible and functional.
   * This is the core requirement - users must be able to switch languages.
   */
  it('should provide accessible language switching functionality in the UI', () => {
    renderApp();

    // The LanguageSelector component should render a Listbox button
    // Look for any button that could trigger language selection
    const allButtons = screen.queryAllByRole('button');
    
    // Check if any button has language-related content or attributes
    const hasLanguageButton = allButtons.some(button => {
      const text = button.textContent || '';
      const ariaLabel = button.getAttribute('aria-label') || '';
      const className = button.className || '';
      
      // Check for language indicators: text, aria-label, or specific classes
      return (
        /language|भाषा|english|hindi|हिन्दी/i.test(text) ||
        /language|भाषा/i.test(ariaLabel) ||
        className.includes('language')
      );
    });

    // EXPECTED TO FAIL on unfixed code: no language button found
    // EXPECTED TO PASS after fix: language button exists
    expect(hasLanguageButton).toBe(true);
  });

  /**
   * Property 1 (DOM Query): Direct verification that LanguageSelector is not in DOM
   * 
   * This test directly checks that the LanguageSelector component or its
   * characteristic elements are not present in the rendered DOM tree.
   */
  it('should have language selector component rendered in the DOM', () => {
    const { container } = renderApp();

    // The LanguageSelector uses Headless UI Listbox component
    // Look for characteristic elements: buttons with language content
    const languageRelatedElements = container.querySelectorAll(
      'button[aria-label*="language" i], button[aria-label*="भाषा" i], [class*="language"]'
    );

    // Also check for the presence of language names in buttons
    const buttonsWithLanguageText = Array.from(container.querySelectorAll('button')).filter(
      button => /english|hindi|हिन्दी/i.test(button.textContent || '')
    );

    const totalLanguageElements = languageRelatedElements.length + buttonsWithLanguageText.length;

    // EXPECTED TO FAIL on unfixed code: totalLanguageElements will be 0
    // EXPECTED TO PASS after fix: at least one language element exists
    expect(totalLanguageElements).toBeGreaterThan(0);
  });
});
