import React, { useMemo } from 'react';
import { useLanguage } from '@/contexts/LanguageContext';
import type { SoilHealthData } from '@/types';

interface SoilHistoryChartProps {
  history: SoilHealthData[];
  nutrient: keyof Pick<SoilHealthData, 'pH' | 'organicCarbon' | 'nitrogen' | 'phosphorus' | 'potassium'>;
}

export const SoilHistoryChart: React.FC<SoilHistoryChartProps> = ({
  history,
  nutrient
}) => {
  const { t } = useLanguage();

  const chartData = useMemo(() => {
    if (!history || history.length === 0) return null;

    const sortedHistory = [...history].sort(
      (a, b) => new Date(a.collectionDate).getTime() - new Date(b.collectionDate).getTime()
    );

    const values = sortedHistory.map(item => item[nutrient] as number);
    const dates = sortedHistory.map(item => new Date(item.collectionDate));

    const minValue = Math.min(...values);
    const maxValue = Math.max(...values);
    const valueRange = maxValue - minValue || 1;

    return {
      values,
      dates,
      minValue,
      maxValue,
      valueRange,
      sortedHistory
    };
  }, [history, nutrient]);

  if (!chartData || chartData.values.length === 0) {
    return (
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
        <p className="text-center text-gray-500 dark:text-gray-400">
          {t('soilAnalysis.noHistoryData', 'No historical data available')}
        </p>
      </div>
    );
  }

  const { values, dates, minValue, maxValue, valueRange } = chartData;

  // Chart dimensions
  const width = 800;
  const height = 300;
  const padding = { top: 20, right: 20, bottom: 40, left: 60 };
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;

  // Calculate points for the line
  const points = values.map((value, index) => {
    const x = padding.left + (index / (values.length - 1 || 1)) * chartWidth;
    const y = padding.top + chartHeight - ((value - minValue) / valueRange) * chartHeight;
    return { x, y, value, date: dates[index] };
  });

  // Create path for the line
  const linePath = points
    .map((point, index) => `${index === 0 ? 'M' : 'L'} ${point.x} ${point.y}`)
    .join(' ');

  // Create path for the area under the line
  const areaPath = `${linePath} L ${points[points.length - 1].x} ${height - padding.bottom} L ${padding.left} ${height - padding.bottom} Z`;

  const getNutrientLabel = (nutrient: string): string => {
    const labels: Record<string, string> = {
      pH: t('soilAnalysis.ph', 'pH'),
      organicCarbon: t('soilAnalysis.organicCarbon', 'Organic Carbon'),
      nitrogen: t('soilAnalysis.nitrogen', 'Nitrogen'),
      phosphorus: t('soilAnalysis.phosphorus', 'Phosphorus'),
      potassium: t('soilAnalysis.potassium', 'Potassium')
    };
    return labels[nutrient] || nutrient;
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-6">
      <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">
        {getNutrientLabel(nutrient)} {t('soilAnalysis.trend', 'Trend')}
      </h3>
      
      <div className="overflow-x-auto">
        <svg
          viewBox={`0 0 ${width} ${height}`}
          className="w-full h-auto"
          style={{ maxHeight: '300px' }}
        >
          {/* Grid lines */}
          {[0, 0.25, 0.5, 0.75, 1].map((ratio) => {
            const y = padding.top + chartHeight * (1 - ratio);
            const value = minValue + valueRange * ratio;
            return (
              <g key={ratio}>
                <line
                  x1={padding.left}
                  y1={y}
                  x2={width - padding.right}
                  y2={y}
                  stroke="currentColor"
                  strokeWidth="1"
                  className="text-gray-200 dark:text-gray-700"
                  strokeDasharray="4"
                />
                <text
                  x={padding.left - 10}
                  y={y + 4}
                  textAnchor="end"
                  className="text-xs fill-gray-600 dark:fill-gray-400"
                >
                  {(value ?? 0).toFixed(1)}
                </text>
              </g>
            );
          })}

          {/* Area under the line */}
          <path
            d={areaPath}
            fill="currentColor"
            className="text-green-200 dark:text-green-900"
            opacity="0.3"
          />

          {/* Line */}
          <path
            d={linePath}
            fill="none"
            stroke="currentColor"
            strokeWidth="3"
            className="text-green-600 dark:text-green-400"
          />

          {/* Data points */}
          {points.map((point, index) => (
            <g key={index}>
              <circle
                cx={point.x}
                cy={point.y}
                r="5"
                fill="currentColor"
                className="text-green-600 dark:text-green-400"
              />
              <circle
                cx={point.x}
                cy={point.y}
                r="3"
                fill="white"
              />
            </g>
          ))}

          {/* X-axis labels (dates) */}
          {points.map((point, index) => {
            // Show labels for first, last, and every other point if there are many
            const showLabel = index === 0 || index === points.length - 1 || 
                            (points.length <= 6 || index % 2 === 0);
            if (!showLabel) return null;

            return (
              <text
                key={`date-${index}`}
                x={point.x}
                y={height - padding.bottom + 20}
                textAnchor="middle"
                className="text-xs fill-gray-600 dark:fill-gray-400"
              >
                {point.date.toLocaleDateString('en-US', { month: 'short', year: '2-digit' })}
              </text>
            );
          })}

          {/* Axis lines */}
          <line
            x1={padding.left}
            y1={padding.top}
            x2={padding.left}
            y2={height - padding.bottom}
            stroke="currentColor"
            strokeWidth="2"
            className="text-gray-400 dark:text-gray-600"
          />
          <line
            x1={padding.left}
            y1={height - padding.bottom}
            x2={width - padding.right}
            y2={height - padding.bottom}
            stroke="currentColor"
            strokeWidth="2"
            className="text-gray-400 dark:text-gray-600"
          />
        </svg>
      </div>

      {/* Legend */}
      <div className="mt-4 flex items-center justify-center space-x-6 text-sm">
        <div className="flex items-center">
          <div className="w-4 h-4 bg-green-600 rounded-full mr-2"></div>
          <span className="text-gray-700 dark:text-gray-300">
            {t('soilAnalysis.currentValue', 'Current')}: {(values[values.length - 1] ?? 0).toFixed(2)}
          </span>
        </div>
        <div className="flex items-center">
          <div className="w-4 h-1 bg-gray-400 mr-2"></div>
          <span className="text-gray-700 dark:text-gray-300">
            {t('soilAnalysis.range', 'Range')}: {(minValue ?? 0).toFixed(2)} - {(maxValue ?? 0).toFixed(2)}
          </span>
        </div>
      </div>
    </div>
  );
};
