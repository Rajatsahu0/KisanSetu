using KisanMitraAI.Core.Models;
using KisanMitraAI.Infrastructure.Data;
using KisanMitraAI.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.Repositories.PostgreSQL;

/// <summary>
/// Implementation of regenerative plan repository using PostgreSQL
/// </summary>
public class RegenerativePlanRepository : IRegenerativePlanRepository
{
    private readonly KisanMitraDbContext _context;
    private readonly ILogger<RegenerativePlanRepository> _logger;

    public RegenerativePlanRepository(
        KisanMitraDbContext context,
        ILogger<RegenerativePlanRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> SavePlanAsync(RegenerativePlan plan, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new RegenerativePlanEntity
            {
                PlanId = plan.PlanId,
                FarmerId = plan.FarmerId,
                Recommendations = JsonSerializer.Serialize(plan.Recommendations),
                MonthlyActions = JsonSerializer.Serialize(plan.MonthlyActions),
                CarbonEstimate = JsonSerializer.Serialize(plan.CarbonEstimate),
                CreatedAt = plan.CreatedAt,
                ValidUntil = plan.ValidUntil,
                EstimatedCostSavings = plan.EstimatedCostSavings
            };

            _context.RegenerativePlans.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Saved regenerative plan {PlanId} for farmer {FarmerId}", plan.PlanId, plan.FarmerId);
            return entity.PlanId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving regenerative plan {PlanId}", plan.PlanId);
            throw;
        }
    }

    public async Task<RegenerativePlan?> GetPlanAsync(string planId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.RegenerativePlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlanId == planId, cancellationToken);

            return entity == null ? null : MapToModel(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting regenerative plan {PlanId}", planId);
            throw;
        }
    }

    public async Task<IEnumerable<RegenerativePlan>> GetPlansByFarmerAsync(string farmerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.RegenerativePlans
                .AsNoTracking()
                .Where(p => p.FarmerId == farmerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(cancellationToken);

            return entities.Select(MapToModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting regenerative plans for farmer {FarmerId}", farmerId);
            throw;
        }
    }

    private RegenerativePlan MapToModel(RegenerativePlanEntity entity)
    {
        var recommendations = JsonSerializer.Deserialize<List<PlanRecommendation>>(entity.Recommendations ?? "[]") 
            ?? new List<PlanRecommendation>();
        var monthlyActions = JsonSerializer.Deserialize<List<MonthlyAction>>(entity.MonthlyActions) 
            ?? new List<MonthlyAction>();
        var carbonEstimate = JsonSerializer.Deserialize<CarbonSequestrationEstimate>(entity.CarbonEstimate)
            ?? throw new InvalidOperationException("Invalid carbon estimate data");

        return new RegenerativePlan(
            planId: entity.PlanId,
            farmerId: entity.FarmerId,
            recommendations: recommendations,
            monthlyActions: monthlyActions,
            carbonEstimate: carbonEstimate,
            createdAt: entity.CreatedAt,
            validUntil: entity.ValidUntil,
            estimatedCostSavings: entity.EstimatedCostSavings
        );
    }
}
