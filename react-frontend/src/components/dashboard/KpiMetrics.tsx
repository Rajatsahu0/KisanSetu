import React from 'react';

export interface KpiCard {
  icon: string;
  label: string;
  value: string | number;
  trend?: 'up' | 'down' | 'stable';
  change?: string;
}

interface KpiMetricsProps {
  cards: KpiCard[];
  isLoading?: boolean;
}

const trendConfig = {
  up: { arrow: '↑', color: 'text-green-600 dark:text-green-400', bg: 'bg-green-50 dark:bg-green-900/20' },
  down: { arrow: '↓', color: 'text-red-600 dark:text-red-400', bg: 'bg-red-50 dark:bg-red-900/20' },
  stable: { arrow: '→', color: 'text-gray-500 dark:text-gray-400', bg: 'bg-gray-50 dark:bg-gray-800' },
};

export const KpiMetrics: React.FC<KpiMetricsProps> = ({ cards, isLoading }) => {
  if (isLoading) {
    return (
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="bg-white dark:bg-gray-800 rounded-xl p-5 shadow-sm animate-pulse">
            <div className="h-8 w-8 bg-gray-200 dark:bg-gray-700 rounded-lg mb-3" />
            <div className="h-8 w-20 bg-gray-200 dark:bg-gray-700 rounded mb-2" />
            <div className="h-4 w-24 bg-gray-200 dark:bg-gray-700 rounded" />
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
      {cards.map((card, i) => {
        const t = trendConfig[card.trend || 'stable'];
        return (
          <div key={i} className="bg-white dark:bg-gray-800 rounded-xl p-5 shadow-sm hover:shadow-md transition-shadow border border-gray-100 dark:border-gray-700">
            <div className="flex items-center justify-between mb-3">
              <span className="text-2xl">{card.icon}</span>
              {card.change && (
                <span className={`text-xs font-semibold px-2 py-0.5 rounded-full ${t.bg} ${t.color}`}>
                  {t.arrow} {card.change}
                </span>
              )}
            </div>
            <div className="text-2xl sm:text-3xl font-bold text-gray-900 dark:text-white mb-1">{card.value}</div>
            <div className="text-xs sm:text-sm text-gray-500 dark:text-gray-400">{card.label}</div>
          </div>
        );
      })}
    </div>
  );
};
