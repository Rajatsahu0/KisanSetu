using KisanMitraAI.Core.HistoricalAnalytics;
using KisanMitraAI.Core.Models;
using KisanMitraAI.Core.AI;
using KisanMitraAI.Infrastructure.AI;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.HistoricalAnalytics;

/// <summary>
/// Implementation of insights generator using Vertex AI Gemini.
/// OPTIMIZED: Uses consolidated single-prompt analysis (4 calls → 1 call per data type).
/// </summary>
public class InsightsGenerator : IInsightsGenerator
{
    private readonly GeminiService _geminiService;
    private readonly GeminiModelConfig _modelConfig;
    private readonly ILogger<InsightsGenerator> _logger;

    public InsightsGenerator(
        GeminiService geminiService,
        GeminiModelConfig modelConfig,
        ILogger<InsightsGenerator> logger)
    {
        _geminiService = geminiService ?? throw new ArgumentNullException(nameof(geminiService));
        _modelConfig = modelConfig ?? throw new ArgumentNullException(nameof(modelConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<DataPattern>> DetectPatternsAsync<T>(
        TrendData<T> trendData,
        string dataType,
        CancellationToken cancellationToken = default)
    {
        var result = await GenerateConsolidatedAnalysisAsync(trendData, "unknown", dataType, cancellationToken);
        return result.Patterns;
    }

    public async Task<IEnumerable<Insight>> GenerateInsightsAsync<T>(
        TrendData<T> trendData,
        string farmerId,
        string dataType,
        CancellationToken cancellationToken = default)
    {
        var result = await GenerateConsolidatedAnalysisAsync(trendData, farmerId, dataType, cancellationToken);
        return result.Insights;
    }

    public async Task<TrendAnalysis> AnalyzeTrendAsync<T>(
        TrendData<T> trendData,
        CancellationToken cancellationToken = default)
    {
        var result = await GenerateConsolidatedAnalysisAsync(trendData, "unknown", "data", cancellationToken);
        return result.TrendAnalysis;
    }

    public async Task<IEnumerable<ActionSuggestion>> SuggestActionsAsync<T>(
        TrendData<T> trendData,
        string farmerId,
        string dataType,
        CancellationToken cancellationToken = default)
    {
        var result = await GenerateConsolidatedAnalysisAsync(trendData, farmerId, dataType, cancellationToken);
        return result.Suggestions;
    }

    public async Task<IEnumerable<Insight>> GenerateComparisonInsightsAsync<T>(
        PeriodComparison<T> comparison,
        string farmerId,
        string dataType,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildComparisonInsightsPrompt(comparison, farmerId, dataType);
        var response = await InvokeBedrockAsync(prompt, cancellationToken);
        return ParseInsightsResponse(response);
    }

    /// <summary>
    /// ONE Bedrock call replaces 4 separate calls (patterns + insights + trend + actions).
    /// Latency: 3-6s instead of 8-20s per data type.
    /// </summary>
    private async Task<ConsolidatedAnalysisResult> GenerateConsolidatedAnalysisAsync<T>(
        TrendData<T> trendData,
        string farmerId,
        string dataType,
        CancellationToken cancellationToken)
    {
        var dataPoints = trendData.DataPoints.ToList();
        var hasEnoughForPatterns = dataPoints.Count >= 24;

        var sb = new StringBuilder();
        sb.AppendLine($"Analyze the following {dataType} data for farmer {farmerId} and provide a COMPLETE analysis.");
        sb.AppendLine();
        sb.AppendLine($"Trend Direction: {trendData.Direction}");
        sb.AppendLine($"Min: {trendData.MinValue}, Max: {trendData.MaxValue}, Avg: {trendData.AverageValue}");
        sb.AppendLine($"Data Points: {dataPoints.Count}");
        sb.AppendLine();
        sb.AppendLine("Sample Data:");
        foreach (var point in dataPoints.Take(50))
            sb.AppendLine($"- {point.Timestamp:yyyy-MM-dd}: {point.Value}");
        if (trendData.Anomalies.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Anomalies:");
            foreach (var a in trendData.Anomalies.Take(5))
                sb.AppendLine($"- {a.Point.Timestamp:yyyy-MM-dd}: {a.Reason}");
        }
        sb.AppendLine();
        sb.AppendLine("Provide ALL of the following in your response (use these exact section headers):");
        sb.AppendLine();
        if (hasEnoughForPatterns)
        {
            sb.AppendLine("## PATTERNS");
            sb.AppendLine("Identify seasonal, cyclical, trending, volatile, or stable patterns.");
        }
        sb.AppendLine("## INSIGHTS");
        sb.AppendLine("Provide 2-4 actionable insights with severity (Info/Warning/Critical/Positive).");
        sb.AppendLine("## TREND ANALYSIS");
        sb.AppendLine("Describe trend strength (Weak/Moderate/Strong), contributing factors, and predictions.");
        sb.AppendLine("## ACTION SUGGESTIONS");
        sb.AppendLine("Provide 2-4 specific actions with priority (Low/Medium/High/Urgent).");

        var response = await InvokeBedrockAsync(sb.ToString(), cancellationToken);

        var patterns = new List<DataPattern>();
        var insights = new List<Insight>();
        var suggestions = new List<ActionSuggestion>();

        var sections = response.Split(new[] { "## " }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var section in sections)
        {
            var upper = section.TrimStart().ToUpperInvariant();
            if (upper.StartsWith("PATTERNS"))
            {
                var start = hasEnoughForPatterns ? dataPoints.First().Timestamp : DateTimeOffset.UtcNow;
                var end = hasEnoughForPatterns ? dataPoints.Last().Timestamp : DateTimeOffset.UtcNow;
                patterns.AddRange(ParsePatternResponse(section, start, end));
            }
            else if (upper.StartsWith("INSIGHTS"))
                insights.AddRange(ParseInsightsResponse(section));
            else if (upper.StartsWith("ACTION"))
                suggestions.AddRange(ParseActionSuggestionsResponse(section));
        }

        if (!insights.Any())
            insights.AddRange(ParseInsightsResponse(response));
        if (!suggestions.Any())
            suggestions.AddRange(ParseActionSuggestionsResponse(response));

        var trendAnalysis = ParseTrendAnalysisResponse(response, trendData.Direction);

        return new ConsolidatedAnalysisResult(patterns, insights, trendAnalysis, suggestions);
    }

    private record ConsolidatedAnalysisResult(
        IEnumerable<DataPattern> Patterns,
        IEnumerable<Insight> Insights,
        TrendAnalysis TrendAnalysis,
        IEnumerable<ActionSuggestion> Suggestions);

    private string BuildComparisonInsightsPrompt<T>(PeriodComparison<T> comparison, string farmerId, string dataType)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Generate insights from comparing {dataType} data across multiple periods for farmer {farmerId}:");
        sb.AppendLine();
        foreach (var period in comparison.Periods)
        {
            sb.AppendLine($"Period: {period.Period.Label}");
            sb.AppendLine($"  Average: {period.AverageValue}, Total: {period.TotalValue}, Data Points: {period.DataPointCount}");
        }
        sb.AppendLine();
        sb.AppendLine("Existing Insights:");
        foreach (var insight in comparison.Insights)
            sb.AppendLine($"- {insight.Description}");
        sb.AppendLine();
        sb.AppendLine("Generate additional insights about patterns, improvements, declines, and recommended actions.");
        return sb.ToString();
    }

    private async Task<string> InvokeBedrockAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            return await _geminiService.GenerateContentAsync(
                _modelConfig.InsightsModel, prompt, 0.7f, 2000, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking Bedrock for insights generation");
            throw;
        }
    }

    private IEnumerable<DataPattern> ParsePatternResponse(string response, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var patterns = new List<DataPattern>();
        if (response.Contains("seasonal", StringComparison.OrdinalIgnoreCase))
            patterns.Add(new DataPattern(PatternType.Seasonal, "Seasonal pattern detected in the data", startDate, endDate, 0.8f, new[] { "Recurring patterns at specific times of year" }));
        if (response.Contains("trend", StringComparison.OrdinalIgnoreCase))
            patterns.Add(new DataPattern(PatternType.Trending, "Consistent trend detected", startDate, endDate, 0.85f, new[] { "Consistent directional movement over time" }));
        return patterns;
    }

    private IEnumerable<Insight> ParseInsightsResponse(string response)
    {
        var insights = new List<Insight>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var currentInsight = new StringBuilder();
        foreach (var line in lines)
        {
            if (line.StartsWith("##") || line.StartsWith("**"))
            {
                if (currentInsight.Length > 0)
                {
                    insights.Add(CreateInsightFromText(currentInsight.ToString()));
                    currentInsight.Clear();
                }
            }
            currentInsight.AppendLine(line);
        }
        if (currentInsight.Length > 0)
            insights.Add(CreateInsightFromText(currentInsight.ToString()));

        return insights.Any() ? insights : new[]
        {
            new Insight("Data Analysis Complete", response, InsightSeverity.Info, 0.9f, Array.Empty<string>(), Array.Empty<ActionSuggestion>())
        };
    }

    private Insight CreateInsightFromText(string text)
    {
        var severity = text.Contains("warning", StringComparison.OrdinalIgnoreCase) ? InsightSeverity.Warning :
                      text.Contains("critical", StringComparison.OrdinalIgnoreCase) ? InsightSeverity.Critical :
                      text.Contains("positive", StringComparison.OrdinalIgnoreCase) ? InsightSeverity.Positive :
                      InsightSeverity.Info;
        return new Insight("Insight", text.Trim(), severity, 0.85f, Array.Empty<string>(), Array.Empty<ActionSuggestion>());
    }

    private TrendAnalysis ParseTrendAnalysisResponse(string response, TrendDirection direction)
    {
        var strength = response.Contains("strong", StringComparison.OrdinalIgnoreCase) ? TrendStrength.Strong :
                      response.Contains("moderate", StringComparison.OrdinalIgnoreCase) ? TrendStrength.Moderate :
                      TrendStrength.Weak;
        return new TrendAnalysis(direction, strength, response, Array.Empty<TrendFactor>(), Array.Empty<string>());
    }

    private IEnumerable<ActionSuggestion> ParseActionSuggestionsResponse(string response)
    {
        var suggestions = new List<ActionSuggestion>();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("-") || line.TrimStart().StartsWith("*") ||
                line.TrimStart().StartsWith("1.") || line.TrimStart().StartsWith("2."))
            {
                var priority = line.Contains("urgent", StringComparison.OrdinalIgnoreCase) ? ActionPriority.Urgent :
                              line.Contains("high", StringComparison.OrdinalIgnoreCase) ? ActionPriority.High :
                              line.Contains("medium", StringComparison.OrdinalIgnoreCase) ? ActionPriority.Medium :
                              ActionPriority.Low;
                suggestions.Add(new ActionSuggestion(
                    line.Trim().TrimStart('-', '*', '1', '2', '3', '4', '5', '.', ' '),
                    "Based on historical data analysis", priority, Array.Empty<string>()));
            }
        }
        return suggestions.Any() ? suggestions : new[]
        {
            new ActionSuggestion("Continue monitoring data trends", "Maintain current practices while tracking performance", ActionPriority.Low, new[] { "Better understanding of long-term patterns" })
        };
    }
}
