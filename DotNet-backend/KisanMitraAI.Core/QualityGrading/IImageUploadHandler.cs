namespace KisanMitraAI.Core.QualityGrading;

public interface IImageUploadHandler
{
    Task<ImageUploadResult> UploadImageAsync(
        Stream imageStream,
        string farmerId,
        string produceType,
        CancellationToken cancellationToken = default);
}

public record ImageUploadResult(
    string ImageS3Key,
    bool IsValid,
    string? ValidationMessage = null);
