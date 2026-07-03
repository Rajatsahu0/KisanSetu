import React, { Fragment, useState, useEffect, useRef, useCallback } from 'react';
import { Listbox, Transition } from '@headlessui/react';
import { CheckIcon, LanguageIcon, XMarkIcon } from '@heroicons/react/24/outline';
import { useLanguage } from '@/contexts/LanguageContext';
import { clsx } from 'clsx';

const NUDGE_KEY = 'kisanmitra_lang_selected';
const POSITION_KEY = 'kisanmitra_lang_pos';
const MOBILE_BREAKPOINT = 1024; // lg breakpoint

const getStoredPosition = (): { x: number; y: number } | null => {
  try {
    const stored = localStorage.getItem(POSITION_KEY);
    if (stored) return JSON.parse(stored);
  } catch {}
  return null;
};

export const FloatingLanguageToggle: React.FC = () => {
  const { currentLanguage, changeLanguage, supportedLanguages, t } = useLanguage();
  const [showNudge, setShowNudge] = useState(false);
  const [isMobile, setIsMobile] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const [position, setPosition] = useState<{ x: number; y: number } | null>(null);
  const dragRef = useRef<HTMLDivElement>(null);
  const dragStartRef = useRef<{ startX: number; startY: number; origX: number; origY: number } | null>(null);
  const wasDraggedRef = useRef(false);

  // Detect mobile
  useEffect(() => {
    const check = () => setIsMobile(window.innerWidth < MOBILE_BREAKPOINT);
    check();
    window.addEventListener('resize', check);
    return () => window.removeEventListener('resize', check);
  }, []);

  // Load stored position on mobile
  useEffect(() => {
    if (isMobile) {
      const stored = getStoredPosition();
      if (stored) {
        const clamped = clampPosition(stored.x, stored.y);
        setPosition(clamped);
      }
    } else {
      setPosition(null);
    }
  }, [isMobile]);

  // Nudge logic
  useEffect(() => {
    if (!localStorage.getItem(NUDGE_KEY)) {
      const timer = setTimeout(() => setShowNudge(true), 1500);
      return () => clearTimeout(timer);
    }
  }, []);

  useEffect(() => {
    if (!showNudge) return;
    const timer = setTimeout(() => {
      localStorage.setItem(NUDGE_KEY, 'true');
      setShowNudge(false);
    }, 5000);
    return () => clearTimeout(timer);
  }, [showNudge]);

  const clampPosition = (x: number, y: number) => {
    const size = 48;
    const pad = 8;
    return {
      x: Math.max(pad, Math.min(x, window.innerWidth - size - pad)),
      y: Math.max(pad, Math.min(y, window.innerHeight - size - pad)),
    };
  };

  // Touch drag handlers (mobile only)
  const handleTouchStart = useCallback((e: React.TouchEvent) => {
    if (!isMobile) return;
    const touch = e.touches[0];
    const el = dragRef.current;
    if (!el) return;
    const rect = el.getBoundingClientRect();
    dragStartRef.current = {
      startX: touch.clientX,
      startY: touch.clientY,
      origX: rect.left,
      origY: rect.top,
    };
    wasDraggedRef.current = false;
  }, [isMobile]);

  const handleTouchMove = useCallback((e: React.TouchEvent) => {
    if (!isMobile || !dragStartRef.current) return;
    const touch = e.touches[0];
    const dx = touch.clientX - dragStartRef.current.startX;
    const dy = touch.clientY - dragStartRef.current.startY;

    // Only start dragging after 8px movement (prevents accidental drags on tap)
    if (!wasDraggedRef.current && Math.abs(dx) < 8 && Math.abs(dy) < 8) return;

    wasDraggedRef.current = true;
    setIsDragging(true);
    e.preventDefault();

    const newX = dragStartRef.current.origX + dx;
    const newY = dragStartRef.current.origY + dy;
    setPosition(clampPosition(newX, newY));
  }, [isMobile]);

  const handleTouchEnd = useCallback(() => {
    if (!isMobile) return;
    dragStartRef.current = null;
    setIsDragging(false);
    if (wasDraggedRef.current && position) {
      localStorage.setItem(POSITION_KEY, JSON.stringify(position));
    }
  }, [isMobile, position]);

  const handleLanguageChange = (code: string) => {
    changeLanguage(code);
    localStorage.setItem(NUDGE_KEY, 'true');
    setShowNudge(false);
  };

  const dismissNudge = (e: React.MouseEvent) => {
    e.stopPropagation();
    localStorage.setItem(NUDGE_KEY, 'true');
    setShowNudge(false);
  };

  // Dropdown position: open upward if toggle is in bottom half, downward if top half
  const dropdownAbove = position ? position.y > window.innerHeight / 2 : true;

  const containerStyle: React.CSSProperties = isMobile && position
    ? { position: 'fixed', left: position.x, top: position.y, right: 'auto', transform: 'none', zIndex: 50 }
    : {};

  const containerClass = isMobile && position
    ? ''
    : 'fixed right-4 top-1/2 -translate-y-1/2 z-50';

  return (
    <div
      ref={dragRef}
      className={containerClass}
      style={containerStyle}
      role="region"
      aria-label={t('settings.language') || 'Language selector'}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
    >
      {/* Nudge message */}
      {showNudge && !isDragging && (
        <div className="absolute right-14 sm:right-16 md:right-[4.5rem] top-1/2 -translate-y-1/2 animate-fade-in">
          <div className="relative bg-white dark:bg-gray-800 border border-green-300 dark:border-green-700 rounded-lg shadow-xl px-3 sm:px-4 py-2 sm:py-2.5 flex items-center gap-2 max-w-[calc(100vw-5rem)]">
            <div className="absolute -right-2 top-1/2 -translate-y-1/2 w-0 h-0 border-t-[6px] border-t-transparent border-b-[6px] border-b-transparent border-l-[8px] border-l-green-300 dark:border-l-green-700" />
            <div className="absolute -right-[7px] top-1/2 -translate-y-1/2 w-0 h-0 border-t-[5px] border-t-transparent border-b-[5px] border-b-transparent border-l-[7px] border-l-white dark:border-l-gray-800" />
            <span className="text-xs sm:text-sm text-gray-800 dark:text-gray-200 font-medium">
              🌐 भाषा चुनें / Select Language
            </span>
            <button
              onClick={dismissNudge}
              className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 ml-1"
              aria-label="Dismiss"
            >
              <XMarkIcon className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      <Listbox value={currentLanguage.code} onChange={handleLanguageChange}>
        {({ open }) => (
          <div className="relative">
            <Listbox.Button
              className={clsx(
                'flex items-center justify-center',
                'w-12 h-12 md:w-14 md:h-14',
                'rounded-full shadow-lg',
                'bg-white dark:bg-gray-800',
                'border-2 border-primary-500 dark:border-primary-400',
                'hover:bg-primary-50 dark:hover:bg-gray-700',
                'focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2',
                'transition-all duration-200',
                'cursor-pointer select-none',
                isDragging && 'scale-110 shadow-2xl opacity-80',
                showNudge && !isDragging && 'animate-pulse ring-2 ring-green-400 ring-offset-2'
              )}
              aria-label={`${t('settings.language') || 'Language'}: ${currentLanguage.nativeName}`}
              onClick={(e: React.MouseEvent) => {
                // Don't open dropdown if user just finished dragging
                if (wasDraggedRef.current) {
                  e.preventDefault();
                  wasDraggedRef.current = false;
                  return;
                }
                if (showNudge) {
                  localStorage.setItem(NUDGE_KEY, 'true');
                  setShowNudge(false);
                }
              }}
            >
              <LanguageIcon
                className="h-6 w-6 md:h-7 md:w-7 text-primary-600 dark:text-primary-400"
                aria-hidden="true"
              />
            </Listbox.Button>

            <Transition
              as={Fragment}
              show={open && !isDragging}
              enter="transition ease-out duration-100"
              enterFrom="transform opacity-0 scale-95"
              enterTo="transform opacity-100 scale-100"
              leave="transition ease-in duration-75"
              leaveFrom="transform opacity-100 scale-100"
              leaveTo="transform opacity-0 scale-95"
            >
              <Listbox.Options
                className={clsx(
                  'absolute right-0',
                  dropdownAbove ? 'bottom-full mb-2' : 'top-full mt-2',
                  'w-48 md:w-56',
                  dropdownAbove ? 'origin-bottom-right' : 'origin-top-right',
                  'rounded-lg shadow-xl',
                  'bg-white dark:bg-gray-800',
                  'border border-gray-200 dark:border-gray-700',
                  'py-1',
                  'focus:outline-none',
                  'max-h-60 overflow-auto'
                )}
              >
                {supportedLanguages.map((language) => (
                  <Listbox.Option
                    key={language.code}
                    value={language.code}
                    className={({ active }) =>
                      clsx(
                        'relative cursor-pointer select-none py-3 pl-10 pr-4',
                        active
                          ? 'bg-primary-100 dark:bg-primary-900 text-primary-900 dark:text-primary-100'
                          : 'text-gray-900 dark:text-gray-100'
                      )
                    }
                  >
                    {({ selected }) => (
                      <>
                        <span className={clsx('block truncate', selected ? 'font-semibold' : 'font-normal')}>
                          {language.nativeName}
                        </span>
                        {selected && (
                          <span className="absolute inset-y-0 left-0 flex items-center pl-3 text-primary-600 dark:text-primary-400">
                            <CheckIcon className="h-5 w-5" aria-hidden="true" />
                          </span>
                        )}
                      </>
                    )}
                  </Listbox.Option>
                ))}
              </Listbox.Options>
            </Transition>
          </div>
        )}
      </Listbox>
    </div>
  );
};
