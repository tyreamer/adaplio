using Adaplio.Api.Data;
using Adaplio.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Adaplio.Api.Plans;

public static class PlanEndpoints
{
    public static void MapPlanEndpoints(this WebApplication app)
    {
        var planGroup = app.MapGroup("/api").WithTags("Plans & Templates");

        // Template endpoints (trainer)
        planGroup.MapPost("/trainer/templates", CreateTemplate)
            .RequireAuthorization()
            .WithName("CreateTemplate");

        planGroup.MapGet("/trainer/templates", GetTrainerTemplates)
            .RequireAuthorization()
            .WithName("GetTrainerTemplates");

        planGroup.MapPut("/trainer/templates/{id}", UpdateTemplate)
            .RequireAuthorization()
            .WithName("UpdateTemplate");

        planGroup.MapDelete("/trainer/templates/{id}", DeleteTemplate)
            .RequireAuthorization()
            .WithName("DeleteTemplate");

        // Proposal endpoints (trainer → client)
        planGroup.MapPost("/trainer/proposals", CreateProposal)
            .RequireAuthorization()
            .WithName("CreateProposal");

        planGroup.MapGet("/trainer/proposals", GetTrainerProposals)
            .RequireAuthorization()
            .WithName("GetTrainerProposals");

        planGroup.MapGet("/trainer/clients", GetTrainerClients)
            .RequireAuthorization()
            .WithName("GetTrainerClients");

        planGroup.MapGet("/client/proposals", GetClientProposals)
            .RequireAuthorization()
            .WithName("GetClientProposals");

        planGroup.MapGet("/client/proposals/{id}", GetClientProposal)
            .RequireAuthorization()
            .WithName("GetClientProposal");

        // Acceptance endpoints (client)
        planGroup.MapPost("/client/proposals/{id}/accept", AcceptProposal)
            .RequireAuthorization()
            .WithName("AcceptProposal");

        planGroup.MapGet("/client/plans", GetClientPlans)
            .RequireAuthorization()
            .WithName("GetClientPlans");

        // Board endpoints (client)
        planGroup.MapGet("/client/board", GetClientBoard)
            .RequireAuthorization()
            .WithName("GetClientBoard");

        planGroup.MapPost("/client/board/quick-log", QuickLogProgress)
            .RequireAuthorization()
            .WithName("QuickLogProgress");
    }

    private static async Task<IResult> CreateTemplate(
        CreateTemplateRequest request,
        IPlanService planService,
        AppDbContext context,
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

            var trainerProfile = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == int.Parse(userId));

            if (trainerProfile == null)
            {
                return Results.NotFound("Trainer profile not found");
            }

            var template = await planService.CreateTemplateAsync(trainerProfile.Id, request);

