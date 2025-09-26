using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

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
    public int CurrentLevelStored { get; set; } = 1;

    [Column("current_streak")]
    public int CurrentStreak { get; set; } = 0;

    [Column("longest_streak")]
    public int LongestStreak { get; set; } = 0;

    [Column("weekly_streaks")]
    public int WeeklyStreaks { get; set; } = 0;

    [Column("longest_weekly_streak")]
    public int LongestWeeklyStreak { get; set; } = 0;

    [Column("last_activity_date")]
    public DateOnly? LastActivityDate { get; set; }

    [Column("badges_earned")]
    public string BadgesEarned { get; set; } = "[]";

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation property
    [ForeignKey(nameof(ClientProfileId))]
    public ClientProfile ClientProfile { get; set; } = null!;

    // Convenience property for working with badges
    [NotMapped]
    public List<Badge> Badges
    {
        get
        {
            try
            {
                return string.IsNullOrEmpty(BadgesEarned) || BadgesEarned == "[]"
                    ? new List<Badge>()
                    : JsonSerializer.Deserialize<List<Badge>>(BadgesEarned) ?? new List<Badge>();
            }
            catch (JsonException)
            {
                return new List<Badge>();
            }
        }
        set => BadgesEarned = JsonSerializer.Serialize(value);
    }

    // Calculated level based on XP (1 + floor(sqrt(xp_total / 10)))
    [NotMapped]
    public int Level => 1 + (int)Math.Floor(Math.Sqrt(TotalXp / 10.0));

    // XP needed for next level (additional XP, not total)
    [NotMapped]
    public int XpForNextLevel
    {
        get
        {
            var nextLevel = Level + 1;
            var nextLevelTotalXp = (nextLevel - 1) * (nextLevel - 1) * 10;
            return nextLevelTotalXp - TotalXp;
        }
    }

    // Progress to next level (0-1)
    [NotMapped]
    public double LevelProgress
    {
        get
        {
            var currentLevelXp = (Level - 1) * (Level - 1) * 10;
            var nextLevel = Level + 1;
            var nextLevelXp = (nextLevel - 1) * (nextLevel - 1) * 10;
            var progressXp = TotalXp - currentLevelXp;
            var levelRangeXp = nextLevelXp - currentLevelXp;
            return levelRangeXp > 0 ? (double)progressXp / levelRangeXp : 1.0;
        }
    }

    // Alias properties for backwards compatibility with frontend
    [NotMapped]
    public int XpTotal => TotalXp;

    [NotMapped]
    public int CurrentStreakDays => CurrentStreak;

    [NotMapped]
    public int LongestStreakDays => LongestStreak;

    [NotMapped]
    public int WeeklyStreakWeeks => WeeklyStreaks;
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