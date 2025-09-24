using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("plan_template_item")]
public class PlanTemplateItem
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("plan_template_id")]
    public int PlanTemplateId { get; set; }

    [Column("exercise_id")]
    public int ExerciseId { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; }

    [Column("sets")]
    public int? Sets { get; set; }

    [Column("reps")]
    public int? Reps { get; set; }

    [Column("hold_seconds")]
    public int? HoldSeconds { get; set; }

    [Column("frequency_per_week")]
    public int? FrequencyPerWeek { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("days_of_week")]
    [MaxLength(20)]
    public string? DaysOfWeek { get; set; } // JSON array like ["Monday", "Wednesday", "Friday"]

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(PlanTemplateId))]
    public PlanTemplate PlanTemplate { get; set; } = null!;

    [ForeignKey(nameof(ExerciseId))]
    public Exercise Exercise { get; set; } = null!;
}