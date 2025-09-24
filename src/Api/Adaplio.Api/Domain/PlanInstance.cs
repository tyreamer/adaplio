using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("plan_instance")]
public class PlanInstance
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("plan_proposal_id")]
    public int PlanProposalId { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "active"; // active, paused, completed, cancelled

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("planned_end_date")]
    public DateOnly? PlannedEndDate { get; set; }

    [Column("actual_end_date")]
    public DateOnly? ActualEndDate { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ClientProfileId))]
    public ClientProfile ClientProfile { get; set; } = null!;

    [ForeignKey(nameof(PlanProposalId))]
    public PlanProposal PlanProposal { get; set; } = null!;

    public ICollection<ExerciseInstance> ExerciseInstances { get; set; } = [];
    public ICollection<PlanItemAcceptance> PlanItemAcceptances { get; set; } = [];
}