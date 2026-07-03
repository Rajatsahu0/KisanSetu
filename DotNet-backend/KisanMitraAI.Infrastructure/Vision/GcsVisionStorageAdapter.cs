namespace KisanMitraAI.Infrastructure.Vision;

/// <summary>
/// Adapter that bridges Storage.S3.IS3StorageService to Vision.IS3StorageService.
/// Both interfaces are backed by GCS — this just handles the parameter difference.
/// </summary>
public class GcsVisionStorageAdapter : IS3StorageService
{
    private readonly Storage.S3.IS3StorageService _inner;

    public GcsVisionStorageAdapter(Storage.S3.IS3StorageService inner)
    {
        _inner = inner;
    }

    public async Task<string> UploadAsync(Stream stream, string key, string contentType, CancellationToken cancellationToken = default)
    {
        return await _inner.UploadAsync(stream, key, "", contentType, cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string key, string farmerId = "", CancellationToken cancellationToken = default)
    {
        return await _inner.DownloadAsync(key, farmerId, cancellationToken);
    }

    public async Task<string> GeneratePresignedUrlAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        return await _inner.GeneratePresignedUrlAsync(key, "", expiration, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _inner.DeleteAsync(key, cancellationToken);
    }
}
