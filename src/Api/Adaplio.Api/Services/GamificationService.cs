using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Adaplio.Api.Services;

public interface IGamificationService
{
    Task<GamificationResult> AwardXpForProgressAsync(int progressEventId, int clientProfileId);
    Task<Domain.Gamification> GetOrCreateGamificationAsync(int clientProfileId);
    Task<Domain.Gamification?> GetGamificationAsync(int clientProfileId);
    Task<WeeklyProgressData> GetWeeklyProgressAsync(int clientProfileId, DateTime? weekStart = null);
}

public class GamificationService : IGamificationService
{
    private readonly AppDbContext _context;

    public GamificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GamificationResult> AwardXpForProgressAsync(int progressEventId, int clientProfileId)
    {
        if (progressEventId <= 0 || clientProfileId <= 0)
        {
            throw new ArgumentException("Invalid progressEventId or clientProfileId");
        }

        // Use a transaction to ensure idempotency
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Check if XP has already been awarded for this progress event (idempotency)
            var existingAward = await _context.XpAwards
                .FirstOrDefaultAsync(xa => xa.ProgressEventId == progressEventId);

            if (existingAward != null)
            {
                // Already awarded, return existing result
                var existingGamification = await GetOrCreateGamificationAsync(clientProfileId);
                return new GamificationResult
                {
                    XpAwarded = 0,
                    NewBadges = new List<Badge>(),
                    LeveledUp = false,
                    CurrentLevel = existingGamification.Level,
                    TotalXp = existingGamification.TotalXp,
                    CurrentStreak = existingGamification.CurrentStreak,
                    AlreadyAwarded = true
                };
            }

            // Get the progress event to determine XP amount
            var progressEvent = await _context.ProgressEvents
                .FirstOrDefaultAsync(pe => pe.Id == progressEventId);

            if (progressEvent == null)
            {
                throw new ArgumentException($"Progress event {progressEventId} not found");
            }

            // Calculate XP based on event type
            var xpAwarded = CalculateXpForEvent(progressEvent);

            // Get or create gamification record
            var gamification = await GetOrCreateGamificationAsync(clientProfileId);
            var previousLevel = gamification.Level;

            // Update streaks
            UpdateStreaks(gamification, DateOnly.FromDateTime(progressEvent.LoggedAt.Date));

            // Add XP
            gamification.TotalXp += xpAwarded;
            gamification.UpdatedAt = DateTimeOffset.UtcNow;

            // Check for new badges
            var newBadges = CheckForNewBadges(gamification, progressEvent);

            // Add new badges to the collection
            var allBadges = gamification.Badges;
            foreach (var newBadge in newBadges)
            {
                allBadges.Add(newBadge);
            }
            gamification.Badges = allBadges;

            // Record the XP award
            var xpAward = new XpAward
            {
                ProgressEventId = progressEventId,
                UserId = clientProfileId,
                XpAwarded = xpAwarded
            };
            _context.XpAwards.Add(xpAward);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new GamificationResult
            {
                XpAwarded = xpAwarded,
                NewBadges = newBadges,
                LeveledUp = gamification.Level > previousLevel,
                CurrentLevel = gamification.Level,
                TotalXp = gamification.TotalXp,
                CurrentStreak = gamification.CurrentStreak,
                AlreadyAwarded = false
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Domain.Gamification> GetOrCreateGamificationAsync(int clientProfileId)
    {
        var gamification = await _context.Gamifications
            .FirstOrDefaultAsync(g => g.ClientProfileId == clientProfileId);

        if (gamification == null)
        {
            gamification = new Domain.Gamification
            {
                ClientProfileId = clientProfileId
            };
            _context.Gamifications.Add(gamification);
            await _context.SaveChangesAsync();
        }

        return gamification;
    }

    public async Task<Domain.Gamification?> GetGamificationAsync(int clientProfileId)
    {
        return await _context.Gamifications
            .FirstOrDefaultAsync(g => g.ClientProfileId == clientProfileId);
    }

    private static int CalculateXpForEvent(ProgressEvent progressEvent)
    {
        return progressEvent.EventType switch
        {
            "set_completed" => 10,
            "exercise_completed" => 25,
            "session_completed" => 50,
            _ => 5 // Default fallback
        };
    }

    private static void UpdateStreaks(Domain.Gamification gamification, DateOnly eventDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        if (gamification.LastActivityDate == null)
        {
            // First activity
            gamification.CurrentStreak = 1;
            gamification.WeeklyStreaks = IsStartOfWeek(eventDate) ? 1 : 0;
        }
        else
        {
            var lastActivity = gamification.LastActivityDate.Value;
            var daysDiff = eventDate.DayNumber - lastActivity.DayNumber;

            if (daysDiff == 1)
            {
                // Consecutive day
                gamification.CurrentStreak++;
            }
            else if (daysDiff == 0)
            {
                // Same day, no streak change
                return;
            }
            else
            {
                // Streak broken
                gamification.CurrentStreak = 1;
            }

            // Update weekly streak
            if (IsStartOfWeek(eventDate) && gamification.CurrentStreak >= 7)
            {
                gamification.WeeklyStreaks++;
            }
        }

        // Update longest streaks
        if (gamification.CurrentStreak > gamification.LongestStreak)
        {
            gamification.LongestStreak = gamification.CurrentStreak;
        }

        if (gamification.WeeklyStreaks > gamification.LongestWeeklyStreak)
        {
            gamification.LongestWeeklyStreak = gamification.WeeklyStreaks;
        }

        gamification.LastActivityDate = eventDate;
    }

    private static bool IsStartOfWeek(DateOnly date)
    {
        return date.DayOfWeek == DayOfWeek.Monday;
    }

    private static List<Badge> CheckForNewBadges(Domain.Gamification gamification, ProgressEvent progressEvent)
    {
        var newBadges = new List<Badge>();
        var existingBadgeIds = gamification.Badges.Select(b => b.Id).ToHashSet();

        // First Steps Badge
        if (gamification.TotalXp >= 10 && !existingBadgeIds.Contains("first_steps"))
        {
            newBadges.Add(new Badge
            {
                Id = "first_steps",
                Name = "First Steps",
                Description = "Completed your first exercise",
                Icon = "🌱",
                Color = "#22C55E",
                Rarity = "common"
            });
        }

        // Streak Badges
        if (gamification.CurrentStreak >= 3 && !existingBadgeIds.Contains("streak_3"))
        {
            newBadges.Add(new Badge
            {
                Id = "streak_3",
                Name = "On a Roll",
                Description = "3 days in a row",
                Icon = "🔥",
                Color = "#F59E0B",
                Rarity = "common"
            });
        }

        if (gamification.CurrentStreak >= 7 && !existingBadgeIds.Contains("streak_7"))
        {
            newBadges.Add(new Badge
            {
                Id = "streak_7",
                Name = "Week Warrior",
                Description = "7 days in a row",
                Icon = "⚡",
                Color = "#3B82F6",
                Rarity = "rare"
            });
        }

        if (gamification.CurrentStreak >= 30 && !existingBadgeIds.Contains("streak_30"))
        {
            newBadges.Add(new Badge
            {
                Id = "streak_30",
                Name = "Unstoppable",
                Description = "30 days in a row",
                Icon = "🏆",
                Color = "#EF4444",
                Rarity = "legendary"
            });
        }

        // Level Badges
        if (gamification.Level >= 5 && !existingBadgeIds.Contains("level_5"))
        {
            newBadges.Add(new Badge
            {
                Id = "level_5",
                Name = "Rising Star",
                Description = "Reached level 5",
                Icon = "⭐",
                Color = "#8B5CF6",
                Rarity = "rare"
            });
        }

        if (gamification.Level >= 10 && !existingBadgeIds.Contains("level_10"))
        {
            newBadges.Add(new Badge
            {
                Id = "level_10",
                Name = "Elite Performer",
                Description = "Reached level 10",
                Icon = "💫",
                Color = "#EC4899",
                Rarity = "epic"
            });
        }

        // Pain Management Badge (shows user is managing pain well)
        if (progressEvent.PainLevel.HasValue && progressEvent.PainLevel <= 3 &&
            progressEvent.DifficultyRating.HasValue && progressEvent.DifficultyRating >= 7 &&
            !existingBadgeIds.Contains("pain_manager"))
        {
            newBadges.Add(new Badge
            {
                Id = "pain_manager",
                Name = "Pain Manager",
                Description = "Completed challenging exercise with low pain",
                Icon = "🛡️",
                Color = "#06B6D4",
                Rarity = "epic"
            });
        }

        return newBadges;
    }

    public async Task<WeeklyProgressData> GetWeeklyProgressAsync(int clientProfileId, DateTime? weekStart = null)
    {
        // Calculate week boundaries
        var startOfWeek = weekStart ?? GetStartOfWeek(DateTime.UtcNow);
        var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

        // Convert to DateTimeOffset for comparison (SQLite compatibility)
        var startOfWeekOffset = new DateTimeOffset(startOfWeek, TimeSpan.Zero);
        var endOfWeekOffset = new DateTimeOffset(endOfWeek, TimeSpan.Zero);

        // Get XP awards for this week (client-side filtering for SQLite compatibility)
        var allXpAwards = await _context.XpAwards
            .Where(xa => xa.UserId == clientProfileId)
            .ToListAsync();

        var weeklyXpAwards = allXpAwards
            .Where(xa => xa.CreatedAt >= startOfWeekOffset && xa.CreatedAt <= endOfWeekOffset)
            .Sum(xa => xa.XpAwarded);

        // Get total gamification data
        var gamification = await GetOrCreateGamificationAsync(clientProfileId);

        // Calculate tiers based on current level and total XP
        var tiers = CalculateWeeklyTiers(gamification.Level, gamification.TotalXp);

        // Calculate break-even threshold (minimum weekly XP expected)
        var breakEven = CalculateBreakEvenXp(gamification.Level);

        // Calculate next estimate
        var nextTier = tiers.FirstOrDefault(t => t.Threshold > weeklyXpAwards);
        var nextEstimate = nextTier != null
            ? new WeeklyProgressNextEstimate
            {
                NeededDelta = nextTier.Threshold - weeklyXpAwards,
                SuggestedAction = GetSuggestedAction(nextTier.Threshold - weeklyXpAwards)
            }
            : null;

        return new WeeklyProgressData
        {
            Unit = "xp",
            CurrentValue = weeklyXpAwards,
            BreakEven = breakEven,
            Tiers = tiers,
            NextEstimate = nextEstimate,
            WeekStartDate = startOfWeek,
            WeekEndDate = endOfWeek.AddTicks(1),
            HasCelebration = weeklyXpAwards > gamification.TotalXp * 0.1, // Celebration if weekly XP > 10% of total
            CelebrationMessage = weeklyXpAwards > breakEven ? "Great work this week! 🎉" : null
        };
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    private static List<WeeklyProgressTier> CalculateWeeklyTiers(int currentLevel, int totalXp)
    {
        // Base tiers that scale with user level
        var baseMultiplier = Math.Max(1, currentLevel / 2);

        return new List<WeeklyProgressTier>
        {
            new() { Threshold = 10 * baseMultiplier, Label = "Bronze", Reward = new WeeklyProgressReward { Kind = "badge", Value = "Bronze Week" } },
            new() { Threshold = 25 * baseMultiplier, Label = "Silver", Reward = new WeeklyProgressReward { Kind = "multiplier", Value = "1.5×" } },
            new() { Threshold = 45 * baseMultiplier, Label = "Gold", Reward = new WeeklyProgressReward { Kind = "multiplier", Value = "2×" } },
            new() { Threshold = 70 * baseMultiplier, Label = "Platinum", Reward = new WeeklyProgressReward { Kind = "perk", Value = "PT shout-out" } },
            new() { Threshold = 100 * baseMultiplier, Label = "Diamond", Reward = new WeeklyProgressReward { Kind = "multiplier", Value = "2.5×" } }
        };
    }

    private static int CalculateBreakEvenXp(int currentLevel)
    {
        // Break-even is typically 60% of Gold tier
        var baseMultiplier = Math.Max(1, currentLevel / 2);
        return (int)(45 * baseMultiplier * 0.6);
    }

    private static string GetSuggestedAction(int neededXp)
    {
        return neededXp switch
        {
            <= 5 => "Complete one more exercise",
            <= 15 => "Log 2-3 more exercises",
            <= 30 => "Complete your daily routine",
            _ => "Focus on this week's exercises"
        };
    }
}

public class GamificationResult
{
    public int XpAwarded { get; set; }
    public List<Badge> NewBadges { get; set; } = new();
    public bool LeveledUp { get; set; }
    public int CurrentLevel { get; set; }
    public int TotalXp { get; set; }
    public int CurrentStreak { get; set; }
    public bool AlreadyAwarded { get; set; }
}

public class WeeklyProgressData
{
    public string Unit { get; set; } = "xp";
    public int CurrentValue { get; set; }
    public int BreakEven { get; set; }
    public List<WeeklyProgressTier> Tiers { get; set; } = new();
    public WeeklyProgressNextEstimate? NextEstimate { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public bool HasCelebration { get; set; }
    public string? CelebrationMessage { get; set; }
}

public class WeeklyProgressTier
{
    public int Threshold { get; set; }
    public string? Label { get; set; }
    public WeeklyProgressReward Reward { get; set; } = new();
}

public class WeeklyProgressReward
{
    public string Kind { get; set; } = "multiplier";
    public string Value { get; set; } = "";
}

public class WeeklyProgressNextEstimate
{
    public int NeededDelta { get; set; }
    public string SuggestedAction { get; set; } = "";
}