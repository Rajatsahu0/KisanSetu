import React, { useState, useEffect } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import { ChartContainer } from '@/components/charts/ChartContainer';
import { TrendChart } from '@/components/charts/TrendChart';
import { TimeRangeFilter } from '@/components/charts/TimeRangeFilter';
import { DataTypeSelector } from '@/components/charts/DataTypeSelector';
import { InsightsPanel } from '@/components/charts/InsightsPanel';
import { ExportButton } from '@/components/charts/ExportButton';
import { InsufficientDataMessage } from '@/components/charts/InsufficientDataMessage';
import { LoadingSpinner } from '@/components/common/LoadingSpinner';
import { TimeRange, CustomTimeRange } from '@/types/chartData';
import { generateMockHistoricalData } from '@/services/historicalDataService';
import { BarChart3 } from 'lucide-react';

type DataType = 'prices' | 'soilHealth' | 'qualityGrades' | 'all';

export const HistoricalDataPage: React.FC = () => {
  const { t } = useLanguage();
  const [selectedRange, setSelectedRange] = useState<TimeRange>('30days');
  const [customRange, setCustomRange] = useState<CustomTimeRange>();
  const [selectedDataType, setSelectedDataType] = useState<DataType>('all');
  const [loading, setLoading] = useState(false);
  const [data, setData] = useState<any>(null);

  useEffect(() => {
    loadHistoricalData();
  }, [selectedRange, selectedDataType, customRange]);

  const loadHistoricalData = async () => {
    setLoading(true);
    try {
      // Using mock data for now - replace with actual API call
      const mockData = generateMockHistoricalData(selectedDataType, selectedRange);
      
      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 800));
      
      setData(mockData);
    } catch (error) {
      console.error('Failed to load historical data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleRangeChange = (range: TimeRange, custom?: CustomTimeRange) => {
    setSelectedRange(range);
    if (custom) {
      setCustomRange(custom);
    }
  };

  const handleDataTypeChange = (type: DataType) => {
    setSelectedDataType(type);
  };

  const getDataTypeLabel = (type: DataType): string => {
    const labels: Record<DataType, string> = {
      all: t('history.allData', 'All Data'),
      prices: t('history.marketPrices', 'Market Prices'),
      soilHealth: t('history.soilHealth', 'Soil Health'),
      qualityGrades: t('history.qualityGrades', 'Quality Grades'),
    };
    return labels[type];
  };

  const getDateRangeLabel = (): string => {
    if (selectedRange === 'custom' && customRange) {
      return `${customRange.startDate.toLocaleDateString()} - ${customRange.endDate.toLocaleDateString()}`;
    }
    const labels: Record<TimeRange, string> = {
      '7days': t('history.last7Days', 'Last 7 Days'),
      '30days': t('history.last30Days', 'Last 30 Days'),
      'season': t('history.lastSeason', 'This Season'),
      'year': t('history.lastYear', 'This Year'),
      'custom': t('history.customRange', 'Custom Range'),
    };
    return labels[selectedRange];
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div className="flex items-center space-x-3 min-w-0">
            <BarChart3 className="w-7 h-7 sm:w-8 sm:h-8 text-blue-600 flex-shrink-0" />
            <div className="min-w-0">
              <h1 className="text-xl sm:text-3xl font-bold text-gray-900 dark:text-gray-100">
                {t('nav.historicalData', 'Historical Data')}
              </h1>
              <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                {t('history.subtitle', 'Analyze trends and patterns in your farming data')}
              </p>
            </div>
          </div>
          {data && !data.hasInsufficientData && (
            <ExportButton
              title={`${getDataTypeLabel(selectedDataType)} - ${getDateRangeLabel()}`}
              chartData={data.chartData}
              comparisonData={data.comparisonData}
              trendData={data.trendData}
              insights={data.insights}
              metadata={{
                dateRange: getDateRangeLabel(),
                dataType: getDataTypeLabel(selectedDataType),
                generatedAt: new Date().toLocaleString(),
              }}
            />
          )}
        </div>
      </div>

      {/* Filters */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <TimeRangeFilter
          selectedRange={selectedRange}
          customRange={customRange}
          onRangeChange={handleRangeChange}
        />
        <DataTypeSelector
          selectedType={selectedDataType}
          onTypeChange={handleDataTypeChange}
        />
      </div>

      {/* Loading State */}
      {loading && (
        <div className="flex justify-center items-center py-12">
          <LoadingSpinner size="lg" />
        </div>
      )}

      {/* Insufficient Data */}
      {!loading && data?.hasInsufficientData && (
        <InsufficientDataMessage
          dataType={getDataTypeLabel(selectedDataType)}
          minDataPoints={5}
          currentDataPoints={data.dataPointCount}
        />
      )}

      {/* Data Visualization */}
      {!loading && data && !data.hasInsufficientData && (
        <>
          {/* Main Chart */}
          <ChartContainer
            data={data.chartData}
            responsive={true}
            height={400}
          />

          {/* Trend Analysis */}
          {data.trendData && (
            <TrendChart
              data={data.trendData}
              comparisonPeriods={data.comparisonData?.periods}
              showAnnotations={true}
              timeRange={selectedRange}
              title={`${getDataTypeLabel(selectedDataType)} Trend Analysis`}
            />
          )}

          {/* AI Insights */}
          {data.insights && data.insights.length > 0 && (
            <InsightsPanel insights={data.insights} loading={false} />
          )}
        </>
      )}

      {/* Mobile Touch Instructions */}
      <div className="lg:hidden bg-blue-50 dark:bg-blue-900/20 rounded-lg p-4">
        <p className="text-sm text-blue-800 dark:text-blue-200">
          💡 Tip: Touch and drag on charts to explore data points. Pinch to zoom on mobile devices.
        </p>
      </div>
    </div>
  );
};
