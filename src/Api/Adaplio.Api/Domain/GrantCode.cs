using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("grant_code")]
public class GrantCode
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("trainer_profile_id")]
    public int TrainerProfileId { get; set; }

    [Required]
    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }

    [Column("used_at")]
    public DateTimeOffset? UsedAt { get; set; }

    [Column("used_by_client_profile_id")]
    public int? UsedByClientProfileId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    // Navigation properties
    [ForeignKey(nameof(TrainerProfileId))]
    public TrainerProfile TrainerProfile { get; set; } = null!;

    [ForeignKey(nameof(UsedByClientProfileId))]
    public ClientProfile? UsedByClientProfile { get; set; }
}