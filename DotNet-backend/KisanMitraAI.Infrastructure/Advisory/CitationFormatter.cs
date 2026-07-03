using KisanMitraAI.Core.Advisory;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Advisory;

/// <summary>
/// Formats citations for display to farmers
/// </summary>
public class CitationFormatter
{
    private readonly ILogger<CitationFormatter> _logger;

    public CitationFormatter(ILogger<CitationFormatter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Formats citations for text display
    /// </summary>
    public string FormatCitationsForText(IEnumerable<Citation> citations)
    {
        if (!citations.Any())
        {
            return string.Empty;
        }

        var formatted = new System.Text.StringBuilder();
        formatted.AppendLine("\n\nSources:");
        
        int index = 1;
        foreach (var citation in citations.OrderByDescending(c => c.RelevanceScore))
        {
            formatted.AppendLine($"{index}. {ExtractDocumentName(citation.DocumentTitle)}");
            
            if (!string.IsNullOrWhiteSpace(citation.RelevantExcerpt))
            {
                var excerpt = TruncateExcerpt(citation.RelevantExcerpt, 200);
                formatted.AppendLine($"   \"{excerpt}\"");
            }
            
            formatted.AppendLine();
            index++;
        }

        return formatted.ToString();
    }

    /// <summary>
    /// Formats citations for voice output (simplified)
    /// </summary>
    public string FormatCitationsForVoice(IEnumerable<Citation> citations)
    {
        if (!citations.Any())
        {
            return string.Empty;
        }

        var citationCount = citations.Count();
        var topSource = citations.OrderByDescending(c => c.RelevanceScore).First();
        var sourceName = ExtractDocumentName(topSource.DocumentTitle);

        if (citationCount == 1)
        {
            return $"This information is from {sourceName}.";
        }
        else
        {
            return $"This information is from {citationCount} sources, including {sourceName}.";
        }
    }

    /// <summary>
    /// Formats citations as structured data for API responses
    /// </summary>
    public List<CitationMetadata> FormatCitationsForApi(IEnumerable<Citation> citations)
    {
        return citations.Select(c => new CitationMetadata(
            Title: ExtractDocumentName(c.DocumentTitle),
            Uri: c.DocumentUri,
            Excerpt: TruncateExcerpt(c.RelevantExcerpt, 500),
            RelevanceScore: c.RelevanceScore,
            Category: CategorizeDocument(c.DocumentTitle)
        )).ToList();
    }

    /// <summary>
    /// Calculates overall citation quality score
    /// </summary>
    public float CalculateCitationQuality(IEnumerable<Citation> citations)
    {
        if (!citations.Any())
        {
            return 0.0f;
        }

        // Quality based on:
        // 1. Number of citations (more is better, up to 5)
        // 2. Average relevance score
        // 3. Diversity of sources

        var citationList = citations.ToList();
        var count = citationList.Count;
        var avgRelevance = citationList.Average(c => c.RelevanceScore);
        var uniqueSources = citationList.Select(c => ExtractDocumentName(c.DocumentTitle)).Distinct().Count();

        var countScore = Math.Min(count / 5.0f, 1.0f);
        var relevanceScore = avgRelevance;
        var diversityScore = Math.Min(uniqueSources / 3.0f, 1.0f);

        return (countScore + relevanceScore + diversityScore) / 3.0f;
    }

    private string ExtractDocumentName(string documentTitle)
    {
        // Extract filename from S3 URI or path
        if (string.IsNullOrWhiteSpace(documentTitle))
        {
            return "Unknown Source";
        }

        // Handle S3 URIs
        if (documentTitle.StartsWith("s3://"))
        {
            var parts = documentTitle.Split('/');
            var filename = parts.Last();
            return System.IO.Path.GetFileNameWithoutExtension(filename);
        }

        // Handle file paths
        if (documentTitle.Contains('/') || documentTitle.Contains('\\'))
        {
            var filename = System.IO.Path.GetFileName(documentTitle);
            return System.IO.Path.GetFileNameWithoutExtension(filename);
        }

        return documentTitle;
    }

    private string TruncateExcerpt(string excerpt, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(excerpt))
        {
            return string.Empty;
        }

        if (excerpt.Length <= maxLength)
        {
            return excerpt;
        }

        // Truncate at word boundary
        var truncated = excerpt.Substring(0, maxLength);
        var lastSpace = truncated.LastIndexOf(' ');
        
        if (lastSpace > 0)
        {
            truncated = truncated.Substring(0, lastSpace);
        }

        return truncated + "...";
    }

    private string CategorizeDocument(string documentTitle)
    {
        var title = documentTitle.ToLowerInvariant();

        if (title.Contains("pest") || title.Contains("disease"))
            return "Pest & Disease Management";
        
        if (title.Contains("soil") || title.Contains("fertilizer"))
            return "Soil Health";
        
        if (title.Contains("crop") || title.Contains("cultivation"))
            return "Crop Management";
        
        if (title.Contains("irrigation") || title.Contains("water"))
            return "Water Management";
        
        if (title.Contains("seed") || title.Contains("variety"))
            return "Seed Selection";
        
        if (title.Contains("market") || title.Contains("price"))
            return "Market Information";
        
        if (title.Contains("organic") || title.Contains("regenerative"))
            return "Sustainable Farming";

        return "General Agriculture";
    }
}

public record CitationMetadata(
    string Title,
    string Uri,
    string Excerpt,
    float RelevanceScore,
    string Category);
