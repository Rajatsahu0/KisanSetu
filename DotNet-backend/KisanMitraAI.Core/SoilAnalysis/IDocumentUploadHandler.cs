namespace KisanMitraAI.Core.SoilAnalysis;

public interface IDocumentUploadHandler
{
    Task<DocumentUploadResult> UploadDocumentAsync(
        Stream documentStream,
        string farmerId,
        string documentType,
        CancellationToken cancellationToken);
}

public record DocumentUploadResult(
    string DocumentS3Key,
    string FarmerId,
    string DocumentType,
    long FileSizeBytes,
    DateTimeOffset UploadedAt);
