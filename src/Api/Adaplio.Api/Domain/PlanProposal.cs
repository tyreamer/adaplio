using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("plan_proposal")]
public class PlanProposal
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("trainer_profile_id")]
    public int TrainerProfileId { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("plan_template_id")]
    public int? PlanTemplateId { get; set; }

    [Required]
    [Column("proposal_name")]
    [MaxLength(200)]
    public string ProposalName { get; set; } = string.Empty;

    [Column("message")]
    public string? Message { get; set; }

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "pending"; // pending, accepted, declined, expired

    [Column("proposed_at")]
    public DateTimeOffset ProposedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }

    [Column("responded_at")]
    public DateTimeOffset? RespondedAt { get; set; }

    [Column("custom_plan_json")]
    public string? CustomPlanJson { get; set; } // JSON blob for custom plan if not using template

    [Column("starts_on")]
    public DateOnly? StartsOn { get; set; } // When the plan should start (default: next Monday)

    // Navigation properties
    [ForeignKey(nameof(TrainerProfileId))]
    public TrainerProfile TrainerProfile { get; set; } = null!;

    [ForeignKey(nameof(ClientProfileId))]
    public ClientProfile ClientProfile { get; set; } = null!;

    [ForeignKey(nameof(PlanTemplateId))]
    public PlanTemplate? PlanTemplate { get; set; }

    public PlanInstance? PlanInstance { get; set; } // Created when accepted
}