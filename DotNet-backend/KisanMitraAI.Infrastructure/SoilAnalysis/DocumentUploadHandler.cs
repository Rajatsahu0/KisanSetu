using KisanMitraAI.Core.SoilAnalysis;
using KisanMitraAI.Infrastructure.Storage.S3;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.SoilAnalysis;

public class DocumentUploadHandler : IDocumentUploadHandler
{
    private readonly IS3StorageService _storageService;
    private readonly ILogger<DocumentUploadHandler> _logger;
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private static readonly HashSet<string> AllowedContentTypes = new()
    {
        "image/jpeg", "image/png", "application/pdf"
    };

    public DocumentUploadHandler(
        IS3StorageService storageService,
        ILogger<DocumentUploadHandler> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentUploadResult> UploadDocumentAsync(
        Stream documentStream, string farmerId, string documentType,
        CancellationToken cancellationToken)
    {
        if (documentStream == null) throw new ArgumentNullException(nameof(documentStream));
        if (string.IsNullOrWhiteSpace(farmerId)) throw new ArgumentException("Farmer ID required", nameof(farmerId));
        if (string.IsNullOrWhiteSpace(documentType)) throw new ArgumentException("Document type required", nameof(documentType));

        if (documentStream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException($"Document exceeds maximum {MaxFileSizeBytes} bytes");

        var contentType = await DetectContentTypeAsync(documentStream, cancellationToken);
        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException($"Format '{contentType}' not supported. Use JPEG, PNG, or PDF.");

        var timestamp = DateTimeOffset.UtcNow;
        var ext = contentType switch { "image/jpeg" => ".jpg", "image/png" => ".png", _ => ".pdf" };
        var fileName = $"{documentType}/{timestamp:yyyyMMdd-HHmmss}-{Guid.NewGuid()}{ext}";

        try
        {
            var gcsKey = await _storageService.UploadAsync(
                documentStream, fileName, farmerId, contentType, cancellationToken);

            _logger.LogInformation("Document uploaded. FarmerId: {FarmerId}, Key: {Key}", farmerId, gcsKey);

            return new DocumentUploadResult(gcsKey, farmerId, documentType, documentStream.Length, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document. FarmerId: {FarmerId}", farmerId);
            throw new InvalidOperationException("Failed to upload document to storage", ex);
        }
    }

    private static async Task<string> DetectContentTypeAsync(Stream stream, CancellationToken ct)
    {
        var buffer = new byte[8];
        var pos = stream.Position;
        await stream.ReadAsync(buffer, 0, buffer.Length, ct);
        stream.Position = pos;

        if (buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF) return "image/jpeg";
        if (buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47) return "image/png";
        if (buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46) return "application/pdf";
        return "application/octet-stream";
    }
}
