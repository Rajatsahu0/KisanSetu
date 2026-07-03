using KisanMitraAI.Infrastructure.Data;
using KisanMitraAI.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.Repositories.PostgreSQL;

/// <summary>
/// Implementation of audit log repository using PostgreSQL
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly KisanMitraDbContext _context;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(
        KisanMitraDbContext context,
        ILogger<AuditLogRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task LogActionAsync(
        string farmerId,
        string action,
        string resourceType,
        string resourceId,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        string status = "Success",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new AuditLogEntity
            {
                FarmerId = farmerId,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Details = details,
                IpAddress = ipAddress ?? string.Empty,
                UserAgent = userAgent ?? string.Empty,
                Timestamp = DateTimeOffset.UtcNow,
                Status = status
            };

            _context.AuditLogs.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Logged action {Action} for farmer {FarmerId} on {ResourceType}/{ResourceId}", 
                action, farmerId, resourceType, resourceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging action {Action} for farmer {FarmerId}", action, farmerId);
            // Don't throw - audit logging should not break the main flow
        }
    }

    public async Task<IEnumerable<AuditLogEntry>> GetAuditTrailAsync(
        string farmerId,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.AuditLogs
                .AsNoTracking()
                .Where(log => log.FarmerId == farmerId);

            if (startDate.HasValue)
            {
                query = query.Where(log => log.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(log => log.Timestamp <= endDate.Value);
            }

            var entities = await query
                .OrderByDescending(log => log.Timestamp)
                .ToListAsync(cancellationToken);

            return entities.Select(e => new AuditLogEntry(
                LogId: e.LogId,
                FarmerId: e.FarmerId,
                Action: e.Action,
                ResourceType: e.ResourceType,
                ResourceId: e.ResourceId,
                Details: e.Details,
                IpAddress: e.IpAddress,
                UserAgent: e.UserAgent,
                Timestamp: e.Timestamp,
                Status: e.Status
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit trail for farmer {FarmerId}", farmerId);
            throw;
        }
    }
}
