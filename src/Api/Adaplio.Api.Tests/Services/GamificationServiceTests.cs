using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Adaplio.Api.Tests.Services;

public class GamificationServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GamificationService _gamificationService;

    public GamificationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _gamificationService = new GamificationService(_context);
    }

    [Fact]
    public async Task AwardXpForProgressAsync_ShouldCreateNewGamificationRecord_WhenNotExists()
    {
        // Arrange
        var clientProfileId = 1;
        var progressEvent = new ProgressEvent
        {
            Id = 1,
            ClientProfileId = clientProfileId,
            EventType = "exercise_completed",
            LoggedAt = DateTimeOffset.UtcNow
        };

        _context.ProgressEvents.Add(progressEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gamificationService.AwardXpForProgressAsync(progressEvent.Id, clientProfileId);

        // Assert
        Assert.False(result.AlreadyAwarded);
        Assert.Equal(25, result.XpAwarded); // exercise_completed = 25 XP
        Assert.Equal(1, result.CurrentLevel);
        Assert.Equal(25, result.TotalXp);

        var gamification = await _context.Gamifications.FirstOrDefaultAsync(g => g.ClientProfileId == clientProfileId);
        Assert.NotNull(gamification);
        Assert.Equal(25, gamification.TotalXp);
    }

    [Fact]
    public async Task AwardXpForProgressAsync_ShouldBeIdempotent_WhenCalledMultipleTimes()
    {
        // Arrange
        var clientProfileId = 1;
        var progressEvent = new ProgressEvent
        {
            Id = 1,
            ClientProfileId = clientProfileId,
            EventType = "set_completed",
            LoggedAt = DateTimeOffset.UtcNow
        };

        _context.ProgressEvents.Add(progressEvent);
        await _context.SaveChangesAsync();

        // Act
        var firstResult = await _gamificationService.AwardXpForProgressAsync(progressEvent.Id, clientProfileId);
        var secondResult = await _gamificationService.AwardXpForProgressAsync(progressEvent.Id, clientProfileId);

        // Assert
        Assert.False(firstResult.AlreadyAwarded);
        Assert.Equal(10, firstResult.XpAwarded);

        Assert.True(secondResult.AlreadyAwarded);
        Assert.Equal(0, secondResult.XpAwarded);

        // Verify only one XP award record exists
        var xpAwards = await _context.XpAwards.Where(xa => xa.ProgressEventId == progressEvent.Id).ToListAsync();
        Assert.Single(xpAwards);
    }

    [Theory]
    [InlineData("set_completed", 10)]
    [InlineData("exercise_completed", 25)]
    [InlineData("session_completed", 50)]
    [InlineData("unknown_type", 5)]
    public async Task AwardXpForProgressAsync_ShouldAwardCorrectXp_BasedOnEventType(string eventType, int expectedXp)
    {
        // Arrange
        var clientProfileId = 1;
        var progressEvent = new ProgressEvent
        {
            Id = 1,
            ClientProfileId = clientProfileId,
            EventType = eventType,
            LoggedAt = DateTimeOffset.UtcNow
        };

        _context.ProgressEvents.Add(progressEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gamificationService.AwardXpForProgressAsync(progressEvent.Id, clientProfileId);

        // Assert
        Assert.Equal(expectedXp, result.XpAwarded);
    }

    [Fact]
    public async Task AwardXpForProgressAsync_ShouldAwardFirstStepsBadge_WhenReaching10Xp()
    {
        // Arrange
        var clientProfileId = 1;
        var progressEvent = new ProgressEvent
        {
            Id = 1,
            ClientProfileId = clientProfileId,
            EventType = "exercise_completed", // 25 XP
            LoggedAt = DateTimeOffset.UtcNow
        };

        _context.ProgressEvents.Add(progressEvent);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gamificationService.AwardXpForProgressAsync(progressEvent.Id, clientProfileId);

        // Assert
        Assert.True(result.NewBadges.Any(b => b.Id == "first_steps"));
        var firstStepsBadge = result.NewBadges.First(b => b.Id == "first_steps");
        Assert.Equal("First Steps", firstStepsBadge.Name);
        Assert.Equal("Completed your first exercise", firstStepsBadge.Description);
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ShouldReturnCorrectData_ForCurrentWeek()
    {
        // Arrange
        var clientProfileId = 1;
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1); // Monday

        // Create gamification record
        var gamification = new Domain.Gamification
        {
            ClientProfileId = clientProfileId,
            TotalXp = 100,
            Level = 2
        };
        _context.Gamifications.Add(gamification);

        // Create XP awards for this week
        var xpAward1 = new XpAward
        {
            UserId = clientProfileId,
            XpAwarded = 15,
            CreatedAt = weekStart.AddDays(1)
        };
        var xpAward2 = new XpAward
        {
            UserId = clientProfileId,
            XpAwarded = 20,
            CreatedAt = weekStart.AddDays(2)
        };

        _context.XpAwards.AddRange(xpAward1, xpAward2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gamificationService.GetWeeklyProgressAsync(clientProfileId, weekStart);

        // Assert
        Assert.Equal("xp", result.Unit);
        Assert.Equal(35, result.CurrentValue); // 15 + 20
        Assert.True(result.BreakEven > 0);
        Assert.NotEmpty(result.Tiers);
        Assert.Equal("Bronze", result.Tiers.First().Label);
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ShouldScaleTiers_BasedOnUserLevel()
    {
        // Arrange
        var clientProfileId = 1;

        // Create high-level gamification record
        var gamification = new Domain.Gamification
        {
            ClientProfileId = clientProfileId,
            TotalXp = 1000,
            Level = 10
        };
        _context.Gamifications.Add(gamification);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gamificationService.GetWeeklyProgressAsync(clientProfileId);

        // Assert
        var baseMultiplier = Math.Max(1, gamification.Level / 2); // Should be 5 for level 10
        var expectedBronzeThreshold = 10 * baseMultiplier; // 50

        Assert.Equal(expectedBronzeThreshold, result.Tiers.First().Threshold);
        Assert.True(result.BreakEven > 20); // Should be higher for high-level users
    }

    [Fact]
    public async Task GetWeeklyProgressAsync_ShouldIncludeNextEstimate_WhenNotAtMaxTier()
    {
        // Arrange
        var clientProfileId = 1;
        var gamification = new Domain.Gamification
        {
            ClientProfileId = clientProfileId,
            TotalXp = 50,
            Level = 1
        };
        _context.Gamifications.Add(gamification);

        // Add some XP for this week (less than first tier)
        var xpAward = new XpAward
        {
            UserId = clientProfileId,
            XpAwarded = 5,
            CreatedAt = DateTime.UtcNow
        };
        _context.XpAwards.Add(xpAward);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gamificationService.GetWeeklyProgressAsync(clientProfileId);

        // Assert
        Assert.NotNull(result.NextEstimate);
        Assert.True(result.NextEstimate.NeededDelta > 0);
        Assert.NotEmpty(result.NextEstimate.SuggestedAction);
    }

    [Fact]
    public async Task GetOrCreateGamificationAsync_ShouldCreateNew_WhenNotExists()
    {
        // Arrange
        var clientProfileId = 1;

        // Act
        var result = await _gamificationService.GetOrCreateGamificationAsync(clientProfileId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(clientProfileId, result.ClientProfileId);
        Assert.Equal(1, result.Level);
        Assert.Equal(0, result.TotalXp);

        // Verify it was saved to database
        var saved = await _context.Gamifications.FirstOrDefaultAsync(g => g.ClientProfileId == clientProfileId);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task GetOrCreateGamificationAsync_ShouldReturnExisting_WhenExists()
    {
        // Arrange
        var clientProfileId = 1;
        var existing = new Domain.Gamification
        {
            ClientProfileId = clientProfileId,
            TotalXp = 100,
            Level = 5
        };
        _context.Gamifications.Add(existing);
        await _context.SaveChangesAsync();

        // Act
        var result = await _gamificationService.GetOrCreateGamificationAsync(clientProfileId);

        // Assert
        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(100, result.TotalXp);
        Assert.Equal(5, result.Level);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}