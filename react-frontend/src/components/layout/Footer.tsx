import React from 'react';
import { Link } from 'react-router-dom';
import { useLanguage } from '@/contexts/LanguageContext';
import { GlobeAltIcon, PhoneIcon, EnvelopeIcon } from '@heroicons/react/24/outline';

export const Footer: React.FC = () => {
  const { t, currentLanguage, changeLanguage, supportedLanguages } = useLanguage();

  return (
    <footer 
      className="bg-gray-800 dark:bg-gray-950 text-gray-300 mt-auto"
      role="contentinfo"
    >
      <div className="px-2 sm:px-4 lg:px-6 py-8">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-8">
          {/* About Section */}
          <div>
            <h3 className="text-white font-semibold mb-4 flex items-center space-x-2">
              <span className="text-2xl" role="img" aria-label="Wheat emoji">🌾</span>
              <span>KisanMitra AI</span>
            </h3>
            <p className="text-sm text-gray-400">
              {t('footer.about', 'Your intelligent farming assistant for soil analysis, quality grading, and agricultural advisory.')}
            </p>
          </div>

          {/* Quick Links */}
          <nav aria-label={t('footer.quickLinksNav', 'Footer quick links')}>
            <h3 className="text-white font-semibold mb-4">{t('footer.quickLinks', 'Quick Links')}</h3>
            <ul className="space-y-2 text-sm">
              <li>
                <Link 
                  to="/dashboard" 
                  className="hover:text-primary-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 rounded px-1"
                >
                  {t('nav.dashboard', 'Dashboard')}
                </Link>
              </li>
              <li>
                <Link 
                  to="/soil-analysis" 
                  className="hover:text-primary-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 rounded px-1"
                >
                  {t('nav.soilAnalysis', 'Soil Analysis')}
                </Link>
              </li>
              <li>
                <Link 
                  to="/planting-advisory" 
                  className="hover:text-primary-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 rounded px-1"
                >
                  {t('nav.plantingAdvisory', 'Planting Advisory')}
                </Link>
              </li>
              <li>
                <Link 
                  to="/quality-grading" 
                  className="hover:text-primary-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 rounded px-1"
                >
                  {t('nav.qualityGrading', 'Quality Grading')}
                </Link>
              </li>
              <li>
                <Link 
                  to="/voice-queries" 
                  className="hover:text-primary-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 rounded px-1"
                >
                  {t('nav.voiceQueries', 'Voice Queries')}
                </Link>
              </li>
            </ul>
          </nav>

          {/* Support */}
          <div>
            <h3 className="text-white font-semibold mb-4">{t('footer.support', 'Support')}</h3>
            <ul className="space-y-2 text-sm">
              <li className="flex items-center space-x-2">
                <PhoneIcon className="h-4 w-4" aria-hidden="true" />
                <a 
                  href="tel:1800-XXX-XXXX" 
                  className="hover:text-primary-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 rounded px-1"
                  aria-label={t('footer.phoneAriaLabel', 'Call support at 1800-XXX-XXXX')}
                >
                  1800-XXX-XXXX
                </a>
              </li>
              <li className="flex items-center space-x-2">
                <EnvelopeIcon className="h-4 w-4" aria-hidden="true" />
                <a 
                  href="mailto:support@kisanmitra.ai" 
                  className="hover:text-primary-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 rounded px-1"
                  aria-label={t('footer.emailAriaLabel', 'Email support at support@kisanmitra.ai')}
                >
                  support@kisanmitra.ai
                </a>
              </li>
              <li>
                <Link 
                  to="/about" 
                  className="hover:text-primary-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 rounded px-1"
                >
                  {t('footer.helpCenter', 'Help Center')}
                </Link>
              </li>
              <li>
                <Link 
                  to="/about" 
                  className="hover:text-primary-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 rounded px-1"
                >
                  {t('footer.faq', 'FAQ')}
                </Link>
              </li>
            </ul>
          </div>

          {/* Language Selection */}
          <div>
            <h3 className="text-white font-semibold mb-4 flex items-center space-x-2">
              <GlobeAltIcon className="h-5 w-5" aria-hidden="true" />
              <span>{t('footer.language', 'Language')}</span>
            </h3>
            <label htmlFor="language-selector" className="sr-only">
              {t('footer.selectLanguage', 'Select language')}
            </label>
            <select
              id="language-selector"
              value={currentLanguage.code}
              onChange={(e) => changeLanguage(e.target.value)}
              className="w-full px-3 py-2 bg-gray-700 text-white rounded-lg border border-gray-600 focus:outline-none focus:ring-2 focus:ring-primary-500 text-sm"
              aria-label={t('footer.selectLanguage', 'Select language')}
            >
              {supportedLanguages.map((lang) => (
                <option key={lang.code} value={lang.code}>
                  {lang.nativeName}
                </option>
              ))}
            </select>
            <p className="text-xs text-gray-400 mt-2">
              {t('footer.languageNote', 'Select your preferred language')}
            </p>
          </div>
        </div>

        {/* Bottom Bar */}
        <div className="border-t border-gray-700 mt-8 pt-6 flex flex-col sm:flex-row justify-between items-center text-sm text-gray-400 gap-4">
          <p>
            &copy; {new Date().getFullYear()} KisanMitra AI. {t('footer.rights', 'All rights reserved.')}
          </p>
          <nav 
            className="flex space-x-4 mt-4 sm:mt-0"
            aria-label={t('footer.legalNav', 'Legal information')}
          >
            <Link 
              to="/dashboard" 
              className="hover:text-primary-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 rounded px-1"
            >
              {t('footer.privacy', 'Privacy Policy')}
            </Link>
            <Link 
              to="/dashboard" 
              className="hover:text-primary-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 rounded px-1"
            >
              {t('footer.terms', 'Terms of Service')}
            </Link>
          </nav>
        </div>
      </div>
    </footer>
  );
};
