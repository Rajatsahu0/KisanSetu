using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KisanMitraAI.Infrastructure.Data.Entities;

/// <summary>
/// Entity Framework entity for Farm
/// </summary>
[Table("Farms")]
public class FarmEntity
{
    [Key]
    [MaxLength(100)]
    public string FarmId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FarmerId { get; set; } = string.Empty;

    [Required]
    public float AreaInAcres { get; set; }

    [MaxLength(100)]
    public string SoilType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string IrrigationType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string CurrentCrops { get; set; } = string.Empty; // JSON array

    public double Latitude { get; set; }
    
    public double Longitude { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    
    public DateTimeOffset UpdatedAt { get; set; }
}
