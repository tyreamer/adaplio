using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Adaplio.Api.Auth;

public static class InviteEndpoints
{
    public static void MapInviteEndpoints(this WebApplication app)
    {
        var inviteGroup = app.MapGroup("/api/invites").WithTags("Invites");

        // SMS invite endpoint (public)
        inviteGroup.MapPost("/sms", SendSMSInvite)
            .WithName("SendSMSInvite");

        // Create invite token endpoint (trainer only)
        inviteGroup.MapPost("/token", CreateInviteToken)
            .RequireAuthorization()
            .WithName("CreateInviteToken");

        // Validate invite token (public)
        inviteGroup.MapGet("/validate/{token}", ValidateInviteToken)
            .WithName("ValidateInviteToken");
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
                var grantCode = await context.GrantCodes
                    .Include(gc => gc.TrainerProfile)
                    .FirstOrDefaultAsync(gc => gc.Code == request.InviteCode &&
                                             gc.ExpiresAt > DateTimeOffset.UtcNow &&
                                             gc.UsedAt == null);

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

    private static async Task<IResult> CreateInviteToken(
        CreateInviteTokenRequest request,
        AppDbContext context,
        IAliasService aliasService,
        HttpContext httpContext)
    {
        try
        {
            var userId = httpContext.User.FindFirst("UserId")?.Value;
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

            var grantCode = await context.GrantCodes
                .FirstOrDefaultAsync(gc => gc.TrainerProfileId == trainerProfile.Id &&
                                         gc.ExpiresAt > DateTimeOffset.UtcNow &&
                                         gc.UsedAt == null);

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

    private static async Task<IResult> ValidateInviteToken(
        string token,
        AppDbContext context)
    {
        try
        {
            var inviteToken = await context.InviteTokens
                .Include(it => it.GrantCode)
                    .ThenInclude(gc => gc.TrainerProfile)
                .FirstOrDefaultAsync(it => it.Token == token &&
                                         it.ExpiresAt > DateTimeOffset.UtcNow &&
                                         it.UsedAt == null);

            if (inviteToken == null)
            {
                return Results.NotFound(new { error = "Invalid or expired invite token" });
            }

            var response = new ValidateInviteTokenResponse(
                IsValid: true,
                TrainerName: inviteToken.GrantCode?.TrainerProfile?.FullName ?? "Unknown Trainer",
                ClinicName: inviteToken.GrantCode?.TrainerProfile?.PracticeName ?? "Unknown Clinic",
                GrantCode: inviteToken.GrantCode?.Code ?? "",
                ExpiresAt: inviteToken.ExpiresAt
            );

            return Results.Ok(response);
        }
        catch (Exception)
        {
            return Results.Problem("Failed to validate invite token");
        }
    }
}

// DTOs
public record SMSInviteRequest(
    [Required] string PhoneNumber,
    string? InviteCode = null
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

public record ValidateInviteTokenResponse(
    bool IsValid,
    string TrainerName,
    string ClinicName,
    string GrantCode,
    DateTimeOffset ExpiresAt
);