using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Adaplio.Api.Auth;

public static class ConsentEndpoints
{
    public static void MapConsentEndpoints(this WebApplication app)
    {
        var consentGroup = app.MapGroup("/api").WithTags("Consent & Pairing");

        // Trainer endpoints
        consentGroup.MapPost("/trainer/grants", CreateGrant)
            .RequireAuthorization()
            .WithName("CreateGrant");


        // Client endpoints
        consentGroup.MapPost("/client/grants/accept", AcceptGrant)
            .RequireAuthorization()
            .WithName("AcceptGrant");

        // Development endpoint
        if (app.Environment.IsDevelopment())
        {
            consentGroup.MapPost("/dev/grants/seed", SeedGrant)
                .WithName("SeedGrant");
        }
    }

    private static async Task<IResult> CreateGrant(
        CreateGrantRequest request,
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

            // Get trainer profile
            var trainerProfile = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.UserId == int.Parse(userId));

            if (trainerProfile == null)
            {
                return Results.NotFound("Trainer profile not found");
            }

            // Generate unique grant code
            string grantCode;
            do
            {
                grantCode = aliasService.GenerateUniqueCode();
            } while (await context.GrantCodes.AnyAsync(gc => gc.Code == grantCode));

            var expiresAt = DateTimeOffset.UtcNow.AddHours(24); // 24 hour expiry

            var grant = new GrantCode
            {
                TrainerProfileId = trainerProfile.Id,
                Code = grantCode,
                ExpiresAt = expiresAt,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString()
            };

            context.GrantCodes.Add(grant);
            await context.SaveChangesAsync();

            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var grantUrl = $"{baseUrl}/grant/{grantCode}";

            return Results.Ok(new CreateGrantResponse(
                GrantCode: grantCode,
                Url: grantUrl,
                ExpiresAt: expiresAt
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to create grant code. Please try again.");
        }
    }

