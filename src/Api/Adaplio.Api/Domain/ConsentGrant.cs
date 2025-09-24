using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("consent_grant")]
public class ConsentGrant
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("trainer_profile_id")]
    public int TrainerProfileId { get; set; }

    [Column("scope")]
    [MaxLength(100)]
    public string Scope { get; set; } = string.Empty; // e.g. "propose_plan", "view_summary", "message_client"

    [Column("granted_at")]
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [Column("revoked_at")]
    public DateTimeOffset? RevokedAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ClientProfileId))]
    public ClientProfile ClientProfile { get; set; } = null!;

    [ForeignKey(nameof(TrainerProfileId))]
    public TrainerProfile TrainerProfile { get; set; } = null!;
}