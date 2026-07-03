using Google.Cloud.Storage.V1;
using Google.Apis.Auth.OAuth2;
using KisanMitraAI.Infrastructure.Storage.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Storage.GCS;

/// <summary>
/// Google Cloud Storage implementation replacing S3StorageService.
/// Implements the same IS3StorageService interface for backward compatibility.
/// </summary>
public class GcsStorageService : IS3StorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly ILogger<GcsStorageService> _logger;

    public GcsStorageService(
        StorageClient storageClient,
        IConfiguration config,
        ILogger<GcsStorageService> logger)
    {
        _storageClient = storageClient;
        _bucketName = config["GCP:Storage:BucketName"] ?? "kisansetu-storage";
        _logger = logger;
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string farmerId,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var objectName = GenerateObjectName(fileName, farmerId, contentType);

        await _storageClient.UploadObjectAsync(
            _bucketName, objectName, contentType, fileStream,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Uploaded {FileName} to GCS as {ObjectName} for farmer {FarmerId}",
            fileName, objectName, farmerId);

        return objectName;
    }

    public async Task<Stream> DownloadAsync(
        string key,
        string farmerId,
        CancellationToken cancellationToken = default)
    {
        var ms = new MemoryStream();
        await _storageClient.DownloadObjectAsync(
            _bucketName, key, ms, cancellationToken: cancellationToken);
        ms.Position = 0;

        _logger.LogInformation("Downloaded {Key} from GCS for farmer {FarmerId}", key, farmerId);
        return ms;
    }

    public Task<string> GeneratePresignedUrlAsync(
        string fileName,
        string farmerId,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        var objectName = GenerateObjectName(fileName, farmerId, "application/octet-stream");

        // Use V4 signed URL for upload
        var urlSigner = UrlSigner.FromCredential(GoogleCredential.GetApplicationDefault());
        var url = urlSigner.Sign(_bucketName, objectName, expiration, HttpMethod.Put);

        _logger.LogInformation("Generated signed URL for {ObjectName}", objectName);
        return Task.FromResult(url);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _storageClient.DeleteObjectAsync(_bucketName, key, cancellationToken: cancellationToken);
        _logger.LogInformation("Deleted {Key} from GCS", key);
    }

    public Task<string> InitiateMultipartUploadAsync(
        string fileName,
        string farmerId,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // GCS handles multipart/resumable uploads transparently via the SDK
        // Return the object name as the "upload ID"
        var objectName = GenerateObjectName(fileName, farmerId, contentType);
        return Task.FromResult(objectName);
    }

    public async Task<List<string>> ListObjectsAsync(
        string prefix,
        CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();
        var objects = _storageClient.ListObjectsAsync(_bucketName, prefix);

        await foreach (var obj in objects.WithCancellation(cancellationToken))
        {
            keys.Add(obj.Name);
        }

        _logger.LogInformation("Listed {Count} objects with prefix {Prefix}", keys.Count, prefix);
        return keys;
    }

    private string GenerateObjectName(string fileName, string farmerId, string contentType)
    {
        var prefix = DeterminePrefix(contentType);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyy/MM/dd");
        var uniqueId = Guid.NewGuid().ToString("N");
        var sanitized = SanitizeFileName(fileName);
        return $"{prefix}/{farmerId}/{timestamp}/{uniqueId}_{sanitized}";
    }

    private static string DeterminePrefix(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            var ct when ct.StartsWith("audio/") => "audio",
            var ct when ct.StartsWith("image/") => "images",
            var ct when ct.Contains("pdf") || ct.Contains("document") => "documents",
            _ => "misc"
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }
}
