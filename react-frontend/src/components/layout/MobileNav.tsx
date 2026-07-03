import React, { useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useLanguage } from '@/contexts/LanguageContext';
import { cn } from '@/utils/cn';
import {
  HomeIcon,
  BeakerIcon,
  StarIcon,
  MicrophoneIcon,
  CalendarIcon,
  ChartBarIcon,
  UserCircleIcon,
  InformationCircleIcon,
} from '@heroicons/react/24/outline';

interface MobileNavProps {
  isOpen: boolean;
  onClose: () => void;
}

export const MobileNav: React.FC<MobileNavProps> = ({ isOpen, onClose }) => {
  const { t } = useLanguage();
  const location = useLocation();

  // Close menu on route change
  const prevPathRef = React.useRef(location.pathname);
  useEffect(() => {
    if (prevPathRef.current !== location.pathname) {
      onClose();
      prevPathRef.current = location.pathname;
    }
  }, [location.pathname, onClose]);

  // Prevent body scroll when menu is open
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen]);

  // Handle escape key to close menu
  useEffect(() => {
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && isOpen) {
        onClose();
      }
    };

    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, [isOpen, onClose]);

  const navItems = [
    {
      to: '/dashboard',
      icon: HomeIcon,
      label: t('nav.dashboard', 'Dashboard'),
    },
    {
      to: '/soil-analysis',
      icon: BeakerIcon,
      label: t('nav.soilAnalysis', 'Soil Analysis'),
    },
    {
      to: '/planting-advisory',
      icon: CalendarIcon,
      label: t('nav.plantingAdvisory', 'Planting Advisory'),
    },
    {
      to: '/quality-grading',
      icon: StarIcon,
      label: t('nav.qualityGrading', 'Quality Grading'),
    },
    {
      to: '/voice-queries',
      icon: MicrophoneIcon,
      label: t('nav.voiceQueries', 'Voice Queries'),
    },
    {
      to: '/historical-data',
      icon: ChartBarIcon,
      label: t('nav.historicalData', 'Historical Data'),
    },
    {
      to: '/profile',
      icon: UserCircleIcon,
      label: t('nav.profile', 'Profile'),
    },
    {
      to: '/about',
      icon: InformationCircleIcon,
      label: t('nav.about', 'About'),
    },
  ];

  return (
    <>
      {/* Backdrop */}
      <div
        className={cn(
          'fixed inset-0 bg-black bg-opacity-50 z-40 lg:hidden transition-opacity duration-300',
          isOpen ? 'opacity-100' : 'opacity-0 pointer-events-none'
        )}
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Mobile Menu */}
      <nav
        id="mobile-navigation"
        className={cn(
          'fixed top-16 left-0 bottom-0 w-64 bg-white dark:bg-gray-800 shadow-xl z-50 lg:hidden transform transition-transform duration-300 ease-in-out overflow-y-auto',
          isOpen ? 'translate-x-0' : '-translate-x-full pointer-events-none'
        )}
        aria-label={t('nav.mobileMenu', 'Mobile navigation')}
        aria-hidden={!isOpen}
      >
        <div className="py-4">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.to;

            return (
              <Link
                key={item.to}
                to={item.to}
                className={cn(
                  'flex items-center space-x-3 px-6 py-3 transition-colors focus:outline-none focus:ring-2 focus:ring-inset focus:ring-primary-500',
                  isActive
                    ? 'bg-primary-50 dark:bg-primary-900 text-primary-600 dark:text-primary-300 border-l-4 border-primary-600'
                    : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
                )}
                aria-current={isActive ? 'page' : undefined}
                tabIndex={isOpen ? 0 : -1}
              >
                <Icon className="h-6 w-6" aria-hidden="true" />
                <span className="font-medium">{item.label}</span>
              </Link>
            );
          })}
        </div>
      </nav>
    </>
  );
};
