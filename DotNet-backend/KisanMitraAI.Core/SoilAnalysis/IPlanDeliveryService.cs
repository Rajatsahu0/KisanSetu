using KisanMitraAI.Core.Models;

namespace KisanMitraAI.Core.SoilAnalysis;

public interface IPlanDeliveryService
{
    Task<string> FormatPlanAsTextAsync(
        RegenerativePlan plan,
        string language,
        CancellationToken cancellationToken);

    Task<Stream> GeneratePlanPdfAsync(
        RegenerativePlan plan,
        string language,
        CancellationToken cancellationToken);

    Task<Stream> SynthesizeVoiceSummaryAsync(
        RegenerativePlan plan,
        string language,
        string voiceId,
        CancellationToken cancellationToken);

    Task SendNotificationAsync(
        string farmerId,
        string planId,
        NotificationType notificationType,
        CancellationToken cancellationToken);
}

public enum NotificationType
{
    Voice,
    SMS,
    Both
}