    private static async Task<IResult> AcceptGrant(
        AcceptGrantRequest request,
        AppDbContext context,
        IAliasService aliasService,
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

            // Find valid grant code
            var grantCode = await context.GrantCodes
                .Include(gc => gc.TrainerProfile)
                    .ThenInclude(tp => tp.User)
                .FirstOrDefaultAsync(gc =>
                    gc.Code == request.GrantCode &&
                    gc.ExpiresAt > DateTimeOffset.UtcNow &&
                    gc.UsedAt == null);

            if (grantCode == null)
            {
                return Results.BadRequest("Invalid or expired grant code");
            }

            // Check if consent grant already exists
            var existingGrant = await context.ConsentGrants
                .FirstOrDefaultAsync(cg =>
                    cg.ClientProfileId == clientProfile.Id &&
                    cg.TrainerProfileId == grantCode.TrainerProfileId &&
                    cg.RevokedAt == null);

            if (existingGrant != null)
            {
                return Results.BadRequest("You are already connected to this trainer");
            }

            // Mark grant code as used
            grantCode.UsedAt = DateTimeOffset.UtcNow;
            grantCode.UsedByClientProfileId = clientProfile.Id;

            // Generate stable alias for this client-trainer pair
            var clientAlias = aliasService.GenerateClientAlias(clientProfile.Id, grantCode.TrainerProfileId);

            // Update client profile with the alias for this trainer
            if (string.IsNullOrEmpty(clientProfile.Alias))
            {
                clientProfile.Alias = clientAlias;
            }

            // Create consent grants with default scopes
            var defaultScopes = new[] { "propose_plan", "view_summary", "message_client" };
            var consentGrants = defaultScopes.Select(scope => new ConsentGrant
            {
                ClientProfileId = clientProfile.Id,
                TrainerProfileId = grantCode.TrainerProfileId,
                Scope = scope,
                GrantedAt = DateTimeOffset.UtcNow
            }).ToList();

            context.ConsentGrants.AddRange(consentGrants);
            await context.SaveChangesAsync();

            var trainerName = grantCode.TrainerProfile.User.Email; // Use email as display name for now

            return Results.Ok(new AcceptGrantResponse(
                Message: "Successfully connected to trainer",
                TrainerName: trainerName,
                Scopes: defaultScopes
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to accept grant. Please try again.");
        }
    }


    private static async Task<IResult> SeedGrant(
        AppDbContext context,
        IAliasService aliasService)
    {
        try
        {
            // Check if demo trainer already exists
            var existingTrainer = await context.AppUsers
                .Include(u => u.TrainerProfile)
                .FirstOrDefaultAsync(u => u.Email == "demo-trainer@adaplio.local");

            TrainerProfile trainerProfile;

            if (existingTrainer == null)
            {
                // Create a sample trainer
                var trainerUser = new AppUser
                {
                    Email = "demo-trainer@adaplio.local",
                    UserType = "trainer",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoPass123"),
                    IsVerified = true
                };

                context.AppUsers.Add(trainerUser);
                await context.SaveChangesAsync();

                trainerProfile = new TrainerProfile
                {
                    UserId = trainerUser.Id,
                    FullName = "Dr. Demo Trainer",
                    PracticeName = "Demo Physical Therapy"
                };

                context.TrainerProfiles.Add(trainerProfile);
                await context.SaveChangesAsync();
            }
            else
            {
                trainerProfile = existingTrainer.TrainerProfile!;
            }

            // Check if demo client already exists
            var existingClient = await context.AppUsers
                .Include(u => u.ClientProfile)
                .FirstOrDefaultAsync(u => u.Email == "demo-client@adaplio.local");

            ClientProfile clientProfile;

            if (existingClient == null)
            {
                // Create a sample client
                var clientUser = new AppUser
                {
                    Email = "demo-client@adaplio.local",
                    UserType = "client",
                    IsVerified = true
                };

                context.AppUsers.Add(clientUser);
                await context.SaveChangesAsync();

                clientProfile = new ClientProfile
                {
                    UserId = clientUser.Id
                };

                context.ClientProfiles.Add(clientProfile);
                await context.SaveChangesAsync();

                // Generate alias using the actual profile ID
                clientProfile.Alias = aliasService.GenerateClientAlias(clientProfile.Id, trainerProfile.Id);
                await context.SaveChangesAsync();
            }
            else
            {
                clientProfile = existingClient.ClientProfile!;
            }

            // Check if relationship already exists
            var existingGrant = await context.ConsentGrants
                .FirstOrDefaultAsync(cg =>
                    cg.ClientProfileId == clientProfile.Id &&
                    cg.TrainerProfileId == trainerProfile.Id &&
                    cg.RevokedAt == null);

            if (existingGrant != null)
            {
                // Return existing relationship
                var existingGrantCode = await context.GrantCodes
                    .FirstOrDefaultAsync(gc => gc.TrainerProfileId == trainerProfile.Id);

                return Results.Ok(new SeedGrantResponse(
                    TrainerEmail: "demo-trainer@adaplio.local",
                    ClientEmail: "demo-client@adaplio.local",
                    ClientAlias: clientProfile.Alias!,
                    GrantCode: existingGrantCode?.Code ?? "EXISTING",
                    ExpiresAt: existingGrantCode?.ExpiresAt ?? DateTimeOffset.UtcNow.AddHours(24)
                ));
            }

            // Generate a grant code
            string grantCode;
            do
            {
                grantCode = aliasService.GenerateUniqueCode();
            } while (await context.GrantCodes.AnyAsync(gc => gc.Code == grantCode));

            var expiresAt = DateTimeOffset.UtcNow.AddHours(24);

            var grant = new GrantCode
            {
                TrainerProfileId = trainerProfile.Id,
                Code = grantCode,
                ExpiresAt = expiresAt
            };

            context.GrantCodes.Add(grant);

            // Create the consent relationship
            var defaultScopes = new[] { "propose_plan", "view_summary", "message_client" };
            var consentGrants = defaultScopes.Select(scope => new ConsentGrant
            {
                ClientProfileId = clientProfile.Id,
                TrainerProfileId = trainerProfile.Id,
                Scope = scope,
                GrantedAt = DateTimeOffset.UtcNow
            }).ToList();

            context.ConsentGrants.AddRange(consentGrants);
            await context.SaveChangesAsync();

            return Results.Ok(new SeedGrantResponse(
                TrainerEmail: "demo-trainer@adaplio.local",
                ClientEmail: "demo-client@adaplio.local",
                ClientAlias: clientProfile.Alias!,
                GrantCode: grantCode,
                ExpiresAt: expiresAt
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to seed demo data. Please try again.");
        }
    }
}