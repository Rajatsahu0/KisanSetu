using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KisanMitraAI.Infrastructure.Data.Entities;

/// <summary>
/// Entity Framework entity for AuditLog
/// </summary>
[Table("AuditLogs")]
public class AuditLogEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long LogId { get; set; }

    [Required]
    [MaxLength(100)]
    public string FarmerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ResourceType { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ResourceId { get; set; } = string.Empty;

    [Column(TypeName = "jsonb")]
    public string? Details { get; set; }

    [MaxLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string UserAgent { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = string.Empty; // Success, Failed, etc.
}
