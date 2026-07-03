namespace KisanMitraAI.Infrastructure.Repositories.DynamoDB;

/// <summary>
/// Repository for conversation session data in DynamoDB
/// </summary>
public interface ISessionRepository
{
    /// <summary>
    /// Saves a conversation session (up to 10 exchanges)
    /// </summary>
    Task SaveSessionAsync(
        string farmerId,
        string sessionId,
        List<ConversationExchange> exchanges,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a conversation session
    /// </summary>
    Task<ConversationSession?> GetSessionAsync(
        string farmerId,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest conversation session for a farmer
    /// </summary>
    Task<ConversationSession?> GetSessionAsync(
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an exchange to an existing session
    /// </summary>
    Task AddExchangeAsync(
        string farmerId,
        string sessionId,
        ConversationExchange exchange,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a conversation session
/// </summary>
public record ConversationSession(
    string SessionId,
    string FarmerId,
    List<ConversationExchange> Exchanges,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt);

/// <summary>
/// Represents a single conversation exchange
/// </summary>
public record ConversationExchange(
    string Question,
    string Answer,
    DateTimeOffset Timestamp);
