using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("plan_template")]
public class PlanTemplate
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("trainer_profile_id")]
    public int TrainerProfileId { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("category")]
    [MaxLength(100)]
    public string? Category { get; set; }

    [Column("duration_weeks")]
    public int? DurationWeeks { get; set; }

    [Column("is_public")]
    public bool IsPublic { get; set; } = false;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(TrainerProfileId))]
    public TrainerProfile TrainerProfile { get; set; } = null!;

    public ICollection<PlanTemplateItem> PlanTemplateItems { get; set; } = [];
    public ICollection<PlanProposal> PlanProposals { get; set; } = [];
}