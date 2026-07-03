using KisanMitraAI.Core.Advisory;
using KisanMitraAI.Infrastructure.Repositories.DynamoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Advisory;

public class AdvisoryService : IAdvisoryService
{
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ISessionRepository _sessionRepository;
    private readonly IExpertEscalationHandler _expertEscalationHandler;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdvisoryService> _logger;
    private readonly float _confidenceThreshold;
    private readonly int _maxResults;

    public AdvisoryService(
        IKnowledgeBaseService knowledgeBaseService,
        ISessionRepository sessionRepository,
        IExpertEscalationHandler expertEscalationHandler,
        IConfiguration configuration,
        ILogger<AdvisoryService> logger)
    {
        _knowledgeBaseService = knowledgeBaseService;
        _sessionRepository = sessionRepository;
        _expertEscalationHandler = expertEscalationHandler;
        _configuration = configuration;
        _logger = logger;
        
        _confidenceThreshold = float.Parse(
            configuration["AWS:BedrockKnowledgeBase:ConfidenceThreshold"] ?? "0.7");
        _maxResults = int.Parse(
            configuration["AWS:BedrockKnowledgeBase:MaxResults"] ?? "5");
    }

    public async Task<AdvisoryResponse> AskQuestionAsync(
        AdvisoryQuestion question,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing advisory question for farmer {FarmerId}: {Question}",
                question.FarmerId,
                question.QuestionText);

            // OPTIMIZED: Single session read, reused for both history retrieval and save.
            // Previously read session twice (once in GetConversationHistory, once in SaveExchange).
            var session = await _sessionRepository.GetSessionAsync(
                question.FarmerId, cancellationToken);

            var conversationHistory = ExtractConversationHistory(session);

            var enrichedContext = BuildEnrichedContext(
                question.Context, conversationHistory);

            var kbResponse = await _knowledgeBaseService.QueryKnowledgeBaseAsync(
                query: question.QuestionText,
                context: enrichedContext,
                maxResults: _maxResults,
                cancellationToken: cancellationToken);

            var requiresEscalation = kbResponse.ConfidenceScore < _confidenceThreshold;

            if (requiresEscalation)
            {
                _logger.LogWarning(
                    "Low confidence score {Confidence} for question, escalating to expert",
                    kbResponse.ConfidenceScore);

                await _expertEscalationHandler.EscalateQuestionAsync(
                    question, kbResponse.ConfidenceScore, cancellationToken);
            }

            await SaveConversationExchangeAsync(
                question.FarmerId, session,
                question.QuestionText, kbResponse.Answer, cancellationToken);

            var response = new AdvisoryResponse(
                AnswerText: kbResponse.Answer,
                Sources: kbResponse.Citations,
                RequiresExpertEscalation: requiresEscalation,
                RespondedAt: DateTimeOffset.UtcNow);

            _logger.LogInformation(
                "Advisory response generated with {CitationCount} citations, escalation: {Escalation}",
                kbResponse.Citations.Count(), requiresEscalation);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing advisory question");
            throw;
        }
    }

    private List<string> ExtractConversationHistory(dynamic? session)
    {
        try
        {
            if (session == null || session.Exchanges == null)
                return new List<string>();

            var exchanges = (IEnumerable<ConversationExchange>)session.Exchanges;
            return exchanges
                .OrderByDescending(e => e.Timestamp)
                .Take(10)
                .SelectMany(e => new[] { $"Q: {e.Question}", $"A: {e.Answer}" })
                .Reverse()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting conversation history, continuing without context");
            return new List<string>();
        }
    }

    private string BuildEnrichedContext(string baseContext, List<string> conversationHistory)
    {
        if (!conversationHistory.Any())
        {
            return baseContext;
        }

        var contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine(baseContext);
        contextBuilder.AppendLine();
        contextBuilder.AppendLine("Previous conversation:");
        
        foreach (var exchange in conversationHistory)
        {
            contextBuilder.AppendLine(exchange);
        }

        return contextBuilder.ToString();
    }

    private async Task SaveConversationExchangeAsync(
        string farmerId,
        dynamic? existingSession,
        string question,
        string answer,
        CancellationToken cancellationToken)
    {
        try
        {
            // OPTIMIZED: Reuse the session already fetched, no second DynamoDB read.
            string sessionId;
            List<ConversationExchange> exchanges;

            if (existingSession == null)
            {
                sessionId = Guid.NewGuid().ToString();
                exchanges = new List<ConversationExchange>();
            }
            else
            {
                sessionId = existingSession.SessionId;
                exchanges = ((IEnumerable<ConversationExchange>)existingSession.Exchanges).ToList();
            }

            var exchange = new ConversationExchange(
                Question: question,
                Answer: answer,
                Timestamp: DateTimeOffset.UtcNow
            );

            exchanges.Add(exchange);

            // Keep only last 10 exchanges
            if (exchanges.Count > 10)
            {
                exchanges = exchanges
                    .OrderByDescending(e => e.Timestamp)
                    .Take(10)
                    .OrderBy(e => e.Timestamp)
                    .ToList();
            }

            await _sessionRepository.SaveSessionAsync(
                farmerId,
                sessionId,
                exchanges,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error saving conversation exchange, continuing");
        }
    }
}
