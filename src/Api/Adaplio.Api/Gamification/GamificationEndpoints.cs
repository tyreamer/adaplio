using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Adaplio.Api.Gamification;

public static class GamificationEndpoints
{
    public static void MapGamificationEndpoints(this WebApplication app)
    {
        var gamificationGroup = app.MapGroup("/api").WithTags("Gamification");

        // Client endpoint - get own gamification data
        gamificationGroup.MapGet("/client/gamification", GetClientGamification)
            .RequireAuthorization()
            .WithName("GetClientGamification");

        // Trainer endpoint - get client gamification summary (requires view_summary consent)
        gamificationGroup.MapGet("/trainer/clients/{clientAlias}/gamification", GetTrainerClientGamification)
            .RequireAuthorization()
            .WithName("GetTrainerClientGamification");
    }

    private static async Task<IResult> GetClientGamification(
        AppDbContext context,
        IGamificationService gamificationService,
        HttpContext httpContext)
    {
        try
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = httpContext.User.FindFirst("user_type")?.Value;

            if (string.IsNullOrEmpty(userId) || userType != "client")
            {
                return Results.Forbid();
            }

            if (!int.TryParse(userId, out var parsedUserId))
            {
                return Results.Forbid();
            }

            // Get client profile
            var clientProfile = await context.ClientProfiles
                .FirstOrDefaultAsync(cp => cp.UserId == parsedUserId);

            if (clientProfile == null)
            {
                return Results.NotFound("Client profile not found");
            }

            // Get gamification data
            var gamification = await gamificationService.GetGamificationAsync(clientProfile.Id);

            if (gamification == null)
            {
                // Return default values for new users
                return Results.Ok(new ClientGamificationResponse(
                    clientProfile.Alias ?? "Unknown",
                    0,
                    1,
                    10,
                    0.0,
                    0,
                    0,
                    0,
                    0,
                    Array.Empty<BadgeDto>()
                ));
            }

            // Convert badges to DTOs
            var badgeDtos = gamification.Badges
                .OrderByDescending(b => b.EarnedAt)
                .Select(b => new BadgeDto(b.Id, b.Name, b.Description, b.Icon, b.Color, b.Rarity, b.EarnedAt))
                .ToArray();

            return Results.Ok(new ClientGamificationResponse(
                clientProfile.Alias ?? "Unknown",
                gamification.XpTotal,
                gamification.Level,
                gamification.XpForNextLevel,
                gamification.LevelProgress,
                gamification.CurrentStreakDays,
                gamification.LongestStreakDays,
                gamification.WeeklyStreakWeeks,
                gamification.LongestWeeklyStreak,
                badgeDtos
            ));
        }
        catch (Exception)
        {
            return Results.Problem("Failed to retrieve gamification data. Please try again.");
        }
    }

    private static async Task<IResult> GetTrainerClientGamification(
        string clientAlias,
        AppDbContext context,
        IGamificationService gamificationService,
        HttpContext httpContext)
    {
        try
        {
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = httpContext.User.FindFirst("user_type")?.Value;

            if (string.IsNullOrEmpty(userId) || userType != "trainer")
            {
                return Results.Forbid();
            }

            if (!int.TryParse(userId, out var parsedUserId))
            {
                return Results.Forbid();
            }

            // Get trainer profile
            var trainerProfile = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == parsedUserId);

            if (trainerProfile == null)
            {
                return Results.NotFound("Trainer profile not found");
            }

            // Find client by alias
            var clientProfile = await context.ClientProfiles
                .FirstOrDefaultAsync(cp => cp.Alias == clientAlias);

            if (clientProfile == null)
            {
                return Results.NotFound("Client not found");
            }

            // Check if trainer has view_summary consent for this client
            var hasConsent = await context.ConsentGrants
                .AnyAsync(cg => cg.ClientProfileId == clientProfile.Id &&
                               cg.TrainerProfileId == trainerProfile.Id &&
                               cg.Scope == "view_summary" &&
                               cg.RevokedAt == null);

            if (!hasConsent)
            {
                return Results.Problem("No consent granted for viewing client gamification data", statusCode: 403);
            }

            // Get gamification data
            var gamification = await gamificationService.GetGamificationAsync(clientProfile.Id);

            if (gamification == null)
            {
                // Return default values for new users
                return Results.Ok(new TrainerClientGamificationResponse(
                    clientAlias,
                    1,
                    0,
                    0,
                    0,
                    Array.Empty<BadgeDto>()
                ));
            }

            // Get recent badges (last 3)
            var recentBadges = gamification.Badges
                .OrderByDescending(b => b.EarnedAt)
                .Take(3)
                .Select(b => new BadgeDto(b.Id, b.Name, b.Description, b.Icon, b.Color, b.Rarity, b.EarnedAt))
                .ToArray();

            return Results.Ok(new TrainerClientGamificationResponse(
                clientAlias,
                gamification.Level,
                gamification.XpTotal,
                gamification.CurrentStreakDays,
                gamification.Badges.Count,
                recentBadges
            ));
        }
        catch (Exception)
        {
            return Results.Problem("Failed to retrieve client gamification data. Please try again.");
        }
    }
}