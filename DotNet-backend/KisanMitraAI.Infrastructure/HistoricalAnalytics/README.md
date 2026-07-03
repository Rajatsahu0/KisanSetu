# Historical Analytics Module

## Overview

The Historical Analytics module provides comprehensive data tracking, trend analysis, visualization, and regional benchmarking capabilities for the Kisan Mitra AI platform. It enables farmers to understand their historical performance, identify patterns, and compare their metrics with regional benchmarks.

## Components

### 1. Data Aggregation Service

**Purpose**: Retrieves and aggregates historical data from Timestream repositories.

**Key Features**:
- Retrieves historical prices, soil data, and grading records
- Calculates trend data with direction, min/max/average values
- Detects anomalies in time-series data
- Supports multi-period comparisons

**Usage**:
```csharp
var aggregationService = serviceProvider.GetRequiredService<IDataAggregationService>();

// Get historical prices
var period = TimePeriod.LastMonths(6);
var prices = await aggregationService.GetHistoricalPricesAsync("wheat", "Delhi", period);

// Calculate price trend
var trend = await aggregationService.CalculatePriceTrendAsync("wheat", "Delhi", period);
```

### 2. Visualization Data Formatter

**Purpose**: Formats historical data for charting and visualization in frontend applications.

**Key Features**:
- Formats time-series data for charts (JSON format)
- Calculates moving averages for trend smoothing
- Prepares comparison data for side-by-side display
- Identifies significant changes (>20% threshold)
- Generates trend lines using linear regression

**Usage**:
```csharp
var formatter = serviceProvider.GetRequiredService<IVisualizationDataFormatter>();

// Format for charting
var chartData = await formatter.FormatTimeSeriesDataAsync(
    trendData,
    "Wheat Prices - Last 6 Months",
    "Date",
    "Price (₹/quintal)");

// Calculate moving average
var movingAvg = await formatter.CalculateMovingAverageAsync(trendData, windowSize: 7, "7-Day Moving Average");
```

### 3. Insights Generator

**Purpose**: Uses Amazon Bedrock (Claude 3.5 Sonnet) to generate AI-powered insights from historical patterns.

**Key Features**:
- Detects patterns (seasonal, cyclical, trending, volatile, stable)
- Generates natural language insights
- Analyzes trend strength and direction
- Suggests actionable recommendations
- Requires 2+ years of data for pattern detection

**Usage**:
```csharp
var insightsGenerator = serviceProvider.GetRequiredService<IInsightsGenerator>();

// Generate insights
var insights = await insightsGenerator.GenerateInsightsAsync(
    trendData,
    farmerId: "farmer123",
    dataType: "soil_health");

// Detect patterns
var patterns = await insightsGenerator.DetectPatternsAsync(trendData, "mandi_prices");

// Get action suggestions
var actions = await insightsGenerator.SuggestActionsAsync(trendData, farmerId, "quality_grades");
```

### 4. Regional Benchmark Aggregator

**Purpose**: Aggregates anonymized data across farmers to provide regional benchmarks while protecting individual privacy.

**Key Features**:
- Ensures minimum 10 farmers for privacy compliance
- Calculates regional averages, medians, percentiles
- Provides percentile rankings for individual farmers
- Supports soil health, quality grades, and yield benchmarks

**Usage**:
```csharp
var benchmarkAggregator = serviceProvider.GetRequiredService<IRegionalBenchmarkAggregator>();

// Get regional soil health benchmark
var benchmark = await benchmarkAggregator.AggregateRegionalSoilHealthAsync("Maharashtra", period);

// Calculate farmer's percentile ranking
var ranking = await benchmarkAggregator.CalculatePercentileRankingAsync(
    farmerId: "farmer123",
    region: "Maharashtra",
    metricType: "soil_health",
    period);

// Validate privacy compliance
var isValid = await benchmarkAggregator.ValidateMinimumFarmerCountAsync("Maharashtra", minimumCount: 10);
```

## Data Models

### Time Period
- `LastDays(int days)` - Last N days
- `LastMonths(int months)` - Last N months
- `LastYears(int years)` - Last N years
- `Season(int year, string name, int startMonth, int endMonth)` - Specific season
- `Custom(DateTimeOffset start, DateTimeOffset end, string label)` - Custom range

### Trend Data
- `DataPoints` - Time-series data points
- `Direction` - Increasing, Decreasing, Stable, Volatile
- `MinValue`, `MaxValue`, `AverageValue` - Statistical measures
- `Anomalies` - Detected outliers with reasons

### Chart Data
- `ChartSeries` - Line, Bar, Area, Scatter
- `ChartAnnotations` - Anomaly markers, peaks, troughs
- `ComparisonChartData` - Multi-period comparisons

### Regional Benchmark
- `FarmerCount` - Number of farmers in aggregation
- `AverageValue`, `MedianValue` - Central tendencies
- `Percentiles` - 25th, 50th, 75th, 90th, 95th
- `IsPrivacyCompliant` - Minimum farmer count met

## Data Retention

The module respects the following retention policies:
- **Soil Data**: 10 years (Timestream)
- **Market Prices**: 5 years (Timestream)
- **Quality Grades**: 2 years (Timestream)

## Privacy Protection

The Regional Benchmark Aggregator implements strict privacy protection:
1. **Minimum Farmer Count**: Requires at least 10 farmers for any regional aggregation
2. **Anonymization**: Individual farmer data is never exposed in regional benchmarks
3. **Validation**: `ValidateMinimumFarmerCountAsync` checks privacy compliance before aggregation
4. **Compliance Flag**: `IsPrivacyCompliant` indicates whether minimum count was met

## Integration with Other Modules

### Timestream Repositories
- `IMandiPriceRepository` - Historical price data
- `ISoilDataRepository` - Historical soil health data
- `IGradingHistoryRepository` - Historical quality grading data

### Amazon Bedrock
- Uses Claude 3.5 Sonnet for insights generation
- Model ID: `anthropic.claude-3-5-sonnet-20241022-v2:0`
- Max tokens: 2000 per request

## Requirements Validation

This module validates the following requirements:

**Requirement 7.1**: Data points are stored with metadata (timestamp, farmer ID)
**Requirement 7.2**: Historical data is retrievable for requested time periods
**Requirement 7.4**: Multi-period comparisons are supported
**Requirement 7.5**: Insights are generated from patterns (2+ years of data)
**Requirement 7.6**: Regional aggregation preserves privacy (minimum 10 farmers)

## Testing

Property-based tests for this module are defined in task 12.5 (optional):
- Property 35: Data points are stored with metadata
- Property 36: Historical data is retrievable
- Property 37: Multi-period comparisons are supported
- Property 38: Insights are generated from patterns
- Property 39: Regional aggregation preserves privacy

## Future Enhancements

1. **Advanced Pattern Detection**: Implement more sophisticated pattern recognition algorithms
2. **Predictive Analytics**: Add forecasting capabilities based on historical trends
3. **Custom Metrics**: Allow farmers to define custom metrics for tracking
4. **Export Capabilities**: Enable export of historical data and insights to PDF/Excel
5. **Real-time Alerts**: Notify farmers when significant changes are detected
