import React from 'react';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  ChartOptions,
} from 'chart.js';
import { Line } from 'react-chartjs-2';
import { TrendLineData, PeriodChartData, TimeRange } from '@/types/chartData';
import { TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { useLanguage } from '@/contexts/LanguageContext';

ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

interface TrendChartProps {
  data: TrendLineData;
  comparisonPeriods?: PeriodChartData[];
  showAnnotations?: boolean;
  timeRange: TimeRange;
  title?: string;
}

export const TrendChart: React.FC<TrendChartProps> = ({
  data,
  comparisonPeriods,
  showAnnotations = true,
  timeRange,
  title,
}) => {
  const { t } = useLanguage();
  const chartTitle = title || t('history.trendAnalysis', 'Historical Trend Analysis');
  const getTrendIcon = () => {
    switch (data.direction) {
      case 'Increasing':
        return <TrendingUp className="w-5 h-5 text-green-600" />;
      case 'Decreasing':
        return <TrendingDown className="w-5 h-5 text-red-600" />;
      case 'Stable':
      default:
        return <Minus className="w-5 h-5 text-gray-600" />;
    }
  };

  const getTrendColor = () => {
    switch (data.direction) {
      case 'Increasing':
        return 'text-green-600';
      case 'Decreasing':
        return 'text-red-600';
      case 'Stable':
      default:
        return 'text-gray-600';
    }
  };

  const chartData = {
    labels: data.actualPoints.map(p => p.label),
    datasets: [
      {
        label: 'Actual Data',
        data: data.actualPoints.map(p => p.value),
        borderColor: 'rgb(59, 130, 246)',
        backgroundColor: 'rgba(59, 130, 246, 0.1)',
        borderWidth: 2,
        pointRadius: 4,
        pointHoverRadius: 6,
        tension: 0.4,
      },
      {
        label: 'Trend Line',
        data: data.trendLinePoints.map(p => p.value),
        borderColor: 'rgb(239, 68, 68)',
        backgroundColor: 'transparent',
        borderWidth: 2,
        borderDash: [5, 5],
        pointRadius: 0,
        pointHoverRadius: 0,
        tension: 0,
      },
      ...(comparisonPeriods?.map((period, index) => ({
        label: period.periodLabel,
        data: period.points.map(p => p.value),
        borderColor: `hsl(${index * 60}, 70%, 50%)`,
        backgroundColor: 'transparent',
        borderWidth: 1.5,
        borderDash: [3, 3],
        pointRadius: 2,
        pointHoverRadius: 4,
        tension: 0.4,
      })) || []),
    ],
  };

  const options: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: {
      mode: 'index',
      intersect: false,
    },
    plugins: {
      legend: {
        position: 'top',
        labels: {
          usePointStyle: true,
          padding: 15,
          font: {
            size: 12,
          },
        },
      },
      title: {
        display: true,
        text: chartTitle,
        font: {
          size: 16,
          weight: 'bold',
        },
        padding: {
          top: 10,
          bottom: 20,
        },
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        padding: 12,
        titleFont: {
          size: 14,
        },
        bodyFont: {
          size: 13,
        },
        callbacks: {
          label: (context: any) => {
            const label = context.dataset.label || '';
            const value = context.parsed.y;
            return `${label}: ${value.toFixed(2)}`;
          },
        },
      },
    },
    scales: {
      x: {
        display: true,
        title: {
          display: true,
          text: t('history.timePeriod', 'Time Period'),
          font: {
            size: 13,
            weight: 'bold',
          },
        },
        grid: {
          display: false,
        },
      },
      y: {
        display: true,
        title: {
          display: true,
          text: 'Value',
          font: {
            size: 13,
            weight: 'bold',
          },
        },
        grid: {
          color: 'rgba(0, 0, 0, 0.05)',
        },
      },
    },
  };

  return (
    <div className="w-full bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
      {/* Trend Summary */}
      {showAnnotations && (
        <div className="mb-4 p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              {getTrendIcon()}
              <div>
                <h3 className="text-sm font-semibold text-gray-900 dark:text-gray-100">
                  {t('history.trendDirection', 'Trend Direction')}: <span className={getTrendColor()}>{data.direction}</span>
                </h3>
                <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
                  {t('history.equation', 'Equation')}: {data.equation}
                </p>
              </div>
            </div>
            <div className="text-right">
              <p className="text-xs text-gray-500 dark:text-gray-400">{t('history.slope', 'Slope')}</p>
              <p className={`text-lg font-bold ${getTrendColor()}`}>
                {data.slope > 0 ? '+' : ''}{data.slope.toFixed(4)}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Chart */}
      <div style={{ height: '400px' }}>
        <Line data={chartData} options={options} />
      </div>

      {/* Comparison Period Stats */}
      {comparisonPeriods && comparisonPeriods.length > 0 && (
        <div className="mt-6 grid grid-cols-1 md:grid-cols-3 gap-4">
          {comparisonPeriods.map((period, index) => (
            <div
              key={index}
              className="p-4 bg-gray-50 dark:bg-gray-700 rounded-lg"
            >
              <h4 className="text-sm font-semibold text-gray-900 dark:text-gray-100 mb-2">
                {period.periodLabel}
              </h4>
              <div className="space-y-1">
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  Average: <span className="font-semibold">{period.averageValue.toFixed(2)}</span>
                </p>
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  Total: <span className="font-semibold">{period.totalValue.toFixed(2)}</span>
                </p>
                <p className="text-xs text-gray-600 dark:text-gray-400">
                  Data Points: <span className="font-semibold">{period.dataPointCount}</span>
                </p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
