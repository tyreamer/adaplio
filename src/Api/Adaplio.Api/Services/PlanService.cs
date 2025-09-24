using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Plans;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Globalization;

namespace Adaplio.Api.Services;

public interface IPlanService
{
    Task<TemplateResponse[]> GetTrainerTemplatesAsync(int trainerProfileId);
    Task<TemplateResponse> CreateTemplateAsync(int trainerProfileId, CreateTemplateRequest request);
    Task<TemplateResponse> UpdateTemplateAsync(int trainerProfileId, int templateId, UpdateTemplateRequest request);
    Task<bool> DeleteTemplateAsync(int trainerProfileId, int templateId);

    Task<ProposalResponse> CreateProposalAsync(int trainerProfileId, CreateProposalRequest request);
    Task<ProposalResponse[]> GetTrainerProposalsAsync(int trainerProfileId);
    Task<ProposalResponse[]> GetClientProposalsAsync(int clientProfileId);
    Task<ProposalResponse?> GetClientProposalAsync(int clientProfileId, int proposalId);

    Task<AcceptProposalResponse> AcceptProposalAsync(int clientProfileId, int proposalId, AcceptProposalRequest request);
    Task<PlanInstanceResponse[]> GetClientPlansAsync(int clientProfileId);
    Task<BoardResponse> GetClientBoardAsync(int clientProfileId, DateOnly weekStart);
}

public class PlanService : IPlanService
{
    private readonly AppDbContext _context;

    public PlanService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TemplateResponse[]> GetTrainerTemplatesAsync(int trainerProfileId)
    {
        var templates = await _context.PlanTemplates
            .Include(pt => pt.PlanTemplateItems)
            .ThenInclude(pti => pti.Exercise)
            .Where(pt => pt.TrainerProfileId == trainerProfileId && !pt.IsDeleted)
            .OrderByDescending(pt => pt.UpdatedAt)
            .ToListAsync();

        return templates.Select(MapTemplateToResponse).ToArray();
    }

