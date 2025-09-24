using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("adherence_week")]
public class AdherenceWeek
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("plan_instance_id")]
    public int? PlanInstanceId { get; set; }

    [Column("year")]
    public int Year { get; set; }

    [Column("week_number")]
    public int WeekNumber { get; set; } // 1-53

    [Column("week_start_date")]
    public DateOnly WeekStartDate { get; set; }

    [Column("total_exercises_planned")]
    public int TotalExercisesPlanned { get; set; }

    [Column("total_exercises_completed")]
    public int TotalExercisesCompleted { get; set; }

    [Column("adherence_percentage")]
    public decimal AdherencePercentage { get; set; }

    [Column("total_hold_seconds_planned")]
    public int TotalHoldSecondsPlanned { get; set; }

    [Column("total_hold_seconds_completed")]
    public int TotalHoldSecondsCompleted { get; set; }

    [Column("average_difficulty_rating")]
    public decimal? AverageDifficultyRating { get; set; }

    [Column("average_pain_level")]
    public decimal? AveragePainLevel { get; set; }

    [Column("calculated_at")]
    public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ClientProfileId))]
    public ClientProfile ClientProfile { get; set; } = null!;

    [ForeignKey(nameof(PlanInstanceId))]
    public PlanInstance? PlanInstance { get; set; }
}