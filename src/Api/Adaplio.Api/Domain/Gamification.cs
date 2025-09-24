using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("gamification")]
public class Gamification
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("client_profile_id")]
    public int ClientProfileId { get; set; }

    [Column("total_xp")]
    public int TotalXp { get; set; } = 0;

    [Column("current_level")]
    public int CurrentLevel { get; set; } = 1;

    [Column("current_streak")]
    public int CurrentStreak { get; set; } = 0;

    [Column("longest_streak")]
    public int LongestStreak { get; set; } = 0;

    [Column("last_activity_date")]
    public DateOnly? LastActivityDate { get; set; }

    [Column("badges_earned")]
    public string? BadgesEarned { get; set; } // JSON array of badge IDs

    [Column("milestones_reached")]
    public string? MilestonesReached { get; set; } // JSON array of milestone data

    [Column("weekly_goals_met")]
    public int WeeklyGoalsMet { get; set; } = 0;

    [Column("total_sessions")]
    public int TotalSessions { get; set; } = 0;

    [Column("total_exercises_completed")]
    public int TotalExercisesCompleted { get; set; } = 0;

    [Column("total_hold_seconds")]
    public int TotalHoldSeconds { get; set; } = 0;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ClientProfileId))]
    public ClientProfile ClientProfile { get; set; } = null!;
}