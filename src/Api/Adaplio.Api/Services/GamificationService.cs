using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Adaplio.Api.Services;

public interface IGamificationService
{
    Task<GamificationResult> AwardXpForProgressAsync(int progressEventId, int clientProfileId);
    Task<Domain.Gamification> GetOrCreateGamificationAsync(int clientProfileId);
    Task<Domain.Gamification?> GetGamificationAsync(int clientProfileId);
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
                TotalXp = existingGamification.XpTotal,
                CurrentStreak = existingGamification.CurrentStreakDays,
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
        gamification.XpTotal += xpAwarded;
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

        return new GamificationResult
        {
            XpAwarded = xpAwarded,
            NewBadges = newBadges,
            LeveledUp = gamification.Level > previousLevel,
            CurrentLevel = gamification.Level,
            TotalXp = gamification.XpTotal,
            CurrentStreak = gamification.CurrentStreakDays,
            AlreadyAwarded = false
        };
    }

    public async Task<Domain.Gamification> GetOrCreateGamificationAsync(int clientProfileId)
    {
        var gamification = await _context.Gamifications
            .FirstOrDefaultAsync(g => g.UserId == clientProfileId);

        if (gamification == null)
        {
            gamification = new Domain.Gamification
            {
                UserId = clientProfileId
            };
            _context.Gamifications.Add(gamification);
            await _context.SaveChangesAsync();
        }

        return gamification;
    }

    public async Task<Domain.Gamification?> GetGamificationAsync(int clientProfileId)
    {
        return await _context.Gamifications
            .FirstOrDefaultAsync(g => g.UserId == clientProfileId);
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
            gamification.CurrentStreakDays = 1;
            gamification.WeeklyStreakWeeks = IsStartOfWeek(eventDate) ? 1 : 0;
        }
        else
        {
            var lastActivity = gamification.LastActivityDate.Value;
            var daysDiff = eventDate.DayNumber - lastActivity.DayNumber;

            if (daysDiff == 1)
            {
                // Consecutive day
                gamification.CurrentStreakDays++;
            }
            else if (daysDiff == 0)
            {
                // Same day, no streak change
                return;
            }
            else
            {
                // Streak broken
                gamification.CurrentStreakDays = 1;
            }

            // Update weekly streak
            if (IsStartOfWeek(eventDate) && gamification.CurrentStreakDays >= 7)
            {
                gamification.WeeklyStreakWeeks++;
            }
        }

        // Update longest streaks
        if (gamification.CurrentStreakDays > gamification.LongestStreakDays)
        {
            gamification.LongestStreakDays = gamification.CurrentStreakDays;
        }

        if (gamification.WeeklyStreakWeeks > gamification.LongestWeeklyStreak)
        {
            gamification.LongestWeeklyStreak = gamification.WeeklyStreakWeeks;
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
        if (gamification.XpTotal >= 10 && !existingBadgeIds.Contains("first_steps"))
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
        if (gamification.CurrentStreakDays >= 3 && !existingBadgeIds.Contains("streak_3"))
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

        if (gamification.CurrentStreakDays >= 7 && !existingBadgeIds.Contains("streak_7"))
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

        if (gamification.CurrentStreakDays >= 30 && !existingBadgeIds.Contains("streak_30"))
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