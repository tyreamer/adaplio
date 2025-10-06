using Adaplio.Api.Domain;

namespace Adaplio.Api.Tests.Helpers;

public static class TestDataBuilder
{
    public static ClientProfile CreateClientProfile(
        int id = 1,
        int userId = 100,
        string alias = "C-TEST",
        string email = "client@test.com")
    {
        return new ClientProfile
        {
            Id = id,
            UserId = userId,
            Alias = alias,
            Email = email,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static TrainerProfile CreateTrainerProfile(
        int id = 1,
        int userId = 200,
        string alias = "T-TEST",
        string email = "trainer@test.com")
    {
        return new TrainerProfile
        {
            Id = id,
            UserId = userId,
            Alias = alias,
            Email = email,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Exercise CreateExercise(
        int id = 1,
        string name = "Test Exercise",
        string? category = "Strength")
    {
        return new Exercise
        {
            Id = id,
            Name = name,
            Category = category,
            Instructions = "Test instructions",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static PlanTemplate CreatePlanTemplate(
        int id = 1,
        int trainerProfileId = 1,
        string name = "Test Template")
    {
        return new PlanTemplate
        {
            Id = id,
            TrainerProfileId = trainerProfileId,
            Name = name,
            Description = "Test template description",
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static PlanTemplateItem CreatePlanTemplateItem(
        int id = 1,
        int planTemplateId = 1,
        int exerciseId = 1)
    {
        return new PlanTemplateItem
        {
            Id = id,
            PlanTemplateId = planTemplateId,
            ExerciseId = exerciseId,
            TargetSets = 3,
            TargetReps = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static PlanInstance CreatePlanInstance(
        int id = 1,
        int clientProfileId = 1,
        string name = "Test Plan")
    {
        return new PlanInstance
        {
            Id = id,
            ClientProfileId = clientProfileId,
            Name = name,
            Status = "active",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static ExerciseInstance CreateExerciseInstance(
        int id = 1,
        int planInstanceId = 1,
        int exerciseId = 1,
        int clientProfileId = 1)
    {
        return new ExerciseInstance
        {
            Id = id,
            PlanInstanceId = planInstanceId,
            ExerciseId = exerciseId,
            ClientProfileId = clientProfileId,
            Status = "active",
            TargetSets = 3,
            TargetReps = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static ProgressEvent CreateProgressEvent(
        int id = 1,
        int clientProfileId = 1,
        int? exerciseInstanceId = null,
        string eventType = "exercise_completed")
    {
        return new ProgressEvent
        {
            Id = id,
            ClientProfileId = clientProfileId,
            ExerciseInstanceId = exerciseInstanceId,
            EventType = eventType,
            LoggedAt = DateTimeOffset.UtcNow
        };
    }

    public static Domain.Gamification CreateGamification(
        int id = 1,
        int clientProfileId = 1,
        int totalXp = 0,
        int level = 1)
    {
        return new Domain.Gamification
        {
            Id = id,
            ClientProfileId = clientProfileId,
            TotalXp = totalXp,
            Level = level,
            CurrentStreakDays = 0,
            LongestStreakDays = 0,
            WeeklyStreakWeeks = 0,
            LongestWeeklyStreak = 0,
            LastActivityDate = DateOnly.FromDateTime(DateTime.Today),
            Badges = "[]",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static XpAward CreateXpAward(
        int id = 1,
        int userId = 1,
        int? progressEventId = null,
        int xpAwarded = 25)
    {
        return new XpAward
        {
            Id = id,
            UserId = userId,
            ProgressEventId = progressEventId,
            XpAwarded = xpAwarded,
            Reason = "Test XP award",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static AdherenceWeek CreateAdherenceWeek(
        int id = 1,
        int clientProfileId = 1,
        DateOnly? weekStartDate = null)
    {
        var startDate = weekStartDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1));

        return new AdherenceWeek
        {
            Id = id,
            ClientProfileId = clientProfileId,
            Year = startDate.Year,
            WeekNumber = ISOWeek.GetWeekOfYear(startDate.ToDateTime(TimeOnly.MinValue)),
            WeekStartDate = startDate,
            TotalExercisesPlanned = 10,
            TotalExercisesCompleted = 7,
            AdherencePercentage = 0.70m,
            CalculatedAt = DateTimeOffset.UtcNow
        };
    }

    public static PlanProposal CreatePlanProposal(
        int id = 1,
        int trainerProfileId = 1,
        int clientProfileId = 1,
        string status = "pending")
    {
        return new PlanProposal
        {
            Id = id,
            TrainerProfileId = trainerProfileId,
            ClientProfileId = clientProfileId,
            Status = status,
            Description = "Test proposal",
            StartsOn = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public static ConsentGrant CreateConsentGrant(
        int id = 1,
        int trainerProfileId = 1,
        int clientProfileId = 1)
    {
        return new ConsentGrant
        {
            Id = id,
            TrainerProfileId = trainerProfileId,
            ClientProfileId = clientProfileId,
            GrantedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddYears(1),
            IsActive = true
        };
    }

    public static MagicLink CreateMagicLink(
        int id = 1,
        string email = "test@test.com",
        string token = "123456")
    {
        return new MagicLink
        {
            Id = id,
            Email = email,
            Token = token,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
