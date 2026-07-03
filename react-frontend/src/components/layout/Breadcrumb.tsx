import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { ChevronRightIcon, HomeIcon } from '@heroicons/react/24/outline';
import { useLanguage } from '@/contexts/LanguageContext';

interface BreadcrumbItem {
  label: string;
  path: string;
}

export const Breadcrumb: React.FC = () => {
  const location = useLocation();
  const { t } = useLanguage();

  // Map paths to readable labels
  const pathLabels: Record<string, string> = {
    dashboard: t('nav.dashboard', 'Dashboard'),
    'soil-analysis': t('nav.soilAnalysis', 'Soil Analysis'),
    'quality-grading': t('nav.qualityGrading', 'Quality Grading'),
    'voice-queries': t('nav.voiceQueries', 'Voice Queries'),
    'planting-advisory': t('nav.plantingAdvisory', 'Planting Advisory'),
    'historical-data': t('nav.historicalData', 'Historical Data'),
    help: t('footer.helpCenter', 'Help Center'),
    about: t('nav.about', 'About'),
  };

  // Generate breadcrumb items from current path
  const generateBreadcrumbs = (): BreadcrumbItem[] => {
    const pathSegments = location.pathname.split('/').filter(Boolean);
    
    const breadcrumbs: BreadcrumbItem[] = [];
    let currentPath = '';

    pathSegments.forEach((segment) => {
      currentPath += `/${segment}`;
      const label = pathLabels[segment] || segment.charAt(0).toUpperCase() + segment.slice(1);
      breadcrumbs.push({ label, path: currentPath });
    });

    return breadcrumbs;
  };

  const breadcrumbs = generateBreadcrumbs();

  // Don't show breadcrumbs on home/dashboard or root
  if (breadcrumbs.length === 0 || location.pathname === '/dashboard' || location.pathname === '/') {
    return null;
  }

  return (
    <nav aria-label={t('nav.breadcrumb', 'Breadcrumb')} className="mb-4">
      <ol className="flex items-center space-x-2 text-sm">
        {/* Home Link */}
        <li>
          <Link
            to="/dashboard"
            className="text-gray-500 hover:text-primary-600 dark:text-gray-400 dark:hover:text-primary-400 transition-colors"
            aria-label={t('nav.home', 'Home')}
          >
            <HomeIcon className="h-5 w-5" />
          </Link>
        </li>

        {/* Breadcrumb Items */}
        {breadcrumbs.map((item, index) => {
          const isLast = index === breadcrumbs.length - 1;

          return (
            <React.Fragment key={item.path}>
              <li className="flex items-center space-x-2">
                <ChevronRightIcon className="h-4 w-4 text-gray-400" />
                {isLast ? (
                  <span
                    className="text-gray-900 dark:text-gray-100 font-medium"
                    aria-current="page"
                  >
                    {item.label}
                  </span>
                ) : (
                  <Link
                    to={item.path}
                    className="text-gray-500 hover:text-primary-600 dark:text-gray-400 dark:hover:text-primary-400 transition-colors"
                  >
                    {item.label}
                  </Link>
                )}
              </li>
            </React.Fragment>
          );
        })}
      </ol>
    </nav>
  );
};
