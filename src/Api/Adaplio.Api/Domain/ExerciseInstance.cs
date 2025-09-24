using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("exercise_instance")]
public class ExerciseInstance
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("plan_instance_id")]
    public int PlanInstanceId { get; set; }

    [Column("exercise_id")]
    public int ExerciseId { get; set; }

    [Column("week_number")]
    public int WeekNumber { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; }

    [Column("target_sets")]
    public int? TargetSets { get; set; }

    [Column("target_reps")]
    public int? TargetReps { get; set; }

    [Column("target_hold_seconds")]
    public int? TargetHoldSeconds { get; set; }

    [Column("frequency_per_week")]
    public int? FrequencyPerWeek { get; set; }

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "pending"; // pending, in_progress, completed, skipped

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(PlanInstanceId))]
    public PlanInstance PlanInstance { get; set; } = null!;

    [ForeignKey(nameof(ExerciseId))]
    public Exercise Exercise { get; set; } = null!;

    public ICollection<ProgressEvent> ProgressEvents { get; set; } = [];
}