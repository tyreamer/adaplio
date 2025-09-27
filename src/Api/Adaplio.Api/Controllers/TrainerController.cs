using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Adaplio.Api.Controllers;

[ApiController]
[Route("api/trainer")]
[Authorize]
public class TrainerController : ControllerBase
{
    private readonly ILogger<TrainerController> _logger;

    public TrainerController(ILogger<TrainerController> logger)
    {
        _logger = logger;
    }

    [HttpGet("clients")]
    public async Task<IActionResult> GetClients()
    {
        try
        {
            var trainerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Getting clients for trainer {TrainerId}", trainerId);

            // Mock data for now - in real implementation, this would query the database
            var mockClients = GenerateMockClients();

            var response = new ClientsResponse
            {
                Clients = mockClients
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trainer clients");
            return StatusCode(500, "Failed to load clients");
        }
    }

    [HttpGet("clients/{clientAlias}/adherence")]
    public async Task<IActionResult> GetClientAdherence(string clientAlias)
    {
        try
        {
            _logger.LogInformation("Getting adherence for client {ClientAlias}", clientAlias);

            // Mock adherence data
            var response = new ClientAdherenceResponse
            {
                ClientAlias = clientAlias,
                CurrentWeekAdherence = 85.7m,
                OverallAdherence = 78.3m,
                RecentWeeks = GenerateMockWeeklyAdherence()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting client adherence for {ClientAlias}", clientAlias);
            return StatusCode(500, "Failed to load adherence data");
        }
    }

    [HttpGet("clients/{clientAlias}/gamification")]
    public async Task<IActionResult> GetClientGamification(string clientAlias)
    {
        try
        {
            _logger.LogInformation("Getting gamification data for client {ClientAlias}", clientAlias);

            // Mock gamification data
            var response = new TrainerClientGamificationResponse
            {
                ClientAlias = clientAlias,
                Level = 7,
                XpTotal = 2850,
                CurrentStreakDays = 12,
                TotalBadges = 8,
                RecentBadges = GenerateMockBadges()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting client gamification for {ClientAlias}", clientAlias);
            return StatusCode(500, "Failed to load gamification data");
        }
    }

    [HttpPost("grants")]
    public async Task<IActionResult> CreateGrant([FromBody] object request)
    {
        try
        {
            var trainerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Creating grant for trainer {TrainerId}", trainerId);

            // Generate mock grant
            var grantCode = GenerateGrantCode();
            var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

            var response = new GrantResponse
            {
                GrantCode = grantCode,
                Url = $"{Request.Scheme}://{Request.Host}/join?invite={grantCode}",
                ExpiresAt = expiresAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating grant");
            return StatusCode(500, "Failed to create invitation");
        }
    }

    private static ClientInfo[] GenerateMockClients()
    {
        var random = new Random();
        var clients = new List<ClientInfo>();

        var mockAliases = new[] { "C-7Q2F", "C-8N5K", "C-3M9L", "C-6P1R", "C-4X8D" };
        var scopes = new[] { "view_summary", "view_progress", "basic_info" };

        for (int i = 0; i < mockAliases.Length; i++)
        {
            var clientScopes = scopes.Take(random.Next(1, scopes.Length + 1)).ToArray();
            var lastActivity = random.Next(0, 10) == 0 ? (DateTimeOffset?)null :
                DateTimeOffset.Now.AddDays(-random.Next(0, 14));

            clients.Add(new ClientInfo
            {
                ClientAlias = mockAliases[i],
                Scopes = clientScopes,
                AdherencePct = (decimal)(random.NextDouble() * 40 + 60), // 60-100%
                LastActivity = lastActivity,
                GrantedAt = DateTimeOffset.Now.AddDays(-random.Next(1, 30))
            });
        }

        return clients.ToArray();
    }

    private static WeeklyAdherence[] GenerateMockWeeklyAdherence()
    {
        var weeks = new List<WeeklyAdherence>();
        var random = new Random();

        for (int i = 0; i < 8; i++) // Last 8 weeks
        {
            var weekStart = DateOnly.FromDateTime(DateTime.Today.AddDays(-i * 7));
            var planned = random.Next(12, 20);
            var completed = random.Next((int)(planned * 0.6), planned + 1);

            weeks.Add(new WeeklyAdherence
            {
                Year = weekStart.Year,
                WeekNumber = GetWeekOfYear(weekStart),
                WeekStartDate = weekStart,
                PlannedCount = planned,
                CompletedCount = completed,
                AdherencePercentage = planned > 0 ? (decimal)completed / planned * 100 : 0
            });
        }

        return weeks.OrderBy(w => w.WeekStartDate).ToArray();
    }

    private static BadgeDto[] GenerateMockBadges()
    {
        var badges = new[]
        {
            new BadgeDto
            {
                Id = "streak_7",
                Name = "Week Warrior",
                Description = "Complete exercises for 7 days straight",
                Icon = "🔥",
                Color = "#FF6B35",
                Rarity = "Common",
                EarnedAt = DateTimeOffset.Now.AddDays(-5)
            },
            new BadgeDto
            {
                Id = "consistent_month",
                Name = "Monthly Master",
                Description = "Maintain 80%+ adherence for a full month",
                Icon = "⭐",
                Color = "#4ECDC4",
                Rarity = "Rare",
                EarnedAt = DateTimeOffset.Now.AddDays(-12)
            },
            new BadgeDto
            {
                Id = "early_bird",
                Name = "Early Bird",
                Description = "Complete exercises before 8 AM",
                Icon = "🌅",
                Color = "#45B7D1",
                Rarity = "Uncommon",
                EarnedAt = DateTimeOffset.Now.AddDays(-18)
            }
        };

        return badges;
    }

    private static string GenerateGrantCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new char[8];

        for (int i = 0; i < code.Length; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }

        return new string(code);
    }

    private static int GetWeekOfYear(DateOnly date)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        return culture.Calendar.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue),
            culture.DateTimeFormat.CalendarWeekRule,
            culture.DateTimeFormat.FirstDayOfWeek);
    }
}

// Response DTOs
public class ClientsResponse
{
    public ClientInfo[] Clients { get; set; } = Array.Empty<ClientInfo>();
}

public class ClientInfo
{
    public string ClientAlias { get; set; } = "";
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public decimal AdherencePct { get; set; }
    public DateTimeOffset? LastActivity { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
}

public class ClientAdherenceResponse
{
    public string ClientAlias { get; set; } = "";
    public WeeklyAdherence[] RecentWeeks { get; set; } = Array.Empty<WeeklyAdherence>();
    public decimal CurrentWeekAdherence { get; set; }
    public decimal OverallAdherence { get; set; }
}

public class WeeklyAdherence
{
    public int Year { get; set; }
    public int WeekNumber { get; set; }
    public DateOnly WeekStartDate { get; set; }
    public decimal AdherencePercentage { get; set; }
    public int CompletedCount { get; set; }
    public int PlannedCount { get; set; }
}

public class TrainerClientGamificationResponse
{
    public string ClientAlias { get; set; } = "";
    public int Level { get; set; }
    public int XpTotal { get; set; }
    public int CurrentStreakDays { get; set; }
    public int TotalBadges { get; set; }
    public BadgeDto[] RecentBadges { get; set; } = Array.Empty<BadgeDto>();
}

public class BadgeDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "";
    public string Rarity { get; set; } = "";
    public DateTimeOffset EarnedAt { get; set; }
}

public class GrantResponse
{
    public string GrantCode { get; set; } = "";
    public string Url { get; set; } = "";
    public DateTimeOffset ExpiresAt { get; set; }
}