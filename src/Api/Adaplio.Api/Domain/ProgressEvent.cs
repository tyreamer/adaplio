using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("progress_event")]
public class ProgressEvent
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("exercise_instance_id")]
    public int ExerciseInstanceId { get; set; }

    [Column("event_type")]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty; // "set_completed", "exercise_completed", "session_completed"

    [Column("sets_completed")]
    public int? SetsCompleted { get; set; }

    [Column("reps_completed")]
    public int? RepsCompleted { get; set; }

    [Column("hold_seconds_completed")]
    public int? HoldSecondsCompleted { get; set; }

    [Column("difficulty_rating")]
    public int? DifficultyRating { get; set; } // 1-10 scale

    [Column("pain_level")]
    public int? PainLevel { get; set; } // 1-10 scale

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("logged_at")]
    public DateTimeOffset LoggedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("session_id")]
    [MaxLength(100)]
    public string? SessionId { get; set; } // Group related events

    // Navigation properties
    [ForeignKey(nameof(ClientProfileId))]
    public ClientProfile ClientProfile { get; set; } = null!;

    [ForeignKey(nameof(ExerciseInstanceId))]
    public ExerciseInstance ExerciseInstance { get; set; } = null!;
}