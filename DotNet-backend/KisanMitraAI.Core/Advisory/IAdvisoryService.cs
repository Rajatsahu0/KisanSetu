namespace KisanMitraAI.Core.Advisory;

public interface IAdvisoryService
{
    Task<AdvisoryResponse> AskQuestionAsync(
        AdvisoryQuestion question,
        CancellationToken cancellationToken);
}

public record AdvisoryQuestion(
    string QuestionText,
    string FarmerId,
    string Context,
    IEnumerable<string> ConversationHistory);

public record AdvisoryResponse(
    string AnswerText,
    IEnumerable<Citation> Sources,
    bool RequiresExpertEscalation,
    DateTimeOffset RespondedAt);
