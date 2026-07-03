namespace KisanMitraAI.Core.SoilAnalysis;

public interface ITextExtractor
{
    Task<TextExtractionResult> ExtractTextAsync(
        string documentS3Key,
        CancellationToken cancellationToken);
}

public record TextExtractionResult(
    string DocumentS3Key,
    Dictionary<string, string> ExtractedFields,
    Dictionary<string, TableData> ExtractedTables,
    float ConfidenceScore,
    DateTimeOffset ExtractedAt);

public record TableData(
    List<List<string>> Rows,
    List<string> Headers);
