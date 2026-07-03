namespace KisanMitraAI.Core.Advisory;

public interface IKnowledgeBaseService
{
    Task<KnowledgeBaseResponse> QueryKnowledgeBaseAsync(
        string query,
        string context,
        int maxResults,
        CancellationToken cancellationToken);
}

public record KnowledgeBaseResponse(
    string Answer,
    IEnumerable<Citation> Citations,
    float ConfidenceScore);

public record Citation(
    string DocumentTitle,
    string DocumentUri,
    string RelevantExcerpt,
    float RelevanceScore);
