// Chart data types matching backend models

export enum ChartSeriesType {
  Line = 'Line',
  Bar = 'Bar',
  Area = 'Area',
  Scatter = 'Scatter'
}

export enum AnnotationType {
  Anomaly = 'Anomaly',
  Peak = 'Peak',
  Trough = 'Trough',
  Milestone = 'Milestone'
}

export enum TrendDirection {
  Increasing = 'Increasing',
  Decreasing = 'Decreasing',
  Stable = 'Stable'
}

export interface ChartPoint {
  label: string;
  value: number;
  timestamp: string;
}

export interface ChartSeries {
  name: string;
  points: ChartPoint[];
  color: string;
  type: ChartSeriesType;
}

export interface ChartAnnotation {
  timestamp: string;
  text: string;
  type: AnnotationType;
}

export interface ChartData {
  title: string;
  xAxisLabel: string;
  yAxisLabel: string;
  series: ChartSeries[];
  annotations: ChartAnnotation[];
}

export interface PeriodChartData {
  periodLabel: string;
  averageValue: number;
  totalValue: number;
  dataPointCount: number;
  points: ChartPoint[];
}

export interface ComparisonChartData {
  title: string;
  periods: PeriodChartData[];
  insights: string[];
}

export interface SignificantChange {
  timestamp: string;
  oldValue: number;
  newValue: number;
  changePercent: number;
  description: string;
}

export interface TrendLineData {
  actualPoints: ChartPoint[];
  trendLinePoints: ChartPoint[];
  direction: TrendDirection;
  slope: number;
  equation: string;
}

export type TimeRange = '7days' | '30days' | 'season' | 'year' | 'custom';

export interface CustomTimeRange {
  startDate: Date;
  endDate: Date;
}

export interface HistoricalDataFilter {
  timeRange: TimeRange;
  customRange?: CustomTimeRange;
  dataType: 'prices' | 'soilHealth' | 'qualityGrades' | 'all';
}

export interface ExportOptions {
  format: 'pdf' | 'csv';
  includeCharts: boolean;
  includeInsights: boolean;
}
