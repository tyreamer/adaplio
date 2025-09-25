using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Adaplio.Api.Domain;

[Table("gamification")]
public class Gamification
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("xp_total")]
    public int XpTotal { get; set; } = 0;

    [Column("current_streak_days")]
    public int CurrentStreakDays { get; set; } = 0;

    [Column("longest_streak_days")]
    public int LongestStreakDays { get; set; } = 0;

    [Column("weekly_streak_weeks")]
    public int WeeklyStreakWeeks { get; set; } = 0;

    [Column("longest_weekly_streak")]
    public int LongestWeeklyStreak { get; set; } = 0;

    [Column("last_activity_date")]
    public DateOnly? LastActivityDate { get; set; }

    [Column("badges_json")]
    public string BadgesJson { get; set; } = "[]";

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation property (using UserId as FK to client_profile.id)
    [ForeignKey(nameof(UserId))]
    public ClientProfile ClientProfile { get; set; } = null!;

    // Convenience property for working with badges
    [NotMapped]
    public List<Badge> Badges
    {
        get => string.IsNullOrEmpty(BadgesJson) || BadgesJson == "[]"
            ? new List<Badge>()
            : JsonSerializer.Deserialize<List<Badge>>(BadgesJson) ?? new List<Badge>();
        set => BadgesJson = JsonSerializer.Serialize(value);
    }

    // Calculated level based on XP (1 + floor(sqrt(xp_total / 10)))
    [NotMapped]
    public int Level => 1 + (int)Math.Floor(Math.Sqrt(XpTotal / 10.0));

    // XP needed for next level
    [NotMapped]
    public int XpForNextLevel
    {
        get
        {
            var nextLevel = Level + 1;
            return (nextLevel - 1) * (nextLevel - 1) * 10;
        }
    }

    // Progress to next level (0-1)
    [NotMapped]
    public double LevelProgress
    {
        get
        {
            var currentLevelXp = (Level - 1) * (Level - 1) * 10;
            var nextLevelXp = XpForNextLevel;
            var progressXp = XpTotal - currentLevelXp;
            var levelRangeXp = nextLevelXp - currentLevelXp;
            return levelRangeXp > 0 ? (double)progressXp / levelRangeXp : 1.0;
        }
    }
}

[Table("xp_award")]
public class XpAward
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("progress_event_id")]
    public int ProgressEventId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("xp_awarded")]
    public int XpAwarded { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ProgressEventId))]
    public ProgressEvent ProgressEvent { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public ClientProfile ClientProfile { get; set; } = null!;
}

public class Badge
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty; // Emoji or icon identifier
    public string Color { get; set; } = string.Empty; // Hex color
    public DateTimeOffset EarnedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Rarity { get; set; } = "common"; // common, rare, epic, legendary
}