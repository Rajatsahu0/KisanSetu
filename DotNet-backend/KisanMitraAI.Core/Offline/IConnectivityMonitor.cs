namespace KisanMitraAI.Core.Offline;

/// <summary>
/// Service for monitoring network connectivity
/// </summary>
public interface IConnectivityMonitor
{
    /// <summary>
    /// Check if network connectivity is available
    /// </summary>
    Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current connectivity status
    /// </summary>
    Task<ConnectivityStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to connectivity status changes
    /// </summary>
    IObservable<ConnectivityStatus> ObserveConnectivityChanges();

    /// <summary>
    /// Check if an operation requires network connectivity
    /// </summary>
    bool RequiresConnectivity(string operationType);
}

/// <summary>
/// Represents network connectivity status
/// </summary>
public record ConnectivityStatus(
    bool IsOnline,
    DateTimeOffset LastCheckedAt,
    string? Message = null);
