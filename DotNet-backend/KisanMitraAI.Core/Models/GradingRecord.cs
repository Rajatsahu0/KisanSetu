namespace KisanMitraAI.Core.Models;

/// <summary>
/// Domain model representing a grading record
/// </summary>
public record GradingRecord
{
    public string RecordId { get; init; }
    public string FarmerId { get; init; }
    public string ProduceType { get; init; }
    public QualityGrade Grade { get; init; }
    public decimal CertifiedPrice { get; init; }
    public string ImageS3Key { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public ImageAnalysisResult Analysis { get; init; }

    public GradingRecord(
        string recordId,
        string farmerId,
        string produceType,
        QualityGrade grade,
        decimal certifiedPrice,
        string imageS3Key,
        DateTimeOffset timestamp,
        ImageAnalysisResult analysis)
    {
        if (string.IsNullOrWhiteSpace(recordId))
        {
            throw new ArgumentException("Record ID cannot be null or empty", nameof(recordId));
        }

        if (string.IsNullOrWhiteSpace(farmerId))
        {
            throw new ArgumentException("Farmer ID cannot be null or empty", nameof(farmerId));
        }

        if (string.IsNullOrWhiteSpace(produceType))
        {
            throw new ArgumentException("Produce type cannot be null or empty", nameof(produceType));
        }

        if (certifiedPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(certifiedPrice), 
                "Certified price cannot be negative");
        }

        if (string.IsNullOrWhiteSpace(imageS3Key))
        {
            throw new ArgumentException("Image S3 key cannot be null or empty", nameof(imageS3Key));
        }

        RecordId = recordId;
        FarmerId = farmerId;
        ProduceType = produceType;
        Grade = grade;
        CertifiedPrice = certifiedPrice;
        ImageS3Key = imageS3Key;
        Timestamp = timestamp;
        Analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
    }
}
