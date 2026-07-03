import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

// Import translation files
import enTranslations from './locales/en/translation.json';
import hiTranslations from './locales/hi/translation.json';
import bnTranslations from './locales/bn/translation.json';
import teTranslations from './locales/te/translation.json';
import mrTranslations from './locales/mr/translation.json';
import taTranslations from './locales/ta/translation.json';
import guTranslations from './locales/gu/translation.json';
import knTranslations from './locales/kn/translation.json';
import paTranslations from './locales/pa/translation.json';
import mlTranslations from './locales/ml/translation.json';

const resources = {
  en: { translation: enTranslations },
  hi: { translation: hiTranslations },
  bn: { translation: bnTranslations },
  te: { translation: teTranslations },
  mr: { translation: mrTranslations },
  ta: { translation: taTranslations },
  gu: { translation: guTranslations },
  kn: { translation: knTranslations },
  pa: { translation: paTranslations },
  ml: { translation: mlTranslations },
};

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: 'en',
    debug: false,
    
    interpolation: {
      escapeValue: false, // React already escapes values
    },

    detection: {
      order: ['localStorage', 'navigator'],
      caches: ['localStorage'],
      lookupLocalStorage: 'language',
    },

    react: {
      useSuspense: false,
    },
  });

export default i18n;