    public async Task<TemplateResponse> CreateTemplateAsync(int trainerProfileId, CreateTemplateRequest request)
    {
        var template = new PlanTemplate
        {
            TrainerProfileId = trainerProfileId,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            DurationWeeks = request.DurationWeeks,
            IsPublic = request.IsPublic,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.PlanTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Add items
        for (int i = 0; i < request.Items.Length; i++)
        {
            var itemRequest = request.Items[i];

            // Find or create exercise
            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Name.ToLower() == itemRequest.ExerciseName.ToLower());

            if (exercise == null)
            {
                exercise = new Exercise
                {
                    Name = itemRequest.ExerciseName,
                    Description = itemRequest.ExerciseDescription,
                    Category = itemRequest.ExerciseCategory,
                    DefaultSets = itemRequest.TargetSets,
                    DefaultReps = itemRequest.TargetReps,
                    DefaultHoldSeconds = itemRequest.HoldSeconds,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                _context.Exercises.Add(exercise);
                await _context.SaveChangesAsync();
            }

            var templateItem = new PlanTemplateItem
            {
                PlanTemplateId = template.Id,
                ExerciseId = exercise.Id,
                OrderIndex = i,
                Sets = itemRequest.TargetSets,
                Reps = itemRequest.TargetReps,
                HoldSeconds = itemRequest.HoldSeconds,
                FrequencyPerWeek = itemRequest.FrequencyPerWeek,
                DaysOfWeek = itemRequest.Days != null ? JsonSerializer.Serialize(itemRequest.Days) : null,
                Notes = itemRequest.Notes,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.PlanTemplateItems.Add(templateItem);
        }

        await _context.SaveChangesAsync();

        // Reload with includes
        var createdTemplate = await _context.PlanTemplates
            .Include(pt => pt.PlanTemplateItems)
            .ThenInclude(pti => pti.Exercise)
            .FirstAsync(pt => pt.Id == template.Id);

        return MapTemplateToResponse(createdTemplate);
    }

    public async Task<TemplateResponse> UpdateTemplateAsync(int trainerProfileId, int templateId, UpdateTemplateRequest request)
    {
        var template = await _context.PlanTemplates
            .Include(pt => pt.PlanTemplateItems)
            .FirstOrDefaultAsync(pt => pt.Id == templateId && pt.TrainerProfileId == trainerProfileId && !pt.IsDeleted);

        if (template == null)
            throw new InvalidOperationException("Template not found");

        // Update template
        template.Name = request.Name;
        template.Description = request.Description;
        template.Category = request.Category;
        template.DurationWeeks = request.DurationWeeks;
        template.IsPublic = request.IsPublic;
        template.UpdatedAt = DateTimeOffset.UtcNow;

        // Remove existing items
        _context.PlanTemplateItems.RemoveRange(template.PlanTemplateItems);

        // Add new items
        for (int i = 0; i < request.Items.Length; i++)
        {
            var itemRequest = request.Items[i];

            // Find or create exercise
            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Name.ToLower() == itemRequest.ExerciseName.ToLower());

            if (exercise == null)
            {
                exercise = new Exercise
                {
                    Name = itemRequest.ExerciseName,
                    Description = itemRequest.ExerciseDescription,
                    Category = itemRequest.ExerciseCategory,
                    DefaultSets = itemRequest.TargetSets,
                    DefaultReps = itemRequest.TargetReps,
                    DefaultHoldSeconds = itemRequest.HoldSeconds,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };
                _context.Exercises.Add(exercise);
                await _context.SaveChangesAsync();
            }

            var templateItem = new PlanTemplateItem
            {
                PlanTemplateId = template.Id,
                ExerciseId = exercise.Id,
                OrderIndex = i,
                Sets = itemRequest.TargetSets,
                Reps = itemRequest.TargetReps,
                HoldSeconds = itemRequest.HoldSeconds,
                FrequencyPerWeek = itemRequest.FrequencyPerWeek,
                DaysOfWeek = itemRequest.Days != null ? JsonSerializer.Serialize(itemRequest.Days) : null,
                Notes = itemRequest.Notes,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.PlanTemplateItems.Add(templateItem);
        }

        await _context.SaveChangesAsync();

        // Reload with includes
        var updatedTemplate = await _context.PlanTemplates
            .Include(pt => pt.PlanTemplateItems)
            .ThenInclude(pti => pti.Exercise)
            .FirstAsync(pt => pt.Id == template.Id);

        return MapTemplateToResponse(updatedTemplate);
    }

    public async Task<bool> DeleteTemplateAsync(int trainerProfileId, int templateId)
    {
        var template = await _context.PlanTemplates
            .FirstOrDefaultAsync(pt => pt.Id == templateId && pt.TrainerProfileId == trainerProfileId && !pt.IsDeleted);

        if (template == null)
            return false;

        template.IsDeleted = true;
        template.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<ProposalResponse> CreateProposalAsync(int trainerProfileId, CreateProposalRequest request)
    {
        // Find client by alias
        var clientProfile = await _context.ClientProfiles
            .FirstOrDefaultAsync(cp => cp.Alias == request.ClientAlias);

        if (clientProfile == null)
            throw new InvalidOperationException("Client not found");

        // Check consent
        var hasConsent = await _context.ConsentGrants
            .AnyAsync(cg => cg.ClientProfileId == clientProfile.Id &&
                           cg.TrainerProfileId == trainerProfileId &&
                           cg.Scope == "propose_plan" &&
                           cg.RevokedAt == null);

        if (!hasConsent)
            throw new UnauthorizedAccessException("No consent to propose plans to this client");

        // Get template
        var template = await _context.PlanTemplates
            .Include(pt => pt.PlanTemplateItems)
            .ThenInclude(pti => pti.Exercise)
            .FirstOrDefaultAsync(pt => pt.Id == request.TemplateId &&
                                     pt.TrainerProfileId == trainerProfileId &&
                                     !pt.IsDeleted);

        if (template == null)
            throw new InvalidOperationException("Template not found");

        // Default starts on to next Monday if not specified
        var startsOn = request.StartsOn ?? GetNextMonday();

        // Create immutable proposal snapshot
        var proposal = new PlanProposal
        {
            TrainerProfileId = trainerProfileId,
            ClientProfileId = clientProfile.Id,
            PlanTemplateId = template.Id,
            ProposalName = template.Name,
            Message = request.Message,
            Status = "pending",
            ProposedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30), // 30 day expiry
            StartsOn = startsOn,
            CustomPlanJson = JsonSerializer.Serialize(template.PlanTemplateItems.Select(pti => new
            {
                ExerciseId = pti.ExerciseId,
                ExerciseName = pti.Exercise.Name,
                ExerciseDescription = pti.Exercise.Description,
                OrderIndex = pti.OrderIndex,
                Sets = pti.Sets,
                Reps = pti.Reps,
                HoldSeconds = pti.HoldSeconds,
                FrequencyPerWeek = pti.FrequencyPerWeek,
                DaysOfWeek = pti.DaysOfWeek,
                Notes = pti.Notes
            }).ToArray())
        };

        _context.PlanProposals.Add(proposal);
        await _context.SaveChangesAsync();

        // Load full proposal for response
        var createdProposal = await _context.PlanProposals
            .Include(pp => pp.TrainerProfile)
            .Include(pp => pp.ClientProfile)
            .Include(pp => pp.PlanTemplate)
            .ThenInclude(pt => pt!.PlanTemplateItems)
            .ThenInclude(pti => pti.Exercise)
            .FirstAsync(pp => pp.Id == proposal.Id);

        return MapProposalToResponse(createdProposal);
    }

    public async Task<ProposalResponse[]> GetTrainerProposalsAsync(int trainerProfileId)
    {
        var proposals = await _context.PlanProposals
            .Include(pp => pp.TrainerProfile)
            .Include(pp => pp.ClientProfile)
            .Include(pp => pp.PlanTemplate)
            .ThenInclude(pt => pt!.PlanTemplateItems)
            .ThenInclude(pti => pti.Exercise)
            .Where(pp => pp.TrainerProfileId == trainerProfileId)
            .OrderByDescending(pp => pp.ProposedAt)
            .ToListAsync();

        return proposals.Select(MapProposalToResponse).ToArray();
    }

    public async Task<ProposalResponse[]> GetClientProposalsAsync(int clientProfileId)
    {
        var proposals = await _context.PlanProposals
            .Include(pp => pp.TrainerProfile)
            .Include(pp => pp.ClientProfile)
            .Include(pp => pp.PlanTemplate)
            .ThenInclude(pt => pt!.PlanTemplateItems)
            .ThenInclude(pti => pti.Exercise)
            .Where(pp => pp.ClientProfileId == clientProfileId)
            .OrderByDescending(pp => pp.ProposedAt)
            .ToListAsync();

        return proposals.Select(MapProposalToResponse).ToArray();
    }

    public async Task<ProposalResponse?> GetClientProposalAsync(int clientProfileId, int proposalId)
    {
        var proposal = await _context.PlanProposals
            .Include(pp => pp.TrainerProfile)
            .Include(pp => pp.ClientProfile)
            .Include(pp => pp.PlanTemplate)
            .ThenInclude(pt => pt!.PlanTemplateItems)
            .ThenInclude(pti => pti.Exercise)
            .FirstOrDefaultAsync(pp => pp.Id == proposalId && pp.ClientProfileId == clientProfileId);

        return proposal != null ? MapProposalToResponse(proposal) : null;
    }

    public async Task<AcceptProposalResponse> AcceptProposalAsync(int clientProfileId, int proposalId, AcceptProposalRequest request)
    {
        var proposal = await _context.PlanProposals
            .Include(pp => pp.PlanTemplate)
            .ThenInclude(pt => pt!.PlanTemplateItems)
            .ThenInclude(pti => pti.Exercise)
            .FirstOrDefaultAsync(pp => pp.Id == proposalId && pp.ClientProfileId == clientProfileId);

        if (proposal == null)
            throw new InvalidOperationException("Proposal not found");

        if (proposal.Status != "pending")
            throw new InvalidOperationException("Proposal already responded to");

        if (proposal.ExpiresAt.HasValue && proposal.ExpiresAt.Value < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("Proposal has expired");

        // Parse items from JSON snapshot
        var proposalItems = JsonSerializer.Deserialize<dynamic[]>(proposal.CustomPlanJson ?? "[]")!;

        // Determine which items to accept
        var itemsToAccept = request.AcceptAll == true
            ? proposalItems.ToList()
            : proposalItems.Where((item, index) => request.AcceptItemIds?.Contains(index) == true).ToList();

        if (!itemsToAccept.Any())
            throw new InvalidOperationException("No items selected for acceptance");

        // Create plan instance
        var planInstance = new PlanInstance
        {
            ClientProfileId = clientProfileId,
            PlanProposalId = proposalId,
            Name = proposal.ProposalName,
            Status = "active",
            StartDate = proposal.StartsOn ?? GetNextMonday(),
            PlannedEndDate = proposal.PlanTemplate?.DurationWeeks.HasValue == true
                ? (proposal.StartsOn ?? GetNextMonday()).AddDays(proposal.PlanTemplate.DurationWeeks.Value * 7)
                : null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.PlanInstances.Add(planInstance);
        await _context.SaveChangesAsync();

        // Create exercise instances and acceptance records
        var acceptedCount = 0;
        for (int i = 0; i < itemsToAccept.Count; i++)
        {
            var item = itemsToAccept[i];
            var itemJson = ((JsonElement)item);

            var exerciseId = itemJson.GetProperty("ExerciseId").GetInt32();
            var daysJson = itemJson.TryGetProperty("DaysOfWeek", out var daysElement) ? daysElement.GetString() : null;
            var days = !string.IsNullOrEmpty(daysJson) ? JsonSerializer.Deserialize<string[]>(daysJson) : null;

            if (days?.Any() == true)
            {
                // Create exercise instances for each scheduled day
                foreach (var day in days)
                {
                    var dayOfWeek = GetDayOfWeekNumber(day);

                    var exerciseInstance = new ExerciseInstance
                    {
                        PlanInstanceId = planInstance.Id,
                        ExerciseId = exerciseId,
                        WeekNumber = 1, // Start with week 1
                        OrderIndex = i,
                        TargetSets = itemJson.TryGetProperty("Sets", out var setsElement) ? setsElement.GetInt32() : null,
                        TargetReps = itemJson.TryGetProperty("Reps", out var repsElement) ? repsElement.GetInt32() : null,
                        TargetHoldSeconds = itemJson.TryGetProperty("HoldSeconds", out var holdElement) ? holdElement.GetInt32() : null,
                        FrequencyPerWeek = itemJson.TryGetProperty("FrequencyPerWeek", out var freqElement) ? freqElement.GetInt32() : null,
                        DayOfWeek = dayOfWeek,
                        Status = "planned",
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    _context.ExerciseInstances.Add(exerciseInstance);
                    await _context.SaveChangesAsync();

                    // Record acceptance
                    var acceptance = new PlanItemAcceptance
                    {
                        PlanInstanceId = planInstance.Id,
                        ExerciseInstanceId = exerciseInstance.Id,
                        Accepted = true,
                        AcceptedAt = DateTimeOffset.UtcNow
                    };

                    _context.PlanItemAcceptances.Add(acceptance);
                    acceptedCount++;
                }
            }
        }

        // Update proposal status
        proposal.Status = "accepted";
        proposal.RespondedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();

        return new AcceptProposalResponse(
            "Proposal accepted successfully",
            planInstance.Id,
            acceptedCount,
            proposalItems.Length
        );
    }

    public async Task<PlanInstanceResponse[]> GetClientPlansAsync(int clientProfileId)
    {
        var plans = await _context.PlanInstances
            .Include(pi => pi.ExerciseInstances)
            .ThenInclude(ei => ei.ProgressEvents)
            .Where(pi => pi.ClientProfileId == clientProfileId)
            .OrderByDescending(pi => pi.CreatedAt)
            .ToListAsync();

        return plans.Select(MapPlanInstanceToResponse).ToArray();
    }

    public async Task<BoardResponse> GetClientBoardAsync(int clientProfileId, DateOnly weekStart)
    {
        var weekEnd = weekStart.AddDays(6);

        // Get exercise instances for this week
        var exerciseInstances = await _context.ExerciseInstances
            .Include(ei => ei.Exercise)
            .Include(ei => ei.PlanInstance)
            .Include(ei => ei.ProgressEvents)
            .Where(ei => ei.PlanInstance.ClientProfileId == clientProfileId &&
                        ei.PlanInstance.Status == "active")
            .ToListAsync();

        // Group by day of week
        var dayGroups = new Dictionary<int, List<ExerciseInstance>>();
        for (int i = 0; i <= 6; i++)
        {
            dayGroups[i] = exerciseInstances.Where(ei => ei.DayOfWeek == i).ToList();
        }

        // Create day responses
        var days = new List<DayBoardResponse>();
        var dayNames = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        for (int i = 0; i <= 6; i++)
        {
            var dayDate = weekStart.AddDays((i + 1) % 7); // Adjust for Monday start
            var dayExercises = dayGroups[i];

            var exercises = dayExercises.Select(ei =>
            {
                var latestProgress = ei.ProgressEvents.OrderByDescending(pe => pe.LoggedAt).FirstOrDefault();

                return new ExerciseCardResponse(
                    ei.Id,
                    ei.Exercise.Name,
                    ei.Exercise.Description,
                    ei.TargetSets,
                    ei.TargetReps,
                    ei.TargetHoldSeconds,
                    ei.Status,
                    latestProgress?.SetsCompleted,
                    latestProgress?.RepsCompleted,
                    latestProgress?.HoldSecondsCompleted,
                    ei.Notes
                );
            }).ToArray();

            days.Add(new DayBoardResponse(
                dayNames[i],
                dayDate,
                i,
                exercises
            ));
        }

        return new BoardResponse(weekStart, weekEnd, days.ToArray());
    }

    private static TemplateResponse MapTemplateToResponse(PlanTemplate template)
    {
        var items = template.PlanTemplateItems
            .OrderBy(pti => pti.OrderIndex)
            .Select(pti =>
            {
                var days = !string.IsNullOrEmpty(pti.DaysOfWeek)
                    ? JsonSerializer.Deserialize<string[]>(pti.DaysOfWeek)
                    : null;

                return new TemplateItemResponse(
                    pti.Id,
                    pti.Exercise.Name,
                    pti.Exercise.Description,
                    pti.Exercise.Category,
                    pti.Sets,
                    pti.Reps,
                    pti.HoldSeconds,
                    pti.FrequencyPerWeek,
                    days,
                    pti.Notes
                );
            }).ToArray();

        return new TemplateResponse(
            template.Id,
            template.Name,
            template.Description,
            template.Category,
            template.DurationWeeks,
            template.IsPublic,
            template.CreatedAt,
            template.UpdatedAt,
            items
        );
    }

    private static ProposalResponse MapProposalToResponse(PlanProposal proposal)
    {
        var items = Array.Empty<ProposalItemResponse>();

        if (!string.IsNullOrEmpty(proposal.CustomPlanJson))
        {
            var proposalItems = JsonSerializer.Deserialize<dynamic[]>(proposal.CustomPlanJson)!;
            items = proposalItems.Select(item =>
            {
                var itemJson = (JsonElement)item;
                var daysJson = itemJson.TryGetProperty("DaysOfWeek", out var daysElement) ? daysElement.GetString() : null;
                var days = !string.IsNullOrEmpty(daysJson) ? JsonSerializer.Deserialize<string[]>(daysJson) : null;

                return new ProposalItemResponse(
                    itemJson.GetProperty("ExerciseId").GetInt32(),
                    itemJson.GetProperty("ExerciseName").GetString()!,
                    itemJson.TryGetProperty("ExerciseDescription", out var descElement) ? descElement.GetString() : null,
                    itemJson.TryGetProperty("Sets", out var setsElement) ? setsElement.GetInt32() : null,
                    itemJson.TryGetProperty("Reps", out var repsElement) ? repsElement.GetInt32() : null,
                    itemJson.TryGetProperty("HoldSeconds", out var holdElement) ? holdElement.GetInt32() : null,
                    days,
                    itemJson.TryGetProperty("Notes", out var notesElement) ? notesElement.GetString() : null
                );
            }).ToArray();
        }

        return new ProposalResponse(
            proposal.Id,
            proposal.TrainerProfile.FullName ?? "Unknown Trainer",
            proposal.ClientProfile.Alias ?? "Unknown",
            proposal.ProposalName,
            proposal.Message,
            proposal.Status,
            proposal.ProposedAt,
            proposal.ExpiresAt,
            proposal.RespondedAt,
            proposal.StartsOn,
            items
        );
    }

    private static PlanInstanceResponse MapPlanInstanceToResponse(PlanInstance plan)
    {
        var totalExercises = plan.ExerciseInstances.Count;
        var completedExercises = plan.ExerciseInstances.Count(ei => ei.Status == "done");

        return new PlanInstanceResponse(
            plan.Id,
            plan.Name,
            plan.Status,
            plan.StartDate,
            plan.PlannedEndDate,
            plan.ActualEndDate,
            plan.CreatedAt,
            totalExercises,
            completedExercises
        );
    }

    private static DateOnly GetNextMonday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7; // If today is Monday, get next Monday
        return today.AddDays(daysUntilMonday);
    }

    private static int GetDayOfWeekNumber(string dayName)
    {
        return dayName.ToLower() switch
        {
            "sunday" => 0,
            "monday" => 1,
            "tuesday" => 2,
            "wednesday" => 3,
            "thursday" => 4,
            "friday" => 5,
            "saturday" => 6,
            _ => 1 // Default to Monday
        };
    }
}