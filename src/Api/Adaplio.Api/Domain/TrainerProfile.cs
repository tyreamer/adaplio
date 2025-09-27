using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("trainer_profile")]
public class TrainerProfile
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("full_name")]
    [MaxLength(200)]
    public string? FullName { get; set; }

    [Column("practice_name")]
    [MaxLength(200)]
    public string? PracticeName { get; set; }

    [Column("license_number")]
    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [Column("bio")]
    public string? Bio { get; set; }

    [Column("credentials")]
    [MaxLength(200)]
    public string? Credentials { get; set; }

    [Column("location")]
    [MaxLength(200)]
    public string? Location { get; set; }

    [Column("website")]
    [MaxLength(500)]
    public string? Website { get; set; }

    [Column("specialties_json")]
    public string? SpecialtiesJson { get; set; } // JSON array of specialties

    [Column("availability_json")]
    public string? AvailabilityJson { get; set; } // JSON array of availability windows

    [Column("default_reminder_time")]
    [MaxLength(5)]
    public string? DefaultReminderTime { get; set; } // HH:mm format

    [Column("logo_url")]
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [Column("mfa_enabled")]
    public bool MfaEnabled { get; set; } = false;

    [Column("mfa_secret")]
    [MaxLength(255)]
    public string? MfaSecret { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; } = null!;

    public ICollection<ConsentGrant> ConsentGrants { get; set; } = [];
    public ICollection<PlanTemplate> PlanTemplates { get; set; } = [];
    public ICollection<PlanProposal> PlanProposals { get; set; } = [];
}