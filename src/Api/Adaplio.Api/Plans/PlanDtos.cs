using System.ComponentModel.DataAnnotations;

namespace Adaplio.Api.Plans;

// Template DTOs
public record CreateTemplateRequest(
    [Required] string Name,
    string? Description,
    string? Category,
    int? DurationWeeks,
    bool IsPublic,
    [Required] TemplateItemRequest[] Items
);

public record UpdateTemplateRequest(
    [Required] string Name,
    string? Description,
    string? Category,
    int? DurationWeeks,
    bool IsPublic,
    [Required] TemplateItemRequest[] Items
);

public record TemplateItemRequest(
    [Required] string ExerciseName,
    string? ExerciseDescription,
    string? ExerciseCategory,
    int? TargetSets,
    int? TargetReps,
    int? HoldSeconds,
    int? FrequencyPerWeek,
    string[]? Days, // ["Monday", "Wednesday", "Friday"]
    string? Notes
);

public record TemplateResponse(
    int Id,
    string Name,
    string? Description,
    string? Category,
    int? DurationWeeks,
    bool IsPublic,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    TemplateItemResponse[] Items
);

public record TemplateItemResponse(
    int Id,
    string ExerciseName,
    string? ExerciseDescription,
    string? ExerciseCategory,
    int? TargetSets,
    int? TargetReps,
    int? HoldSeconds,
    int? FrequencyPerWeek,
    string[]? Days,
    string? Notes
);

public record TemplateListResponse(
    TemplateResponse[] Templates
);

// Proposal DTOs
public record CreateProposalRequest(
    [Required] string ClientAlias,
    [Required] int TemplateId,
    DateOnly? StartsOn,
    string? Message
);

public record ProposalResponse(
    int Id,
    string TrainerName,
    string ClientAlias,
    string ProposalName,
    string? Message,
    string Status,
    DateTimeOffset ProposedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RespondedAt,
    DateOnly? StartsOn,
    ProposalItemResponse[] Items
);

public record ProposalItemResponse(
    int Id,
    string ExerciseName,
    string? ExerciseDescription,
    int? TargetSets,
    int? TargetReps,
    int? HoldSeconds,
    string[]? Days,
    string? Notes
);

public record ProposalListResponse(
    ProposalResponse[] Proposals
);

// Acceptance DTOs
public record AcceptProposalRequest(
    bool? AcceptAll,
    int[]? AcceptItemIds
);

public record AcceptProposalResponse(
    string Message,
    int PlanInstanceId,
    int AcceptedItems,
    int TotalItems
);

// Plan Instance DTOs
public record PlanInstanceResponse(
    int Id,
    string Name,
    string Status,
    DateOnly StartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualEndDate,
    DateTimeOffset CreatedAt,
    int TotalExercises,
    int CompletedExercises
);

public record PlanListResponse(
    PlanInstanceResponse[] Plans
);

// Board DTOs
public record BoardRequest(
    DateOnly WeekStart
);

public record BoardResponse(
    DateOnly WeekStart,
    DateOnly WeekEnd,
    DayBoardResponse[] Days
);

public record DayBoardResponse(
    string DayName,
    DateOnly Date,
    int DayOfWeek,
    ExerciseCardResponse[] Exercises
);

public record ExerciseCardResponse(
    int ExerciseInstanceId,
    string ExerciseName,
    string? ExerciseDescription,
    int? TargetSets,
    int? TargetReps,
    int? HoldSeconds,
    string Status, // planned, done, partial, skipped
    int? CompletedSets,
    int? CompletedReps,
    int? CompletedHoldSeconds,
    string? Notes
);

// Quick log DTO
public record QuickLogRequest(
    [Required] int ExerciseInstanceId,
    [Required] bool Completed,
    int? Reps,
    int? HoldSeconds
);

public record QuickLogResponse(
    string Message,
    int ProgressEventId
);