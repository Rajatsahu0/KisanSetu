import React, { useState } from 'react';
import { TimeRange, CustomTimeRange } from '@/types/chartData';
import { Calendar } from 'lucide-react';
import { useLanguage } from '@/contexts/LanguageContext';

interface TimeRangeFilterProps {
  selectedRange: TimeRange;
  customRange?: CustomTimeRange;
  onRangeChange: (range: TimeRange, customRange?: CustomTimeRange) => void;
}

export const TimeRangeFilter: React.FC<TimeRangeFilterProps> = ({
  selectedRange, customRange, onRangeChange,
}) => {
  const { t } = useLanguage();
  const [showCustomPicker, setShowCustomPicker] = useState(false);
  const [startDate, setStartDate] = useState(
    customRange?.startDate ? new Date(customRange.startDate).toISOString().split('T')[0] : ''
  );
  const [endDate, setEndDate] = useState(
    customRange?.endDate ? new Date(customRange.endDate).toISOString().split('T')[0] : ''
  );

  const timeRanges: { value: TimeRange; label: string }[] = [
    { value: '7days', label: t('history.last7Days', 'Last 7 Days') },
    { value: '30days', label: t('history.last30Days', 'Last 30 Days') },
    { value: 'season', label: t('history.lastSeason', 'This Season') },
    { value: 'year', label: t('history.lastYear', 'This Year') },
    { value: 'custom', label: t('history.customRange', 'Custom Range') },
  ];

  const handleRangeClick = (range: TimeRange) => {
    if (range === 'custom') { setShowCustomPicker(true); } else { setShowCustomPicker(false); onRangeChange(range); }
  };

  const handleCustomRangeApply = () => {
    if (startDate && endDate) {
      onRangeChange('custom', { startDate: new Date(startDate), endDate: new Date(endDate) });
      setShowCustomPicker(false);
    }
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-4">
      <div className="flex items-center space-x-2 mb-3">
        <Calendar className="w-5 h-5 text-gray-600 dark:text-gray-400" />
        <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100">{t('history.timePeriod', 'Time Period')}</h3>
      </div>
      <div className="flex flex-wrap gap-2">
        {timeRanges.map((range) => (
          <button key={range.value} onClick={() => handleRangeClick(range.value)}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${selectedRange === range.value ? 'bg-blue-600 text-white' : 'bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600'}`}>
            {range.label}
          </button>
        ))}
      </div>
      {showCustomPicker && (
        <div className="mt-4 p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">{t('history.startDate', 'Start Date')}</label>
              <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-100" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">{t('history.endDate', 'End Date')}</label>
              <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-100" />
            </div>
          </div>
          <div className="mt-4 flex justify-end space-x-2">
            <button onClick={() => setShowCustomPicker(false)} className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-200 dark:bg-gray-600 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-500">{t('app.cancel', 'Cancel')}</button>
            <button onClick={handleCustomRangeApply} disabled={!startDate || !endDate} className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed">{t('app.apply', 'Apply')}</button>
          </div>
        </div>
      )}
    </div>
  );
};
