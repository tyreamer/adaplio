using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Adaplio.Api.Auth;

public static class InviteEndpoints
{
    public static void MapInviteEndpoints(this WebApplication app)
    {
        var inviteGroup = app.MapGroup("/api/invites").WithTags("Invites");

        // SMS invite endpoint (public)
        inviteGroup.MapPost("/sms", SendSMSInvite)
            .WithName("SendSMSInvite");

        // Email invite endpoint (trainer only)
        inviteGroup.MapPost("/email", SendEmailInvite)
            .RequireAuthorization()
            .WithName("SendEmailInvite");

        // Create invite token endpoint (trainer only)
        inviteGroup.MapPost("/token", CreateInviteToken)
            .RequireAuthorization()
            .WithName("CreateInviteToken");

        // NOTE: Validate invite token moved to InvitesController to avoid route duplication
    }

    private static async Task<IResult> SendSMSInvite(
        SMSInviteRequest request,
        AppDbContext context,
        ISMSService smsService,
        IAliasService aliasService)
    {
        try
        {
            // Generate or validate invite token
            string inviteToken;
            string? trainerName = null;

            if (!string.IsNullOrEmpty(request.InviteCode))
            {
                // Validate the provided code and get trainer info
                // Client-side date evaluation for EF compatibility
                var now = DateTimeOffset.UtcNow;
                var grantCodes = await context.GrantCodes
                    .Include(gc => gc.TrainerProfile)
                    .Where(gc => gc.Code == request.InviteCode && gc.UsedAt == null)
                    .ToListAsync();

                var grantCode = grantCodes.FirstOrDefault(gc => gc.ExpiresAt > now);

                if (grantCode == null)
                {
                    return Results.BadRequest(new { error = "Invalid or expired invite code" });
                }

                trainerName = grantCode.TrainerProfile?.FullName;
                inviteToken = aliasService.GenerateUniqueCode(); // Generate opaque token

                // Create invite token record
                var inviteTokenRecord = new InviteToken
                {
                    Token = inviteToken,
                    GrantCodeId = grantCode.Id,
                    PhoneNumber = request.PhoneNumber,
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                context.InviteTokens.Add(inviteTokenRecord);
                await context.SaveChangesAsync();
            }
            else
            {
                // Create generic invite token without specific trainer
                inviteToken = aliasService.GenerateUniqueCode();

                var inviteTokenRecord = new InviteToken
                {
                    Token = inviteToken,
                    PhoneNumber = request.PhoneNumber,
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                context.InviteTokens.Add(inviteTokenRecord);
                await context.SaveChangesAsync();
            }

            // Send SMS
            var success = await smsService.SendInviteLinkAsync(request.PhoneNumber, inviteToken, trainerName);

            if (success)
            {
                return Results.Ok(new { message = "Invite link sent successfully!" });
            }
            else
            {
                return Results.Problem("Failed to send SMS. Please try again.");
            }
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to send invite. Please try again.");
        }
    }

    private static async Task<IResult> SendEmailInvite(
        EmailInviteRequest request,
        AppDbContext context,
        IEmailService emailService,
        IAliasService aliasService,
        HttpContext httpContext)
    {
        try
        {
            // Get trainer info from authenticated user
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
                return Results.NotFound(new { error = "Trainer profile not found" });
            }

            // Get or create grant code for this trainer
            var now = DateTimeOffset.UtcNow;
            var grantCodes = await context.GrantCodes
                .Where(gc => gc.TrainerProfileId == trainerProfile.Id && gc.UsedAt == null)
                .ToListAsync();

            var grantCode = grantCodes.FirstOrDefault(gc => gc.ExpiresAt > now);

            if (grantCode == null)
            {
                return Results.BadRequest(new { error = "No valid grant code found. Please create a new invitation code first." });
            }

            // Generate invite token
            var inviteToken = aliasService.GenerateUniqueCode();

            var inviteTokenRecord = new InviteToken
            {
                Token = inviteToken,
                GrantCodeId = grantCode.Id,
                Email = request.Email,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
                CreatedAt = DateTimeOffset.UtcNow
            };

            context.InviteTokens.Add(inviteTokenRecord);
            await context.SaveChangesAsync();

            // Build invite URL
            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var inviteUrl = $"{baseUrl}/?invite={inviteToken}";

            // Send email
            var trainerName = trainerProfile.FullName ?? "Your Physical Therapist";
            await emailService.SendInviteEmailAsync(request.Email, inviteUrl, trainerName);

            return Results.Ok(new { message = "Invite email sent successfully!" });
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to send invite email. Please try again.");
        }
    }

    private static async Task<IResult> CreateInviteToken(
        CreateInviteTokenRequest request,
        AppDbContext context,
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

            // Get trainer's grant code
            var trainerProfile = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == int.Parse(userId));

            if (trainerProfile == null)
            {
                return Results.NotFound("Trainer profile not found");
            }

            // Client-side date evaluation for EF compatibility
            var now = DateTimeOffset.UtcNow;
            var grantCodes = await context.GrantCodes
                .Where(gc => gc.TrainerProfileId == trainerProfile.Id && gc.UsedAt == null)
                .ToListAsync();

            var grantCode = grantCodes.FirstOrDefault(gc => gc.ExpiresAt > now);

            if (grantCode == null)
            {
                return Results.BadRequest("No valid grant code found. Please create a new invitation code first.");
            }

            // Generate opaque invite token
            var inviteToken = aliasService.GenerateUniqueCode();

            var inviteTokenRecord = new InviteToken
            {
                Token = inviteToken,
                GrantCodeId = grantCode.Id,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(request.ExpirationHours ?? 24),
                CreatedAt = DateTimeOffset.UtcNow
            };

            context.InviteTokens.Add(inviteTokenRecord);
            await context.SaveChangesAsync();

            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var inviteUrl = $"{baseUrl}/?invite={inviteToken}";
            var qrUrl = $"{baseUrl}/qr/{inviteToken}";

            return Results.Ok(new CreateInviteTokenResponse(
                Token: inviteToken,
                InviteUrl: inviteUrl,
                QRCodeUrl: qrUrl,
                ExpiresAt: inviteTokenRecord.ExpiresAt
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to create invite token");
        }
    }

    // NOTE: ValidateInviteToken method removed - functionality moved to InvitesController
}

// DTOs
public record SMSInviteRequest(
    [Required] string PhoneNumber,
    string? InviteCode = null
);

public record EmailInviteRequest(
    [Required, EmailAddress] string Email
);

public record CreateInviteTokenRequest(
    int? ExpirationHours = 24
);

public record CreateInviteTokenResponse(
    string Token,
    string InviteUrl,
    string QRCodeUrl,
    DateTimeOffset ExpiresAt
);

// NOTE: ValidateInviteTokenResponse removed - using DTO from InvitesController