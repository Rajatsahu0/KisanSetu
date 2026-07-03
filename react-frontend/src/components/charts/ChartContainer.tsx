import React from 'react';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
  Filler,
  ChartOptions,
} from 'chart.js';
import { Line, Bar, Scatter } from 'react-chartjs-2';
import { ChartData, ChartSeriesType } from '@/types/chartData';

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  Title,
  Tooltip,
  Legend,
  Filler
);

interface ChartContainerProps {
  data: ChartData;
  responsive?: boolean;
  onDataPointClick?: (point: { label: string; value: number; timestamp: string }) => void;
  height?: number;
}

export const ChartContainer: React.FC<ChartContainerProps> = ({
  data,
  responsive = true,
  onDataPointClick,
  height = 400,
}) => {
  // Transform backend data to Chart.js format
  const chartData = {
    labels: data.series[0]?.points.map(p => p.label) || [],
    datasets: data.series.map(series => ({
      label: series.name,
      data: series.points.map(p => p.value),
      borderColor: series.color,
      backgroundColor: series.type === ChartSeriesType.Area 
        ? `${series.color}33` // Add transparency for area charts
        : series.color,
      fill: series.type === ChartSeriesType.Area,
      tension: 0.4,
      pointRadius: 4,
      pointHoverRadius: 6,
    })),
  };

  const options: ChartOptions<any> = {
    responsive,
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
        text: data.title,
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
          text: data.xAxisLabel,
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
          text: data.yAxisLabel,
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
    onClick: (event: any, elements: any[]) => {
      if (elements.length > 0 && onDataPointClick) {
        const element = elements[0];
        const datasetIndex = element.datasetIndex;
        const index = element.index;
        const series = data.series[datasetIndex];
        const point = series.points[index];
        onDataPointClick(point);
      }
    },
  };

  // Determine chart type based on first series
  const chartType = data.series[0]?.type || ChartSeriesType.Line;

  const renderChart = () => {
    switch (chartType) {
      case ChartSeriesType.Bar:
        return <Bar data={chartData} options={options} />;
      case ChartSeriesType.Scatter:
        return <Scatter data={chartData} options={options} />;
      case ChartSeriesType.Line:
      case ChartSeriesType.Area:
      default:
        return <Line data={chartData} options={options} />;
    }
  };

  return (
    <div 
      className="w-full bg-white dark:bg-gray-800 rounded-lg shadow-md p-4"
      style={{ height: `${height}px` }}
    >
      {data.series.length === 0 ? (
        <div className="flex items-center justify-center h-full">
          <p className="text-gray-500 dark:text-gray-400">No data available</p>
        </div>
      ) : (
        renderChart()
      )}
    </div>
  );
};
