import React, { useState, useEffect } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import { qualityGradingService, type GradingRecord } from '@/services/qualityGradingService';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import type { QualityGrade } from '@/types';

interface GradingHistoryProps {
  onSelectRecord?: (record: GradingRecord) => void;
}

const gradeColors: Record<string, { bg: string; text: string }> = {
  A: { bg: 'bg-emerald-100 dark:bg-emerald-900/30', text: 'text-emerald-800 dark:text-emerald-200' },
  B: { bg: 'bg-blue-100 dark:bg-blue-900/30', text: 'text-blue-800 dark:text-blue-200' },
  FAQ: { bg: 'bg-sky-100 dark:bg-sky-900/30', text: 'text-sky-800 dark:text-sky-200' },
  C: { bg: 'bg-yellow-100 dark:bg-yellow-900/30', text: 'text-yellow-800 dark:text-yellow-200' },
  NonFAQ: { bg: 'bg-orange-100 dark:bg-orange-900/30', text: 'text-orange-800 dark:text-orange-200' },
  Reject: { bg: 'bg-red-100 dark:bg-red-900/30', text: 'text-red-800 dark:text-red-200' }
};

export const GradingHistory: React.FC<GradingHistoryProps> = ({ onSelectRecord }) => {
  const { t } = useLanguage();
  const [history, setHistory] = useState<GradingRecord[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [filterGrade, setFilterGrade] = useState<QualityGrade | 'all'>('all');
  const [dateRange, setDateRange] = useState<'7days' | '30days' | 'all'>('30days');

  useEffect(() => {
    loadHistory();
  }, [dateRange]);

  const loadHistory = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const endDate = new Date();
      let startDate: Date | undefined;

      if (dateRange === '7days') {
        startDate = new Date();
        startDate.setDate(startDate.getDate() - 7);
      } else if (dateRange === '30days') {
        startDate = new Date();
        startDate.setDate(startDate.getDate() - 30);
      }

      const records = await qualityGradingService.getGradingHistory(startDate, endDate);
      setHistory(records);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load history');
    } finally {
      setIsLoading(false);
    }
  };

  const filteredHistory = history.filter(record => {
    const matchesSearch = searchTerm === '' || 
      (record.produceType && record.produceType.toLowerCase().includes(searchTerm.toLowerCase())) ||
      (record.location && record.location.toLowerCase().includes(searchTerm.toLowerCase())) ||
      (record.recordId && record.recordId.toLowerCase().includes(searchTerm.toLowerCase()));
    
    const matchesGrade = filterGrade === 'all' || record.grade === filterGrade;

    return matchesSearch && matchesGrade;
  });

  const exportToCSV = () => {
    const headers = ['Date', 'Record ID', 'Produce Type', 'Grade', 'Price', 'Location'];
    const rows = filteredHistory.map(record => [
      new Date(record.timestamp).toLocaleDateString(),
      record.recordId,
      record.produceType,
      record.grade,
      record.certifiedPrice.toFixed(2),
      record.location
    ]);

    const csvContent = [
      headers.join(','),
      ...rows.map(row => row.join(','))
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `grading-history-${new Date().toISOString().split('T')[0]}.csv`;
    a.click();
    window.URL.revokeObjectURL(url);
  };

  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <LoadingSpinner size="lg" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg p-6">
        <p className="text-red-800 dark:text-red-200">{error}</p>
        <button
          onClick={loadHistory}
          className="mt-4 px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700"
        >
          {t('common.retry', 'Retry')}
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Filters and Search */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          {/* Search */}
          <div className="md:col-span-2">
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              {t('grading.search', 'Search')}
            </label>
            <input
              type="text"
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              placeholder={t('grading.searchPlaceholder', 'Search by produce type, location, or record ID')}
              className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
            />
          </div>

          {/* Grade Filter */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              {t('grading.filterByGrade', 'Filter by Grade')}
            </label>
            <select
              value={filterGrade}
              onChange={(e) => setFilterGrade(e.target.value as QualityGrade | 'all')}
              className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
            >
              <option value="all">{t('grading.allGrades', 'All Grades')}</option>
              <option value="A">{t('grading.gradeA', 'Grade A (Premium)')}</option>
              <option value="B">{t('grading.gradeB', 'Grade B (Good)')}</option>
              <option value="FAQ">{t('grading.gradeFAQ', 'FAQ (Fair Average)')}</option>
              <option value="C">{t('grading.gradeC', 'Grade C (Fair)')}</option>
              <option value="NonFAQ">{t('grading.gradeNonFAQ', 'Non-FAQ')}</option>
              <option value="Reject">{t('grading.gradeReject', 'Reject')}</option>
            </select>
          </div>

          {/* Date Range */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              {t('grading.dateRange', 'Date Range')}
            </label>
            <select
              value={dateRange}
              onChange={(e) => setDateRange(e.target.value as '7days' | '30days' | 'all')}
              className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100"
            >
              <option value="7days">{t('grading.last7Days', 'Last 7 Days')}</option>
              <option value="30days">{t('grading.last30Days', 'Last 30 Days')}</option>
              <option value="all">{t('grading.allTime', 'All Time')}</option>
            </select>
          </div>
        </div>

        {/* Export Button */}
        <div className="mt-4 flex justify-end">
          <button
            onClick={exportToCSV}
            disabled={filteredHistory.length === 0}
            className="px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center"
          >
            <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            {t('grading.exportCSV', 'Export CSV')}
          </button>
        </div>
      </div>

      {/* Results Count */}
      <div className="text-sm text-gray-600 dark:text-gray-400">
        {t('grading.showingResults', 'Showing')} {filteredHistory.length} {t('grading.of', 'of')} {history.length} {t('grading.records', 'records')}
      </div>

      {/* History List */}
      {filteredHistory.length === 0 ? (
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-12 text-center">
          <p className="text-gray-500 dark:text-gray-400">
            {t('grading.noRecordsFound', 'No grading records found')}
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {filteredHistory.map((record) => {
            const colors = gradeColors[record.grade] || gradeColors['FAQ'];
            return (
              <div
                key={record.recordId}
                onClick={() => onSelectRecord?.(record)}
                className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow cursor-pointer"
              >
                <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
                  <div className="flex-1">
                    <div className="flex flex-wrap items-center gap-2 mb-2">
                      <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
                        {record.produceType}
                      </h3>
                      <span className={`px-3 py-1 rounded-full text-sm font-bold ${colors.bg} ${colors.text}`}>
                        {record.grade}
                      </span>
                    </div>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-2 text-sm text-gray-600 dark:text-gray-400">
                      <div>
                        <span className="font-medium">{t('grading.location', 'Location')}:</span> {record.location}
                      </div>
                      <div>
                        <span className="font-medium">{t('grading.date', 'Date')}:</span> {new Date(record.timestamp).toLocaleDateString()}
                      </div>
                      <div>
                        <span className="font-medium">{t('grading.recordId', 'Record ID')}:</span> {record.recordId.substring(0, 8)}...
                      </div>
                    </div>
                  </div>
                  <div className="sm:text-right flex-shrink-0">
                    <p className="text-2xl sm:text-2xl font-bold text-green-600 dark:text-green-400">
                      ₹{record.certifiedPrice.toFixed(2)}
                    </p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">
                      {t('grading.perQuintal', 'per 100kg (quintal)')}
                    </p>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
