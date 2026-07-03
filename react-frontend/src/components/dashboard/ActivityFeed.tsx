import React, { useState } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';

export interface ActivityItem {
  id: string;
  icon: string;
  title: string;
  description: string;
  timestamp: string;
  type: 'user' | 'system' | 'alert';
  path?: string;
}

interface ActivityFeedProps {
  items: ActivityItem[];
  isLoading?: boolean;
  onItemClick?: (item: ActivityItem) => void;
}

const FILTERS = [
  { key: 'all', label: 'dashboard.filterAll' },
  { key: 'user', label: 'dashboard.filterUser' },
  { key: 'system', label: 'dashboard.filterSystem' },
  { key: 'alert', label: 'dashboard.filterAlerts' },
] as const;

const typeColors = {
  user: 'bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400',
  system: 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400',
  alert: 'bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400',
};

const formatTimeAgo = (ts: string): string => {
  const diff = Date.now() - new Date(ts).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return 'Just now';
  if (mins < 60) return `${mins}m ago`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h ago`;
  const days = Math.floor(hrs / 24);
  if (days === 1) return 'Yesterday';
  return `${days}d ago`;
};

export const ActivityFeed: React.FC<ActivityFeedProps> = ({ items, isLoading, onItemClick }) => {
  const [filter, setFilter] = useState<string>('all');
  const { t } = useLanguage();

  const filtered = filter === 'all' ? items : items.filter(i => i.type === filter);

  if (isLoading) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 p-5">
        <div className="h-6 w-32 bg-gray-200 dark:bg-gray-700 rounded mb-4 animate-pulse" />
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="flex gap-3 py-3 animate-pulse">
            <div className="w-9 h-9 bg-gray-200 dark:bg-gray-700 rounded-lg flex-shrink-0" />
            <div className="flex-1">
              <div className="h-4 w-3/4 bg-gray-200 dark:bg-gray-700 rounded mb-2" />
              <div className="h-3 w-1/2 bg-gray-200 dark:bg-gray-700 rounded" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 p-5 h-full">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-base font-semibold text-gray-900 dark:text-white">{t('dashboard.activity', 'Activity')}</h3>
      </div>

      {/* Filters */}
      <div className="flex gap-1 mb-4">
        {FILTERS.map(f => (
          <button key={f.key} onClick={() => setFilter(f.key)}
            className={`px-3 py-1 text-xs rounded-full transition-colors ${
              filter === f.key
                ? 'bg-green-600 text-white'
                : 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-600'
            }`}>
            {f.label.startsWith('dashboard.') ? t(f.label, f.key) : f.label}
          </button>
        ))}
      </div>

      {/* Items */}
      <div className="space-y-1 max-h-[400px] overflow-y-auto">
        {filtered.length === 0 ? (
          <p className="text-sm text-gray-400 dark:text-gray-500 text-center py-8">{t('dashboard.noActivity', 'No activity to show')}</p>
        ) : (
          filtered.map(item => (
            <button key={item.id} onClick={() => onItemClick?.(item)}
              className="w-full flex items-start gap-3 p-2.5 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors text-left">
              <div className={`w-9 h-9 rounded-lg flex items-center justify-center flex-shrink-0 text-lg ${typeColors[item.type]}`}>
                {item.icon}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-900 dark:text-white truncate">{item.title}</p>
                <p className="text-xs text-gray-500 dark:text-gray-400 truncate">{item.description}</p>
              </div>
              <span className="text-[10px] text-gray-400 dark:text-gray-500 flex-shrink-0 mt-0.5">
                {formatTimeAgo(item.timestamp)}
              </span>
            </button>
          ))
        )}
      </div>
    </div>
  );
};
