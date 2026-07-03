namespace KisanMitraAI.Core.Advisory;

public interface IExpertEscalationHandler
{
    Task<string> EscalateQuestionAsync(
        AdvisoryQuestion question,
        float confidenceScore,
        CancellationToken cancellationToken);
    
    Task<EscalationStatus> GetEscalationStatusAsync(
        string escalationId,
        CancellationToken cancellationToken);
}

public record EscalationStatus(
    string EscalationId,
    string Status,
    DateTimeOffset EscalatedAt,
    DateTimeOffset? RespondedAt,
    string? ExpertResponse);
