/**
 * Language-specific font configurations
 * Ensures appropriate fonts are used for each supported language
 */

export interface LanguageFontConfig {
  code: string;
  fontFamily: string;
  fallbackFonts: string[];
  googleFontsUrl?: string;
}

export const languageFonts: Record<string, LanguageFontConfig> = {
  en: {
    code: 'en',
    fontFamily: 'Inter',
    fallbackFonts: ['system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'sans-serif'],
  },
  hi: {
    code: 'hi',
    fontFamily: 'Noto Sans Devanagari',
    fallbackFonts: ['Noto Sans', 'sans-serif'],
    googleFontsUrl: 'https://fonts.googleapis.com/css2?family=Noto+Sans+Devanagari:wght@400;500;600;700&display=swap',
  },
  bn: {
    code: 'bn',
    fontFamily: 'Noto Sans Bengali',
    fallbackFonts: ['Noto Sans', 'sans-serif'],
    googleFontsUrl: 'https://fonts.googleapis.com/css2?family=Noto+Sans+Bengali:wght@400;500;600;700&display=swap',
  },
  te: {
    code: 'te',
    fontFamily: 'Noto Sans Telugu',
    fallbackFonts: ['Noto Sans', 'sans-serif'],
    googleFontsUrl: 'https://fonts.googleapis.com/css2?family=Noto+Sans+Telugu:wght@400;500;600;700&display=swap',
  },
  mr: {
    code: 'mr',
    fontFamily: 'Noto Sans Devanagari',
    fallbackFonts: ['Noto Sans', 'sans-serif'],
    googleFontsUrl: 'https://fonts.googleapis.com/css2?family=Noto+Sans+Devanagari:wght@400;500;600;700&display=swap',
  },
  ta: {
    code: 'ta',
    fontFamily: 'Noto Sans Tamil',
    fallbackFonts: ['Noto Sans', 'sans-serif'],
    googleFontsUrl: 'https://fonts.googleapis.com/css2?family=Noto+Sans+Tamil:wght@400;500;600;700&display=swap',
  },
  gu: {
    code: 'gu',
    fontFamily: 'Noto Sans Gujarati',
    fallbackFonts: ['Noto Sans', 'sans-serif'],
    googleFontsUrl: 'https://fonts.googleapis.com/css2?family=Noto+Sans+Gujarati:wght@400;500;600;700&display=swap',
  },
  kn: {
    code: 'kn',
    fontFamily: 'Noto Sans Kannada',
    fallbackFonts: ['Noto Sans', 'sans-serif'],
    googleFontsUrl: 'https://fonts.googleapis.com/css2?family=Noto+Sans+Kannada:wght@400;500;600;700&display=swap',
  },
};

/**
 * Get the font family string for a given language
 */
export const getFontFamily = (languageCode: string): string => {
  const config = languageFonts[languageCode] || languageFonts.en;
  return `'${config.fontFamily}', ${config.fallbackFonts.join(', ')}`;
};

/**
 * Load Google Fonts for a specific language
 */
export const loadLanguageFont = (languageCode: string): void => {
  const config = languageFonts[languageCode];
  
  if (!config || !config.googleFontsUrl) {
    return;
  }

  // Check if font is already loaded
  const existingLink = document.querySelector(`link[data-language-font="${languageCode}"]`);
  if (existingLink) {
    return;
  }

  // Create and append link element
  const link = document.createElement('link');
  link.rel = 'stylesheet';
  link.href = config.googleFontsUrl;
  link.setAttribute('data-language-font', languageCode);
  document.head.appendChild(link);
};

/**
 * Apply font family to document root
 */
export const applyLanguageFont = (languageCode: string): void => {
  const fontFamily = getFontFamily(languageCode);
  document.documentElement.style.fontFamily = fontFamily;
  
  // Load the font if needed
  loadLanguageFont(languageCode);
};

/**
 * Preload fonts for all supported languages
 * Call this during app initialization for better performance
 */
export const preloadAllLanguageFonts = (): void => {
  Object.keys(languageFonts).forEach(languageCode => {
    loadLanguageFont(languageCode);
  });
};
