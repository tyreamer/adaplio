namespace Adaplio.Api.Gamification;

// Client responses
public record ClientGamificationResponse(
    string ClientAlias,
    int XpTotal,
    int Level,
    int XpForNextLevel,
    double LevelProgress,
    int CurrentStreakDays,
    int LongestStreakDays,
    int WeeklyStreakWeeks,
    int LongestWeeklyStreak,
    BadgeDto[] Badges
);

public record BadgeDto(
    string Id,
    string Name,
    string Description,
    string Icon,
    string Color,
    string Rarity,
    DateTimeOffset EarnedAt
);

// Trainer view responses
public record TrainerClientGamificationResponse(
    string ClientAlias,
    int Level,
    int XpTotal,
    int CurrentStreakDays,
    int TotalBadges,
    BadgeDto[] RecentBadges // Last 3 badges
);

// Progress celebration response (returned after logging progress)
public record ProgressCelebrationResponse(
    string Message,
    int ProgressEventId,
    CelebrationData? Celebration
);

public record CelebrationData(
    int XpAwarded,
    bool LeveledUp,
    int? NewLevel,
    BadgeDto[] NewBadges,
    int CurrentStreak
);