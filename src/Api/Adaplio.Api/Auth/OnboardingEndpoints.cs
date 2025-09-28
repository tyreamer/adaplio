using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Adaplio.Api.Auth;

public static class OnboardingEndpoints
{
    public static void MapOnboardingEndpoints(this WebApplication app)
    {
        var onboardingGroup = app.MapGroup("/api/client").WithTags("Client Onboarding");

        // Save onboarding preferences
        onboardingGroup.MapPost("/onboarding", SaveOnboardingPreferences)
            .RequireAuthorization()
            .WithName("SaveOnboardingPreferences");

        // Notify PT that client is ready
        onboardingGroup.MapPost("/ready", NotifyPTReady)
            .RequireAuthorization()
            .WithName("NotifyPTReady");

        // Get client's weekly board
        onboardingGroup.MapGet("/weekly-board", GetWeeklyBoard)
            .RequireAuthorization()
            .WithName("GetWeeklyBoard");
    }

    private static async Task<IResult> SaveOnboardingPreferences(
        OnboardingPreferencesRequest request,
        AppDbContext context,
        HttpContext httpContext)
    {
        try
        {
            var userIdClaim = httpContext.User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await context.AppUsers
                .Include(u => u.ClientProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.ClientProfile == null)
            {
                return Results.BadRequest("Client profile not found");
            }

            // Update client profile with onboarding preferences
            var profile = user.ClientProfile;

            // Save preferences (extend ClientProfile model as needed)
            profile.UpdatedAt = DateTimeOffset.UtcNow;

            // For now, store in a simple way - you might want to add specific fields to ClientProfile
            // profile.NotificationsEnabled = request.EnableNotifications;
            // profile.ReminderTime = request.ReminderTime;
            // profile.InjuryGoal = request.InjuryGoal;
            // profile.AffectedSide = request.AffectedSide;

            await context.SaveChangesAsync();

            return Results.Ok(new { message = "Preferences saved successfully" });
        }
        catch (Exception)
        {
            return Results.Problem("Failed to save onboarding preferences");
        }
    }

    private static async Task<IResult> NotifyPTReady(
        AppDbContext context,
        HttpContext httpContext)
    {
        try
        {
            var userIdClaim = httpContext.User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await context.AppUsers
                .Include(u => u.ClientProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user?.ClientProfile == null)
            {
                return Results.BadRequest("Client profile not found");
            }

            // For now, just log that client is ready
            // In a real implementation, you might:
            // - Create a notification record
            // - Send an email to connected trainers
            // - Update client status

            var profile = user.ClientProfile;
            profile.UpdatedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync();

            return Results.Ok(new { message = "Your therapist has been notified that you're ready!" });
        }
        catch (Exception)
        {
            return Results.Problem("Failed to notify therapist");
        }
    }

    private static async Task<IResult> GetWeeklyBoard(
        AppDbContext context,
        HttpContext httpContext)
    {
        try
        {
            var userIdClaim = httpContext.User.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            // For now, return a simple response
            // In a real implementation, this would fetch actual missions/exercises
            var weeklyBoard = new WeeklyBoardResponse(
                HasPlan: false, // Placeholder - check if client has active plan
                Missions: new[]
                {
                    new MissionDto("Welcome", "Get started with your PT plan", false, "beginner"),
                    new MissionDto("Setup Complete", "Complete your profile setup", false, "easy")
                },
                Message: "You don't have any assigned exercises yet. Once you do, they’ll appear here."
            );

            return Results.Ok(weeklyBoard);
        }
        catch (Exception)
        {
            return Results.Problem("Failed to get weekly board");
        }
    }
}

// DTOs
public record OnboardingPreferencesRequest(
    bool ShareSummary = true,
    bool EnableNotifications = true,
    string? ReminderTime = null,
    string? InjuryGoal = null,
    string? AffectedSide = null
);

public record WeeklyBoardResponse(
    bool HasPlan,
    MissionDto[] Missions,
    string? Message = null
);

public record MissionDto(
    string Name,
    string Description,
    bool IsCompleted,
    string Difficulty
);