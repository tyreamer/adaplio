using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("plan_item_acceptance")]
public class PlanItemAcceptance
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("plan_instance_id")]
    public int PlanInstanceId { get; set; }

    [Column("exercise_instance_id")]
    public int ExerciseInstanceId { get; set; }

    [Column("accepted")]
    public bool Accepted { get; set; } = true;

    [Column("reason")]
    [MaxLength(500)]
    public string? Reason { get; set; } // Reason for declining if applicable

    [Column("accepted_at")]
    public DateTimeOffset AcceptedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("modified_sets")]
    public int? ModifiedSets { get; set; }

    [Column("modified_reps")]
    public int? ModifiedReps { get; set; }

    [Column("modified_hold_seconds")]
    public int? ModifiedHoldSeconds { get; set; }

    // Navigation properties
    [ForeignKey(nameof(PlanInstanceId))]
    public PlanInstance PlanInstance { get; set; } = null!;

    [ForeignKey(nameof(ExerciseInstanceId))]
    public ExerciseInstance ExerciseInstance { get; set; } = null!;
}