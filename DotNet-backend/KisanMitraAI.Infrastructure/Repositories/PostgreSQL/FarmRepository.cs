using KisanMitraAI.Core.Models;
using KisanMitraAI.Infrastructure.Data;
using KisanMitraAI.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.Repositories.PostgreSQL;

/// <summary>
/// Implementation of farm repository using PostgreSQL
/// </summary>
public class FarmRepository : IFarmRepository
{
    private readonly KisanMitraDbContext _context;
    private readonly ILogger<FarmRepository> _logger;

    public FarmRepository(
        KisanMitraDbContext context,
        ILogger<FarmRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> CreateAsync(FarmProfile farm, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = new FarmEntity
            {
                FarmId = farm.FarmId,
                FarmerId = farm.FarmerId,
                AreaInAcres = farm.AreaInAcres,
                SoilType = farm.SoilType,
                IrrigationType = farm.IrrigationType,
                CurrentCrops = JsonSerializer.Serialize(farm.CurrentCrops),
                Latitude = farm.Coordinates.Latitude,
                Longitude = farm.Coordinates.Longitude,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _context.Farms.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created farm {FarmId} for farmer {FarmerId}", farm.FarmId, farm.FarmerId);
            return entity.FarmId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating farm {FarmId}", farm.FarmId);
            throw;
        }
    }

    public async Task<FarmProfile?> GetByIdAsync(string farmId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.Farms
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FarmId == farmId, cancellationToken);

            return entity == null ? null : MapToModel(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting farm {FarmId}", farmId);
            throw;
        }
    }

    public async Task<IEnumerable<FarmProfile>> GetByFarmerIdAsync(string farmerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entities = await _context.Farms
                .AsNoTracking()
                .Where(f => f.FarmerId == farmerId)
                .ToListAsync(cancellationToken);

            return entities.Select(MapToModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting farms for farmer {FarmerId}", farmerId);
            throw;
        }
    }

    public async Task UpdateAsync(FarmProfile farm, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.Farms
                .FirstOrDefaultAsync(f => f.FarmId == farm.FarmId, cancellationToken);

            if (entity == null)
            {
                throw new InvalidOperationException($"Farm {farm.FarmId} not found");
            }

            entity.AreaInAcres = farm.AreaInAcres;
            entity.SoilType = farm.SoilType;
            entity.IrrigationType = farm.IrrigationType;
            entity.CurrentCrops = JsonSerializer.Serialize(farm.CurrentCrops);
            entity.Latitude = farm.Coordinates.Latitude;
            entity.Longitude = farm.Coordinates.Longitude;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated farm {FarmId}", farm.FarmId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating farm {FarmId}", farm.FarmId);
            throw;
        }
    }

    public async Task DeleteAsync(string farmId, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.Farms
                .FirstOrDefaultAsync(f => f.FarmId == farmId, cancellationToken);

            if (entity != null)
            {
                _context.Farms.Remove(entity);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Deleted farm {FarmId}", farmId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting farm {FarmId}", farmId);
            throw;
        }
    }

    private FarmProfile MapToModel(FarmEntity entity)
    {
        var crops = JsonSerializer.Deserialize<List<string>>(entity.CurrentCrops) ?? new List<string>();
        var coordinates = new GeoCoordinates(entity.Latitude, entity.Longitude);

        return new FarmProfile(
            farmId: entity.FarmId,
            farmerId: entity.FarmerId,
            areaInAcres: entity.AreaInAcres,
            soilType: entity.SoilType,
            irrigationType: entity.IrrigationType,
            currentCrops: crops,
            coordinates: coordinates
        );
    }
}
