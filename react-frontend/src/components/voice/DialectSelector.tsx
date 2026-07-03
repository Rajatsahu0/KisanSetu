import React from 'react';
import { Check, ChevronDown } from 'lucide-react';
import { useLanguage } from '@/contexts/LanguageContext';
import { Dialect } from '@/types';

interface DialectSelectorProps {
  selectedDialect: string;
  onDialectChange: (dialect: string) => void;
  availableDialects: Dialect[];
}

export const DialectSelector: React.FC<DialectSelectorProps> = ({
  selectedDialect,
  onDialectChange,
  availableDialects
}) => {
  const { t } = useLanguage();
  const [isOpen, setIsOpen] = React.useState(false);

  const selectedDialectInfo = availableDialects.find(d => d.code === selectedDialect);

  const handleSelect = (dialectCode: string) => {
    onDialectChange(dialectCode);
    setIsOpen(false);
  };

  return (
    <div className="relative">
      <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
        {t('voice.selectDialect', 'Select Dialect')}
      </label>

      {/* Dropdown Button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg px-4 py-3 flex items-center justify-between hover:border-green-500 dark:hover:border-green-500 transition-colors"
        aria-label={t('voice.dialectSelector', 'Dialect selector')}
        aria-expanded={isOpen}
      >
        <div className="flex flex-col items-start">
          <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
            {selectedDialectInfo?.name || t('voice.selectDialect', 'Select Dialect')}
          </span>
          {selectedDialectInfo && (
            <span className="text-xs text-gray-500 dark:text-gray-400">
              {selectedDialectInfo.nativeName} • {selectedDialectInfo.region}
            </span>
          )}
        </div>
        <ChevronDown
          className={`w-5 h-5 text-gray-500 transition-transform ${
            isOpen ? 'transform rotate-180' : ''
          }`}
        />
      </button>

      {/* Dropdown Menu */}
      {isOpen && (
        <>
          {/* Backdrop */}
          <div
            className="fixed inset-0 z-10"
            onClick={() => setIsOpen(false)}
            aria-hidden="true"
          />

          {/* Menu */}
          <div className="absolute z-20 w-full mt-2 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-lg max-h-80 overflow-y-auto">
            {availableDialects.map((dialect) => (
              <button
                key={dialect.code}
                onClick={() => handleSelect(dialect.code)}
                className={`w-full px-4 py-3 flex items-center justify-between hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors ${
                  dialect.code === selectedDialect ? 'bg-green-50 dark:bg-green-900/20' : ''
                }`}
              >
                <div className="flex flex-col items-start">
                  <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
                    {dialect.name}
                  </span>
                  <span className="text-xs text-gray-500 dark:text-gray-400">
                    {dialect.nativeName} • {dialect.region}
                  </span>
                </div>
                {dialect.code === selectedDialect && (
                  <Check className="w-5 h-5 text-green-500" />
                )}
              </button>
            ))}
          </div>
        </>
      )}
    </div>
  );
};
