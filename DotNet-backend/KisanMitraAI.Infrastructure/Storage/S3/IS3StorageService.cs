namespace KisanMitraAI.Infrastructure.Storage.S3;

/// <summary>
/// Service for Amazon S3 object storage operations
/// </summary>
public interface IS3StorageService
{
    /// <summary>
    /// Uploads a file to S3
    /// </summary>
    Task<string> UploadAsync(
        Stream fileStream, 
        string fileName, 
        string farmerId,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from S3
    /// </summary>
    Task<Stream> DownloadAsync(
        string s3Key,
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a pre-signed URL for client-side upload
    /// </summary>
    Task<string> GeneratePresignedUrlAsync(
        string fileName,
        string farmerId,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from S3
    /// </summary>
    Task DeleteAsync(
        string s3Key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a multipart upload for large files
    /// </summary>
    Task<string> InitiateMultipartUploadAsync(
        string fileName,
        string farmerId,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all objects in S3 with a given prefix
    /// </summary>
    Task<List<string>> ListObjectsAsync(
        string prefix,
        CancellationToken cancellationToken = default);
}
