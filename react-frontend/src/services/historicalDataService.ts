import { 
  ChartData, 
  ComparisonChartData, 
  TrendLineData, 
  TimeRange, 
  CustomTimeRange 
} from '@/types/chartData';
import { apiClient } from './apiClient';

interface HistoricalDataRequest {
  timeRange: TimeRange;
  customRange?: CustomTimeRange;
  dataType: 'prices' | 'soilHealth' | 'qualityGrades' | 'all';
}

interface HistoricalDataResponse {
  chartData: ChartData;
  comparisonData?: ComparisonChartData;
  trendData?: TrendLineData;
  insights: string[];
  hasInsufficientData: boolean;
  dataPointCount: number;
}

/**
 * Fetch historical data from the backend
 */
export const fetchHistoricalData = async (
  request: HistoricalDataRequest
): Promise<HistoricalDataResponse> => {
  const params = new URLSearchParams({
    timeRange: request.timeRange,
    dataType: request.dataType,
  });

  if (request.customRange) {
    params.append('startDate', request.customRange.startDate.toISOString());
    params.append('endDate', request.customRange.endDate.toISOString());
  }

  return apiClient.get<HistoricalDataResponse>(
    `/api/v1/historical-data?${params.toString()}`
  );
};

/**
 * Fetch AI insights for historical data
 */
export const fetchInsights = async (
  dataType: string,
  timeRange: TimeRange
): Promise<string[]> => {
  const data = await apiClient.get<{ insights: string[] }>(
    `/api/v1/historical-data/insights?dataType=${dataType}&timeRange=${timeRange}`
  );
  return data.insights || [];
};

/**
 * Fetch comparison data for multiple periods
 */
export const fetchComparisonData = async (
  dataType: string,
  periods: string[]
): Promise<ComparisonChartData> => {
  return apiClient.post<ComparisonChartData>(
    '/api/v1/historical-data/comparison',
    { dataType, periods }
  );
};

/**
 * Generate mock data for development/testing
 */
export const generateMockHistoricalData = (
  dataType: string,
  timeRange: TimeRange
): HistoricalDataResponse => {
  const dataPointCount = timeRange === '7days' ? 7 : timeRange === '30days' ? 30 : 90;
  
  const generatePoints = (count: number) => {
    const points = [];
    const now = new Date();
    
    for (let i = count - 1; i >= 0; i--) {
      const date = new Date(now);
      date.setDate(date.getDate() - i);
      
      points.push({
        label: date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
        value: Math.random() * 100 + 50,
        timestamp: date.toISOString(),
      });
    }
    
    return points;
  };

  const points = generatePoints(dataPointCount);
  
  return {
    chartData: {
      title: `${dataType} Historical Data`,
      xAxisLabel: 'Date',
      yAxisLabel: 'Value',
      series: [
        {
          name: dataType,
          points,
          color: '#3b82f6',
          type: 'Line' as any,
        },
      ],
      annotations: [],
    },
    trendData: {
      actualPoints: points,
      trendLinePoints: points.map((p, i) => ({
        ...p,
        value: 60 + i * 0.5,
      })),
      direction: 'Increasing' as any,
      slope: 0.5,
      equation: 'y = 0.5x + 60',
    },
    insights: [
      `${dataType} shows an increasing trend over the selected period.`,
      'Average value has increased by 15% compared to the previous period.',
      'Consider maintaining current practices to sustain this positive trend.',
    ],
    hasInsufficientData: dataPointCount < 5,
    dataPointCount,
  };
};
