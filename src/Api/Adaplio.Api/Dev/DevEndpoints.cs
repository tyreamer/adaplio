using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Adaplio.Api.Dev;

public static class DevEndpoints
{
    public static void MapDevEndpoints(this WebApplication app)
    {
        var devGroup = app.MapGroup("/api/dev").WithTags("Development");

        // Seed template and proposal endpoint
        devGroup.MapPost("/templates/seed", SeedTemplatesAndProposal)
            .WithName("SeedTemplatesAndProposal");
    }

    private static async Task<IResult> SeedTemplatesAndProposal(
        AppDbContext context,
        IPlanService planService)
    {
        try
        {
            // Check if demo data already exists
            var existingTrainer = await context.TrainerProfiles
                .FirstOrDefaultAsync(tp => tp.FullName != null && tp.FullName.Contains("Demo Trainer"));

            if (existingTrainer != null)
            {
                return Results.BadRequest("Demo data already exists. Reset your database if you want to reseed.");
            }

            // Create demo trainer
            var trainerUser = new AppUser
            {
                Email = "demo-trainer@adaplio.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("DemoPass123"),
                UserType = "trainer",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.AppUsers.Add(trainerUser);
            await context.SaveChangesAsync();

            var trainerProfile = new TrainerProfile
            {
                UserId = trainerUser.Id,
                FullName = "Demo Trainer",
                PracticeName = "Demo Physical Therapy Clinic",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.TrainerProfiles.Add(trainerProfile);
            await context.SaveChangesAsync();

            // Create demo client
            var clientUser = new AppUser
            {
                Email = "demo-client@adaplio.local",
                UserType = "client",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.AppUsers.Add(clientUser);
            await context.SaveChangesAsync();

            var clientProfile = new ClientProfile
            {
                UserId = clientUser.Id,
                Alias = "C-DEMO",
                CreatedAt = DateTimeOffset.UtcNow
            };
            context.ClientProfiles.Add(clientProfile);
            await context.SaveChangesAsync();

            // Create consent grants
            var consentGrant = new ConsentGrant
            {
                ClientProfileId = clientProfile.Id,
                TrainerProfileId = trainerProfile.Id,
                Scope = "propose_plan",
                GrantedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(365)
            };
            context.ConsentGrants.Add(consentGrant);

            var consentGrant2 = new ConsentGrant
            {
                ClientProfileId = clientProfile.Id,
                TrainerProfileId = trainerProfile.Id,
                Scope = "view_summary",
                GrantedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(365)
            };
            context.ConsentGrants.Add(consentGrant2);
            await context.SaveChangesAsync();

            // Create demo exercises
            var exercises = new[]
            {
                new Exercise
                {
                    Name = "Wall Push-ups",
                    Description = "Stand arm's length from wall, push against wall",
                    Category = "strength",
                    DefaultSets = 3,
                    DefaultReps = 10,
                    Instructions = "1. Stand arm's length from wall\n2. Place palms flat against wall\n3. Push away from wall and return",
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new Exercise
                {
                    Name = "Shoulder Rolls",
                    Description = "Roll shoulders in circular motion",
                    Category = "mobility",
                    DefaultSets = 2,
                    DefaultReps = 15,
                    Instructions = "1. Stand upright\n2. Roll shoulders backwards in large circles\n3. Keep movements slow and controlled",
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new Exercise
                {
                    Name = "Neck Stretches",
                    Description = "Gentle neck stretching exercises",
                    Category = "mobility",
                    DefaultSets = 1,
                    DefaultReps = 5,
                    DefaultHoldSeconds = 10,
                    Instructions = "1. Tilt head gently to one side\n2. Hold stretch\n3. Return to center and repeat other side",
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new Exercise
                {
                    Name = "Heel Raises",
                    Description = "Rise up onto toes and lower slowly",
                    Category = "strength",
                    DefaultSets = 2,
                    DefaultReps = 20,
                    Instructions = "1. Stand with feet shoulder-width apart\n2. Rise up onto toes\n3. Lower slowly",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            };

            context.Exercises.AddRange(exercises);
            await context.SaveChangesAsync();

            // Create demo template using the service
            var templateRequest = new Plans.CreateTemplateRequest(
                "Upper Body Recovery Plan",
                "A gentle upper body rehabilitation plan for post-injury recovery",
                "Rehabilitation",
                4, // 4 weeks
                true,
                new Plans.TemplateItemRequest[]
                {
                    new("Wall Push-ups", null, null, 3, 10, null, 3, new[] { "Monday", "Wednesday", "Friday" }, "Start gently, focus on form"),
                    new("Shoulder Rolls", null, null, 2, 15, null, 3, new[] { "Monday", "Wednesday", "Friday" }, "Keep movements slow"),
                    new("Neck Stretches", null, null, 1, 5, 10, 5, new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" }, "Hold each stretch for 10 seconds")
                }
            );

            var template = await planService.CreateTemplateAsync(trainerProfile.Id, templateRequest);

            // Create demo proposal
            var proposalRequest = new Plans.CreateProposalRequest(
                clientProfile.Alias,
                template.Id,
                null, // Use default next Monday
                "Hi! I've created a gentle upper body recovery plan for you. This should help with your shoulder mobility and strength. Please review and let me know if you'd like to accept it."
            );

            var proposal = await planService.CreateProposalAsync(trainerProfile.Id, proposalRequest);

            return Results.Ok(new
            {
                Message = "Demo data seeded successfully!",
                TrainerEmail = "demo-trainer@adaplio.local",
                TrainerPassword = "DemoPass123",
                ClientEmail = "demo-client@adaplio.local",
                ClientAlias = clientProfile.Alias,
                TemplateId = template.Id,
                ProposalId = proposal.Id,
                Instructions = new
                {
                    TrainerLogin = "Login as trainer to view templates and proposals at /auth/trainer/login",
                    ClientLogin = "Login as client to view and accept proposals at /auth/client/login",
                    ApiEndpoints = new
                    {
                        Templates = "GET /api/trainer/templates",
                        Proposals = "GET /api/trainer/proposals (trainer), GET /api/client/proposals (client)",
                        AcceptProposal = $"POST /api/client/proposals/{proposal.Id}/accept"
                    }
                }
            });
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to seed demo data: {ex.Message}");
        }
    }
}