            return Results.Ok(template);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to create template: {ex.Message}");
        }
    }

    private static async Task<IResult> GetTrainerTemplates(
        IPlanService planService,
        AppDbContext context,
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

            var trainerProfile = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == int.Parse(userId));

            if (trainerProfile == null)
            {
                return Results.NotFound("Trainer profile not found");
            }

            var templates = await planService.GetTrainerTemplatesAsync(trainerProfile.Id);

            return Results.Ok(new TemplateListResponse(templates));
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get templates: {ex.Message}");
        }
    }

    private static async Task<IResult> UpdateTemplate(
        int id,
        UpdateTemplateRequest request,
        IPlanService planService,
        AppDbContext context,
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

            var trainerProfile = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == int.Parse(userId));

            if (trainerProfile == null)
            {
                return Results.NotFound("Trainer profile not found");
            }

            var template = await planService.UpdateTemplateAsync(trainerProfile.Id, id, request);

            return Results.Ok(template);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to update template: {ex.Message}");
        }
    }

    private static async Task<IResult> DeleteTemplate(
        int id,
        IPlanService planService,
        AppDbContext context,
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

            var trainerProfile = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == int.Parse(userId));

            if (trainerProfile == null)
            {
                return Results.NotFound("Trainer profile not found");
            }

            var deleted = await planService.DeleteTemplateAsync(trainerProfile.Id, id);

            if (!deleted)
            {
                return Results.NotFound("Template not found");
            }

            return Results.Ok(new { Message = "Template deleted successfully" });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to delete template: {ex.Message}");
        }
    }

    private static async Task<IResult> CreateProposal(
        CreateProposalRequest request,
        IPlanService planService,
        AppDbContext context,
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

            var trainerProfile = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == int.Parse(userId));

            if (trainerProfile == null)
            {
                return Results.NotFound("Trainer profile not found");
            }

            var proposal = await planService.CreateProposalAsync(trainerProfile.Id, request);

            return Results.Ok(proposal);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Results.Problem(ex.Message, statusCode: 403);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to create proposal: {ex.Message}");
        }
    }

    private static async Task<IResult> GetTrainerProposals(
        IPlanService planService,
        AppDbContext context,
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

            var trainerProfile = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == int.Parse(userId));

            if (trainerProfile == null)
            {
                return Results.NotFound("Trainer profile not found");
            }

            var proposals = await planService.GetTrainerProposalsAsync(trainerProfile.Id);

            return Results.Ok(new ProposalListResponse(proposals));
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get proposals: {ex.Message}");
        }
    }

    private static async Task<IResult> GetClientProposals(
        IPlanService planService,
        AppDbContext context,
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

            var clientProfile = await context.ClientProfiles
                .FirstOrDefaultAsync(cp => cp.UserId == int.Parse(userId));

            if (clientProfile == null)
            {
                return Results.NotFound("Client profile not found");
            }

            var proposals = await planService.GetClientProposalsAsync(clientProfile.Id);

            return Results.Ok(new ProposalListResponse(proposals));
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get proposals: {ex.Message}");
        }
    }

    private static async Task<IResult> GetClientProposal(
        int id,
        IPlanService planService,
        AppDbContext context,
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

            var clientProfile = await context.ClientProfiles
                .FirstOrDefaultAsync(cp => cp.UserId == int.Parse(userId));

            if (clientProfile == null)
            {
                return Results.NotFound("Client profile not found");
            }

            var proposal = await planService.GetClientProposalAsync(clientProfile.Id, id);

            if (proposal == null)
            {
                return Results.NotFound("Proposal not found");
            }

            return Results.Ok(proposal);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get proposal: {ex.Message}");
        }
    }

    private static async Task<IResult> AcceptProposal(
        int id,
        AcceptProposalRequest request,
        IPlanService planService,
        AppDbContext context,
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

            var clientProfile = await context.ClientProfiles
                .FirstOrDefaultAsync(cp => cp.UserId == int.Parse(userId));

            if (clientProfile == null)
            {
                return Results.NotFound("Client profile not found");
            }

            var response = await planService.AcceptProposalAsync(clientProfile.Id, id, request);

            return Results.Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to accept proposal: {ex.Message}");
        }
    }

    private static async Task<IResult> GetClientPlans(
        IPlanService planService,
        AppDbContext context,
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

            var clientProfile = await context.ClientProfiles
                .FirstOrDefaultAsync(cp => cp.UserId == int.Parse(userId));

            if (clientProfile == null)
            {
                return Results.NotFound("Client profile not found");
            }

            var plans = await planService.GetClientPlansAsync(clientProfile.Id);

            return Results.Ok(new PlanListResponse(plans));
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get plans: {ex.Message}");
        }
    }

    private static async Task<IResult> GetClientBoard(
        string? weekStart,
        IPlanService planService,
        AppDbContext context,
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

            var clientProfile = await context.ClientProfiles
                .FirstOrDefaultAsync(cp => cp.UserId == int.Parse(userId));

            if (clientProfile == null)
            {
                return Results.NotFound("Client profile not found");
            }

            // Parse week start or default to current week's Monday
            DateOnly parsedWeekStart;
            if (!string.IsNullOrEmpty(weekStart) && DateOnly.TryParse(weekStart, out parsedWeekStart))
            {
                // Ensure it's a Monday
                var dayOfWeek = (int)parsedWeekStart.DayOfWeek;
                if (dayOfWeek != 1) // Not Monday
                {
                    var daysToSubtract = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Sunday = 6 days back
                    parsedWeekStart = parsedWeekStart.AddDays(-daysToSubtract);
                }
            }
            else
            {
                // Default to current week's Monday
                var today = DateOnly.FromDateTime(DateTime.Today);
                var dayOfWeek = (int)today.DayOfWeek;
                var daysToSubtract = dayOfWeek == 0 ? 6 : dayOfWeek - 1; // Sunday = 6 days back
                parsedWeekStart = today.AddDays(-daysToSubtract);
            }

            var board = await planService.GetClientBoardAsync(clientProfile.Id, parsedWeekStart);

            return Results.Ok(board);
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get board: {ex.Message}");
        }
    }

    private static async Task<IResult> QuickLogProgress(
        QuickLogRequest request,
        AppDbContext context,
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

            // Create quick progress event
            var progressEvent = new Domain.ProgressEvent
            {
                ExerciseInstanceId = request.ExerciseInstanceId,
                ClientProfileId = clientProfile.Id,
                EventType = request.Completed ? "exercise_completed" : "set_completed",
                SetsCompleted = request.Completed ? exerciseInstance.TargetSets : null,
                RepsCompleted = request.Reps,
                HoldSecondsCompleted = request.HoldSeconds,
                LoggedAt = DateTimeOffset.UtcNow
            };

            context.ProgressEvents.Add(progressEvent);

            // Update exercise instance status
            exerciseInstance.Status = request.Completed ? "done" : "partial";
            exerciseInstance.UpdatedAt = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync();

            return Results.Ok(new QuickLogResponse(
                request.Completed ? "Exercise marked as completed!" : "Progress logged!",
                progressEvent.Id
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to log progress: {ex.Message}");
        }
    }

    private static async Task<IResult> GetTrainerClients(
        AppDbContext context,
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

            var trainerProfile = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == int.Parse(userId));

            if (trainerProfile == null)
            {
                return Results.NotFound("Trainer profile not found");
            }

            // Get clients that have granted consent to this trainer
            var clients = await context.ConsentGrants
                .Where(cg => cg.TrainerProfileId == trainerProfile.Id
                    && cg.ExpiresAt > DateTimeOffset.UtcNow)
                .Include(cg => cg.ClientProfile)
                .ThenInclude(cp => cp.User)
                .Select(cg => new
                {
                    id = cg.ClientProfile.Id,
                    alias = cg.ClientProfile.Alias,
                    email = cg.ClientProfile.User.Email,
                    createdAt = cg.ClientProfile.CreatedAt,
                    scopes = context.ConsentGrants
                        .Where(cg2 => cg2.ClientProfileId == cg.ClientProfile.Id
                            && cg2.TrainerProfileId == trainerProfile.Id
                            && cg2.ExpiresAt > DateTimeOffset.UtcNow)
                        .Select(cg2 => cg2.Scope)
                        .ToList()
                })
                .Distinct()
                .OrderBy(c => c.alias)
                .ToListAsync();

            return Results.Ok(new { clients });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to get clients: {ex.Message}");
        }
    }
}