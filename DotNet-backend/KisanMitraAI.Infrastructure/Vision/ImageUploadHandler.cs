using KisanMitraAI.Core.QualityGrading;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace KisanMitraAI.Infrastructure.Vision;

public class ImageUploadHandler : IImageUploadHandler
{
    private readonly IS3StorageService _s3StorageService;
    private readonly ILogger<ImageUploadHandler> _logger;
    private const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MinImageWidth = 200;
    private const int MinImageHeight = 200;

    public ImageUploadHandler(
        IS3StorageService s3StorageService,
        ILogger<ImageUploadHandler> logger)
    {
        _s3StorageService = s3StorageService;
        _logger = logger;
    }

    public async Task<ImageUploadResult> UploadImageAsync(
        Stream imageStream,
        string farmerId,
        string produceType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate image size
            if (imageStream.Length > MaxImageSizeBytes)
            {
                return new ImageUploadResult(
                    string.Empty,
                    false,
                    $"Image size exceeds maximum allowed size of {MaxImageSizeBytes / (1024 * 1024)} MB");
            }

            // Validate image format and dimensions
            imageStream.Position = 0;
            using var image = await Image.LoadAsync(imageStream, cancellationToken);
            
            var format = image.Metadata.DecodedImageFormat;
            if (format is not JpegFormat and not PngFormat)
            {
                return new ImageUploadResult(
                    string.Empty,
                    false,
                    "Invalid image format. Only JPEG and PNG are supported");
            }

            if (image.Width < MinImageWidth || image.Height < MinImageHeight)
            {
                return new ImageUploadResult(
                    string.Empty,
                    false,
                    $"Image dimensions too small. Minimum size is {MinImageWidth}x{MinImageHeight} pixels");
            }

            // Upload to S3 with farmer-specific prefix
            imageStream.Position = 0;
            var fileName = $"quality-grading/{farmerId}/{produceType}/{Guid.NewGuid()}.jpg";
            var s3Key = await _s3StorageService.UploadAsync(
                imageStream,
                fileName,
                "image/jpeg",
                cancellationToken);

            _logger.LogInformation(
                "Image uploaded successfully for farmer {FarmerId}, produce {ProduceType}, S3 key: {S3Key}",
                farmerId, produceType, s3Key);

            return new ImageUploadResult(s3Key, true);
        }
        catch (UnknownImageFormatException)
        {
            return new ImageUploadResult(
                string.Empty,
                false,
                "Unable to read image format. Please ensure the file is a valid JPEG or PNG image");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image for farmer {FarmerId}", farmerId);
            throw;
        }
    }
}

public interface IS3StorageService
{
    Task<string> UploadAsync(
        Stream stream,
        string key,
        string contentType,
        CancellationToken cancellationToken = default);
    
    Task<Stream> DownloadAsync(
        string key,
        string farmerId = "",
        CancellationToken cancellationToken = default);
    
    Task<string> GeneratePresignedUrlAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken = default);
    
    Task DeleteAsync(
        string key,
        CancellationToken cancellationToken = default);
}
