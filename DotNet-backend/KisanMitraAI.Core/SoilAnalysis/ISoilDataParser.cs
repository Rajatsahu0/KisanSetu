using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.SoilAnalysis;

public interface ISoilDataParser
{
    Task<SoilHealthData> ParseSoilDataAsync(
        TextExtractionResult extraction,
        CancellationToken cancellationToken);

    Task<ValidationResult> ValidateSoilDataAsync(
        SoilHealthData data,
        CancellationToken cancellationToken);
}

public record ValidationResult(
    bool IsValid,
    List<ValidationError> Errors);

public record ValidationError(
    string FieldName,
    string ErrorMessage,
    string? ExtractedValue);
