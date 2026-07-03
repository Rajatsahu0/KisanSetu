using Microsoft.EntityFrameworkCore;
using KisanMitraAI.Infrastructure.Data.Entities;

namespace KisanMitraAI.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for KisanMitra AI application
/// Manages PostgreSQL database operations for farms, audit logs, and regenerative plans
/// </summary>
public class KisanMitraDbContext : DbContext
{
    public KisanMitraDbContext(DbContextOptions<KisanMitraDbContext> options)
        : base(options)
    {
    }

    public DbSet<FarmEntity> Farms { get; set; } = null!;
    public DbSet<AuditLogEntity> AuditLogs { get; set; } = null!;
    public DbSet<RegenerativePlanEntity> RegenerativePlans { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Farm entity
        modelBuilder.Entity<FarmEntity>(entity =>
        {
            entity.HasKey(e => e.FarmId);
            entity.HasIndex(e => e.FarmerId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.HasIndex(e => e.FarmerId);
            entity.HasIndex(e => e.Timestamp);
            entity.Property(e => e.Timestamp).HasDefaultValueSql("NOW()");
        });

        // Configure RegenerativePlan entity
        modelBuilder.Entity<RegenerativePlanEntity>(entity =>
        {
            entity.HasKey(e => e.PlanId);
            entity.HasIndex(e => e.FarmerId);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });
    }
}
