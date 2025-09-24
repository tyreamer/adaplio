using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace Adaplio.Api.Progress;

public static class ProgressEndpoints
{
    public static void MapProgressEndpoints(this WebApplication app)
    {
        var progressGroup = app.MapGroup("/api").WithTags("Progress & Adherence");

        // Client endpoints
        progressGroup.MapPost("/client/progress", LogProgress)
            .RequireAuthorization()
            .WithName("LogProgress");

        progressGroup.MapGet("/client/progress/summary", GetClientAdherenceSummary)
            .RequireAuthorization()
            .WithName("GetClientAdherenceSummary");

        // Trainer endpoints
        progressGroup.MapGet("/trainer/clients/{clientAlias}/adherence", GetTrainerClientAdherence)
            .RequireAuthorization()
            .WithName("GetTrainerClientAdherence");
    }

    private static async Task<IResult> LogProgress(
        LogProgressRequest request,
        AppDbContext context,
        IProgressService progressService,
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

            // Get client profile
            var clientProfile = await context.ClientProfiles
                .FirstOrDefaultAsync(cp => cp.UserId == int.Parse(userId));

            if (clientProfile == null)
            {
                return Results.NotFound("Client profile not found");
            }

            // Verify exercise instance belongs to this client
            var exerciseInstance = await context.ExerciseInstances
                .Include(ei => ei.PlanInstance)
                .FirstOrDefaultAsync(ei => ei.Id == request.ExerciseInstanceId &&
                                          ei.PlanInstance.ClientProfileId == clientProfile.Id);

            if (exerciseInstance == null)
            {
                return Results.NotFound("Exercise instance not found or not accessible");
            }

            // Validate event type
            var validEventTypes = new[] { "exercise_completed", "set_completed", "session_completed" };
            if (!validEventTypes.Contains(request.EventType))
            {
                return Results.BadRequest("EventType must be 'exercise_completed', 'set_completed', or 'session_completed'");
            }

            // Create progress event
            var progressEvent = new ProgressEvent
            {
                ExerciseInstanceId = request.ExerciseInstanceId,
                ClientProfileId = clientProfile.Id,
                EventType = request.EventType,
                SetsCompleted = request.SetsCompleted,
                RepsCompleted = request.RepsCompleted,
                HoldSecondsCompleted = request.HoldSecondsCompleted,
                DifficultyRating = request.DifficultyRating,
                PainLevel = request.PainLevel,
                Notes = request.Notes,
                LoggedAt = DateTimeOffset.UtcNow
            };

            context.ProgressEvents.Add(progressEvent);
            await context.SaveChangesAsync();

            // Update adherence for the week this exercise belongs to
            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            var weekStart = GetWeekStart(currentDate);
            await progressService.UpdateAdherenceWeekAsync(clientProfile.Id, weekStart);

            return Results.Ok(new LogProgressResponse(
                "Progress logged successfully",
                progressEvent.Id
            ));
        }
        catch (Exception)
        {
            return Results.Problem("Failed to log progress. Please try again.");
        }
    }

    private static async Task<IResult> GetClientAdherenceSummary(
        AppDbContext context,
        IProgressService progressService,
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

            // Get client profile
            var clientProfile = await context.ClientProfiles
                .FirstOrDefaultAsync(cp => cp.UserId == int.Parse(userId));

            if (clientProfile == null)
            {
                return Results.NotFound("Client profile not found");
            }

            // Get adherence data for last 12 weeks
            var weeklyData = await progressService.GetClientAdherenceAsync(clientProfile.Id, 12);
            var overallAdherence = await progressService.CalculateOverallAdherenceAsync(clientProfile.Id);

            return Results.Ok(new ClientAdherenceSummaryResponse(
                clientProfile.Alias ?? "Unknown",
                weeklyData,
                overallAdherence
            ));
        }
        catch (Exception)
        {
            return Results.Problem("Failed to retrieve adherence summary. Please try again.");
        }
    }

    private static async Task<IResult> GetTrainerClientAdherence(
        string clientAlias,
        AppDbContext context,
        IProgressService progressService,
        IAliasService aliasService,
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

            // Get trainer profile
            var trainerProfile = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == int.Parse(userId));

            if (trainerProfile == null)
            {
                return Results.NotFound("Trainer profile not found");
            }

            // Find client by alias and verify consent
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
                return Results.Problem("No consent granted for viewing client adherence", statusCode: 403);
            }

            // Get adherence data for last 8 weeks
            var recentWeeks = await progressService.GetClientAdherenceAsync(clientProfile.Id, 8);
            var overallAdherence = await progressService.CalculateOverallAdherenceAsync(clientProfile.Id);

            // Get current week adherence
            var currentWeekStart = GetWeekStart(DateOnly.FromDateTime(DateTime.Now));
            var currentWeekData = recentWeeks.FirstOrDefault(w => w.WeekStartDate == currentWeekStart);
            var currentWeekAdherence = currentWeekData?.AdherencePercentage ?? 0;

            return Results.Ok(new TrainerClientAdherenceResponse(
                clientAlias,
                recentWeeks,
                currentWeekAdherence,
                overallAdherence
            ));
        }
        catch (Exception)
        {
            return Results.Problem("Failed to retrieve client adherence. Please try again.");
        }
    }

    private static DateOnly GetWeekStart(DateOnly date)
    {
        var daysToSubtract = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
        if (daysToSubtract < 0)
            daysToSubtract += 7;
        return date.AddDays(-daysToSubtract);
    }
}