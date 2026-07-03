import React, { useState } from 'react';
import { Clock, Star, Trash2, Search, ChevronRight } from 'lucide-react';
import { useLanguage } from '@/contexts/LanguageContext';
import { VoiceQueryResponse } from '@/types';

interface QueryHistoryItem {
  id: string;
  query: string;
  response: VoiceQueryResponse;
  timestamp: string;
  isFavorite: boolean;
}

interface QueryHistoryProps {
  history: QueryHistoryItem[];
  onSelectQuery: (item: QueryHistoryItem) => void;
  onToggleFavorite: (id: string) => void;
  onDeleteQuery: (id: string) => void;
}

export const QueryHistory: React.FC<QueryHistoryProps> = ({
  history,
  onSelectQuery,
  onToggleFavorite,
  onDeleteQuery
}) => {
  const { t } = useLanguage();
  const [searchTerm, setSearchTerm] = useState('');
  const [showFavoritesOnly, setShowFavoritesOnly] = useState(false);

  const filteredHistory = history.filter(item => {
    const matchesSearch = (item.query || '').toLowerCase().includes(searchTerm.toLowerCase()) ||
      (item.response?.transcription || '').toLowerCase().includes(searchTerm.toLowerCase());
    const matchesFavorite = !showFavoritesOnly || item.isFavorite;
    return matchesSearch && matchesFavorite;
  });

  const formatTimestamp = (timestamp: string): string => {
    try {
      const date = new Date(timestamp);
      
      // Check if date is valid
      if (isNaN(date.getTime())) {
        return t('voice.unknownTime', 'Unknown time');
      }
      
      const now = new Date();
      const diffMs = now.getTime() - date.getTime();
      const diffMins = Math.floor(diffMs / 60000);
      const diffHours = Math.floor(diffMs / 3600000);
      const diffDays = Math.floor(diffMs / 86400000);

      if (diffMins < 1) return t('voice.justNow', 'Just now');
      if (diffMins < 60) return t('voice.minutesAgo', '{{count}} minutes ago', { count: diffMins });
      if (diffHours < 24) return t('voice.hoursAgo', '{{count}} hours ago', { count: diffHours });
      if (diffDays < 7) return t('voice.daysAgo', '{{count}} days ago', { count: diffDays });
      
      return new Intl.DateTimeFormat('en-IN', {
        day: 'numeric',
        month: 'short',
        year: 'numeric'
      }).format(date);
    } catch (error) {
      console.error('Error formatting timestamp:', timestamp, error);
      return t('voice.unknownTime', 'Unknown time');
    }
  };

  const getPricesSummary = (item: QueryHistoryItem): string => {
    if (!item.response.prices || item.response.prices.length === 0) {
      return t('voice.noPrices', 'No prices');
    }
    if (item.response.prices.length === 1) {
      return `1 ${t('voice.price', 'price')}`;
    }
    return `${item.response.prices.length} ${t('voice.prices', 'prices')}`;
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md">
      {/* Header */}
      <div className="p-4 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
            {t('voice.queryHistory', 'Query History')}
          </h2>
          <button
            onClick={() => setShowFavoritesOnly(!showFavoritesOnly)}
            className={`flex items-center space-x-2 px-3 py-1.5 rounded-lg transition-colors ${
              showFavoritesOnly
                ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300'
                : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300'
            }`}
          >
            <Star className={`w-4 h-4 ${showFavoritesOnly ? 'fill-current' : ''}`} />
            <span className="text-sm">{t('voice.favorites', 'Favorites')}</span>
          </button>
        </div>

        {/* Search */}
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder={t('voice.searchHistory', 'Search history...')}
            className="w-full pl-10 pr-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 placeholder-gray-500 dark:placeholder-gray-400 focus:ring-2 focus:ring-green-500 focus:border-transparent"
          />
        </div>
      </div>

      {/* History List — scrollable on load, expands when response area grows */}
      <div className="divide-y divide-gray-200 dark:divide-gray-700 max-h-[calc(100vh-280px)] overflow-y-auto" style={{ scrollbarWidth: 'thin' }}>
        {filteredHistory.length === 0 ? (
          <div className="p-8 text-center">
            <Clock className="w-12 h-12 text-gray-400 mx-auto mb-3" />
            <p className="text-gray-500 dark:text-gray-400">
              {searchTerm
                ? t('voice.noMatchingQueries', 'No matching queries found')
                : showFavoritesOnly
                ? t('voice.noFavorites', 'No favorite queries yet')
                : t('voice.noHistory', 'No query history yet')}
            </p>
          </div>
        ) : (
          filteredHistory.map((item) => (
            <div
              key={item.id}
              className="p-4 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors"
            >
              <div className="flex items-start justify-between">
                <button
                  onClick={() => onSelectQuery(item)}
                  className="flex-1 text-left"
                >
                  <div className="flex items-start space-x-3">
                    <div className="flex-1 min-w-0">
                      {/* Query Text — wraps within box, no truncation */}
                      <p className="text-sm font-medium text-gray-900 dark:text-gray-100 break-words">
                        {item.response.transcription}
                      </p>
                      
                      {/* Metadata */}
                      <div className="mt-1 flex items-center space-x-3 text-xs text-gray-500 dark:text-gray-400">
                        <span className="flex items-center">
                          <Clock className="w-3 h-3 mr-1" />
                          {formatTimestamp(item.timestamp)}
                        </span>
                        <span>{getPricesSummary(item)}</span>
                        <span>{Math.round(item.response.confidence * 100)}% {t('voice.confidence', 'confidence')}</span>
                      </div>
                    </div>

                    <ChevronRight className="w-5 h-5 text-gray-400 flex-shrink-0" />
                  </div>
                </button>

                {/* Actions */}
                <div className="flex items-center space-x-2 ml-3">
                  <button
                    onClick={() => onToggleFavorite(item.id)}
                    className="p-1.5 rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors"
                    aria-label={item.isFavorite ? t('voice.removeFromFavorites', 'Remove from favorites') : t('voice.addToFavorites', 'Add to favorites')}
                  >
                    <Star
                      className={`w-5 h-5 ${
                        item.isFavorite
                          ? 'fill-yellow-400 text-yellow-400'
                          : 'text-gray-400'
                      }`}
                    />
                  </button>
                  <button
                    onClick={() => onDeleteQuery(item.id)}
                    className="p-1.5 rounded-lg hover:bg-red-100 dark:hover:bg-red-900/30 transition-colors"
                    aria-label={t('voice.deleteQuery', 'Delete query')}
                  >
                    <Trash2 className="w-5 h-5 text-red-500" />
                  </button>
                </div>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
