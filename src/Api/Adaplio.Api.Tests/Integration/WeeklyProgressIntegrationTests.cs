using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace Adaplio.Api.Tests.Integration;

public class WeeklyProgressIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WeeklyProgressIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real database context
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid());
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetWeeklyProgress_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync("/api/client/progress/week");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetWeeklyProgress_ShouldReturnCorrectData_WhenAuthenticated()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Create test data
        var clientProfile = new ClientProfile
        {
            Id = 1,
            UserId = 123,
            Alias = "testclient"
        };
        context.ClientProfiles.Add(clientProfile);

        var gamification = new Domain.Gamification
        {
            ClientProfileId = clientProfile.Id,
            TotalXp = 150,
            Level = 3
        };
        context.Gamifications.Add(gamification);

        // Add some XP awards for this week
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1);
        var xpAward = new XpAward
        {
            UserId = clientProfile.Id,
            XpAwarded = 35,
            CreatedAt = weekStart.AddDays(1)
        };
        context.XpAwards.Add(xpAward);

        await context.SaveChangesAsync();

        // Create authenticated client
        var authenticatedClient = CreateAuthenticatedClient("123", "client");

        // Act
        var response = await authenticatedClient.GetAsync("/api/client/progress/week");

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var jsonContent = await response.Content.ReadAsStringAsync();
        var weeklyProgress = JsonSerializer.Deserialize<WeeklyProgressResponse>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(weeklyProgress);
        Assert.Equal("xp", weeklyProgress.Unit);
        Assert.Equal(35, weeklyProgress.CurrentValue);
        Assert.True(weeklyProgress.BreakEven > 0);
        Assert.NotEmpty(weeklyProgress.Tiers);
        Assert.Contains(weeklyProgress.Tiers, t => t.Label == "Bronze");
    }

    [Fact]
    public async Task GetWeeklyProgress_ShouldScaleTiers_ForHighLevelUsers()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var clientProfile = new ClientProfile
        {
            Id = 1,
            UserId = 123,
            Alias = "highlevelclient"
        };
        context.ClientProfiles.Add(clientProfile);

        var gamification = new Domain.Gamification
        {
            ClientProfileId = clientProfile.Id,
            TotalXp = 2000,
            Level = 20 // High level user
        };
        context.Gamifications.Add(gamification);

        await context.SaveChangesAsync();

        var authenticatedClient = CreateAuthenticatedClient("123", "client");

        // Act
        var response = await authenticatedClient.GetAsync("/api/client/progress/week");

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var jsonContent = await response.Content.ReadAsStringAsync();
        var weeklyProgress = JsonSerializer.Deserialize<WeeklyProgressResponse>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(weeklyProgress);

        // For level 20, base multiplier should be 10, so Bronze tier should be 100 XP
        var bronzeTier = weeklyProgress.Tiers.First(t => t.Label == "Bronze");
        Assert.Equal(100, bronzeTier.Threshold); // 10 * 10 = 100
    }

    [Fact]
    public async Task GetWeeklyProgress_ShouldReturnForbidden_ForTrainerUser()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var trainerProfile = new TrainerProfile
        {
            Id = 1,
            UserId = 456,
            Alias = "testtrainer"
        };
        context.TrainerProfiles.Add(trainerProfile);
        await context.SaveChangesAsync();

        var authenticatedClient = CreateAuthenticatedClient("456", "trainer");

        // Act
        var response = await authenticatedClient.GetAsync("/api/client/progress/week");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetWeeklyProgress_ShouldIncludeNextEstimate_WhenNotAtMaxTier()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var clientProfile = new ClientProfile
        {
            Id = 1,
            UserId = 123,
            Alias = "testclient"
        };
        context.ClientProfiles.Add(clientProfile);

        var gamification = new Domain.Gamification
        {
            ClientProfileId = clientProfile.Id,
            TotalXp = 50,
            Level = 1
        };
        context.Gamifications.Add(gamification);

        // Add minimal XP for this week (below first tier)
        var weekStart = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1);
        var xpAward = new XpAward
        {
            UserId = clientProfile.Id,
            XpAwarded = 5,
            CreatedAt = weekStart.AddDays(1)
        };
        context.XpAwards.Add(xpAward);

        await context.SaveChangesAsync();

        var authenticatedClient = CreateAuthenticatedClient("123", "client");

        // Act
        var response = await authenticatedClient.GetAsync("/api/client/progress/week");

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var jsonContent = await response.Content.ReadAsStringAsync();
        var weeklyProgress = JsonSerializer.Deserialize<WeeklyProgressResponse>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(weeklyProgress);
        Assert.NotNull(weeklyProgress.NextEstimate);
        Assert.True(weeklyProgress.NextEstimate.NeededDelta > 0);
        Assert.NotEmpty(weeklyProgress.NextEstimate.SuggestedAction);
    }

    private HttpClient CreateAuthenticatedClient(string userId, string userType)
    {
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Add test authentication
                services.AddAuthentication("Test")
                    .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "Test", options =>
                        {
                            options.UserId = userId;
                            options.UserType = userType;
                        });
            });
        }).CreateClient();

        return client;
    }

    private class WeeklyProgressResponse
    {
        public string Unit { get; set; } = "";
        public int CurrentValue { get; set; }
        public int BreakEven { get; set; }
        public List<ProgressTierDto> Tiers { get; set; } = new();
        public NextEstimateDto? NextEstimate { get; set; }
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public bool HasCelebration { get; set; }
        public string? CelebrationMessage { get; set; }
    }

    private class ProgressTierDto
    {
        public int Threshold { get; set; }
        public string? Label { get; set; }
        public TierRewardDto Reward { get; set; } = new();
    }

    private class TierRewardDto
    {
        public string Kind { get; set; } = "";
        public string Value { get; set; } = "";
    }

    private class NextEstimateDto
    {
        public int NeededDelta { get; set; }
        public string SuggestedAction { get; set; } = "";
    }
}

// Test authentication handler for integration tests
public class TestAuthenticationHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(Microsoft.Extensions.Options.IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger, System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Options.UserId),
            new Claim("user_type", Options.UserType)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal, "Test");

        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(ticket));
    }
}

public class TestAuthenticationSchemeOptions : Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions
{
    public string UserId { get; set; } = "";
    public string UserType { get; set; } = "";
}