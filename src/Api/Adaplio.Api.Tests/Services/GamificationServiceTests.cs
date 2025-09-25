using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Adaplio.Api.Tests.Services;

public class GamificationServiceTests
{
    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AwardXpForProgressAsync_FirstTime_AwardsXpAndCreatesRecord()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new GamificationService(context);

        var progressEvent = new ProgressEvent
        {
            Id = 1,
            EventType = "exercise_completed",
            ClientProfileId = 1,
            LoggedAt = DateTimeOffset.UtcNow
        };
        context.ProgressEvents.Add(progressEvent);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AwardXpForProgressAsync(1, 1);

        // Assert
        Assert.Equal(25, result.XpAwarded); // exercise_completed = 25 XP
        Assert.False(result.AlreadyAwarded);
        Assert.Equal(1, result.CurrentLevel); // Starting level
        Assert.Equal(25, result.TotalXp);

        // Verify XP award record exists
        var xpAward = await context.XpAwards.FirstOrDefaultAsync(xa => xa.ProgressEventId == 1);
        Assert.NotNull(xpAward);
        Assert.Equal(25, xpAward.XpAwarded);
    }

    [Fact]
    public async Task AwardXpForProgressAsync_DuplicateCall_ReturnsExistingResult()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new GamificationService(context);

        var progressEvent = new ProgressEvent
        {
            Id = 1,
            EventType = "exercise_completed",
            ClientProfileId = 1,
            LoggedAt = DateTimeOffset.UtcNow
        };
        context.ProgressEvents.Add(progressEvent);
        await context.SaveChangesAsync();

        // Award XP first time
        await service.AwardXpForProgressAsync(1, 1);

        // Act - Award XP second time (should be idempotent)
        var result = await service.AwardXpForProgressAsync(1, 1);

        // Assert
        Assert.Equal(0, result.XpAwarded); // No additional XP awarded
        Assert.True(result.AlreadyAwarded);

        // Verify only one XP award record exists
        var xpAwards = await context.XpAwards.Where(xa => xa.ProgressEventId == 1).ToListAsync();
        Assert.Single(xpAwards);
    }

    [Fact]
    public async Task AwardXpForProgressAsync_DifferentEventTypes_AwardsDifferentXp()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new GamificationService(context);

        var events = new[]
        {
            new ProgressEvent { Id = 1, EventType = "set_completed", ClientProfileId = 1, LoggedAt = DateTimeOffset.UtcNow },
            new ProgressEvent { Id = 2, EventType = "exercise_completed", ClientProfileId = 1, LoggedAt = DateTimeOffset.UtcNow },
            new ProgressEvent { Id = 3, EventType = "session_completed", ClientProfileId = 1, LoggedAt = DateTimeOffset.UtcNow }
        };

        context.ProgressEvents.AddRange(events);
        await context.SaveChangesAsync();

        // Act & Assert
        var setResult = await service.AwardXpForProgressAsync(1, 1);
        Assert.Equal(10, setResult.XpAwarded); // set_completed = 10 XP

        var exerciseResult = await service.AwardXpForProgressAsync(2, 1);
        Assert.Equal(25, exerciseResult.XpAwarded); // exercise_completed = 25 XP

        var sessionResult = await service.AwardXpForProgressAsync(3, 1);
        Assert.Equal(50, sessionResult.XpAwarded); // session_completed = 50 XP
    }

    [Fact]
    public async Task AwardXpForProgressAsync_ChecksForLevelUp()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new GamificationService(context);

        // Create a gamification record with existing XP near level up
        var gamification = new Domain.Gamification
        {
            UserId = 1,
            XpTotal = 90 // Level formula: 1 + floor(sqrt(xp/10)) = 1 + floor(sqrt(9)) = 4
        };
        context.Gamifications.Add(gamification);

        var progressEvent = new ProgressEvent
        {
            Id = 1,
            EventType = "exercise_completed", // +25 XP
            ClientProfileId = 1,
            LoggedAt = DateTimeOffset.UtcNow
        };
        context.ProgressEvents.Add(progressEvent);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AwardXpForProgressAsync(1, 1);

        // Assert
        Assert.Equal(25, result.XpAwarded);
        Assert.True(result.LeveledUp);
        Assert.Equal(4, result.CurrentLevel); // New level after gaining XP
        Assert.Equal(115, result.TotalXp); // 90 + 25
    }

    [Fact]
    public async Task AwardXpForProgressAsync_ConsecutiveDays_IncreasesStreak()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new GamificationService(context);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        // Create gamification with yesterday's activity
        var gamification = new Domain.Gamification
        {
            UserId = 1,
            XpTotal = 25,
            CurrentStreakDays = 1,
            LongestStreakDays = 1,
            LastActivityDate = yesterday
        };
        context.Gamifications.Add(gamification);

        var progressEvent = new ProgressEvent
        {
            Id = 1,
            EventType = "exercise_completed",
            ClientProfileId = 1,
            LoggedAt = DateTimeOffset.UtcNow
        };
        context.ProgressEvents.Add(progressEvent);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AwardXpForProgressAsync(1, 1);

        // Assert
        Assert.Equal(2, result.CurrentStreak); // Streak should increment

        // Verify gamification record updated
        var updatedGamification = await context.Gamifications.FirstAsync(g => g.UserId == 1);
        Assert.Equal(2, updatedGamification.CurrentStreakDays);
        Assert.Equal(2, updatedGamification.LongestStreakDays);
        Assert.Equal(today, updatedGamification.LastActivityDate);
    }

    [Fact]
    public async Task AwardXpForProgressAsync_BrokenStreak_ResetsToOne()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new GamificationService(context);

        var threeDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3));

        // Create gamification with old activity (broken streak)
        var gamification = new Domain.Gamification
        {
            UserId = 1,
            XpTotal = 25,
            CurrentStreakDays = 5,
            LongestStreakDays = 10,
            LastActivityDate = threeDaysAgo
        };
        context.Gamifications.Add(gamification);

        var progressEvent = new ProgressEvent
        {
            Id = 1,
            EventType = "exercise_completed",
            ClientProfileId = 1,
            LoggedAt = DateTimeOffset.UtcNow
        };
        context.ProgressEvents.Add(progressEvent);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AwardXpForProgressAsync(1, 1);

        // Assert
        Assert.Equal(1, result.CurrentStreak); // Streak should reset to 1

        // Verify gamification record updated
        var updatedGamification = await context.Gamifications.FirstAsync(g => g.UserId == 1);
        Assert.Equal(1, updatedGamification.CurrentStreakDays);
        Assert.Equal(10, updatedGamification.LongestStreakDays); // Longest streak preserved
    }

    [Fact]
    public async Task AwardXpForProgressAsync_EarnsFirstStepsBadge()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new GamificationService(context);

        var progressEvent = new ProgressEvent
        {
            Id = 1,
            EventType = "exercise_completed", // 25 XP, enough for first steps badge (>= 10 XP)
            ClientProfileId = 1,
            LoggedAt = DateTimeOffset.UtcNow
        };
        context.ProgressEvents.Add(progressEvent);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AwardXpForProgressAsync(1, 1);

        // Assert
        Assert.Single(result.NewBadges);
        var badge = result.NewBadges.First();
        Assert.Equal("first_steps", badge.Id);
        Assert.Equal("First Steps", badge.Name);
        Assert.Equal("common", badge.Rarity);

        // Verify badge is persisted
        var gamification = await context.Gamifications.FirstAsync(g => g.UserId == 1);
        Assert.Single(gamification.Badges);
        Assert.Equal("first_steps", gamification.Badges.First().Id);
    }

    [Fact]
    public async Task AwardXpForProgressAsync_EarnsStreakBadge()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new GamificationService(context);

        // Create gamification with 2-day streak (one more day will trigger 3-day streak badge)
        var gamification = new Domain.Gamification
        {
            UserId = 1,
            XpTotal = 50,
            CurrentStreakDays = 2,
            LongestStreakDays = 2,
            LastActivityDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
        };
        context.Gamifications.Add(gamification);

        var progressEvent = new ProgressEvent
        {
            Id = 1,
            EventType = "exercise_completed",
            ClientProfileId = 1,
            LoggedAt = DateTimeOffset.UtcNow
        };
        context.ProgressEvents.Add(progressEvent);
        await context.SaveChangesAsync();

        // Act
        var result = await service.AwardXpForProgressAsync(1, 1);

        // Assert
        var streakBadge = result.NewBadges.FirstOrDefault(b => b.Id == "streak_3");
        Assert.NotNull(streakBadge);
        Assert.Equal("On a Roll", streakBadge.Name);
        Assert.Equal("3 days in a row", streakBadge.Description);
    }

    [Fact]
    public async Task GetOrCreateGamificationAsync_CreatesNewRecord_WhenNotExists()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new GamificationService(context);

        // Act
        var result = await service.GetOrCreateGamificationAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.Equal(0, result.XpTotal);
        Assert.Equal(1, result.Level); // Level formula with 0 XP = 1 + floor(sqrt(0/10)) = 1

        // Verify record was saved to database
        var dbRecord = await context.Gamifications.FirstOrDefaultAsync(g => g.UserId == 1);
        Assert.NotNull(dbRecord);
    }

    [Fact]
    public async Task GetOrCreateGamificationAsync_ReturnsExisting_WhenExists()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var service = new GamificationService(context);

        var existingGamification = new Domain.Gamification
        {
            UserId = 1,
            XpTotal = 100,
            CurrentStreakDays = 5
        };
        context.Gamifications.Add(existingGamification);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetOrCreateGamificationAsync(1);

        // Assert
        Assert.Equal(existingGamification.Id, result.Id);
        Assert.Equal(100, result.XpTotal);
        Assert.Equal(5, result.CurrentStreakDays);
    }
}