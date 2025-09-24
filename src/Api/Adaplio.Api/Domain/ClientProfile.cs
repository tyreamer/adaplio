using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("client_profile")]
public class ClientProfile
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("alias")]
    [MaxLength(50)]
    public string? Alias { get; set; } // e.g. "C-7Q2F" for pseudonymity

    [Column("display_name")]
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    [Column("timezone")]
    [MaxLength(50)]
    public string? Timezone { get; set; }

    [Column("preferences_json")]
    public string? PreferencesJson { get; set; } // JSON blob for client preferences

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; } = null!;

    public ICollection<ConsentGrant> ConsentGrants { get; set; } = [];
    public ICollection<PlanInstance> PlanInstances { get; set; } = [];
    public ICollection<ProgressEvent> ProgressEvents { get; set; } = [];
    public ICollection<AdherenceWeek> AdherenceWeeks { get; set; } = [];
    public Gamification? Gamification { get; set; }
}