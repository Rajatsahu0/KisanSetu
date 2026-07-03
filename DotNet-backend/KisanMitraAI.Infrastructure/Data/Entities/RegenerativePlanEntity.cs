using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KisanMitraAI.Infrastructure.Data.Entities;

/// <summary>
/// Entity Framework entity for RegenerativePlan
/// </summary>
[Table("RegenerativePlans")]
public class RegenerativePlanEntity
{
    [Key]
    [MaxLength(100)]
    public string PlanId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FarmerId { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string? Recommendations { get; set; } // JSON

    [Required]
    [Column(TypeName = "jsonb")]
    public string MonthlyActions { get; set; } = string.Empty; // JSON

    [Required]
    [Column(TypeName = "jsonb")]
    public string CarbonEstimate { get; set; } = string.Empty; // JSON

    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset? ValidUntil { get; set; }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal EstimatedCostSavings { get; set; }
}
