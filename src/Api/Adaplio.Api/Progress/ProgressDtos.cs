using System.ComponentModel.DataAnnotations;

namespace Adaplio.Api.Progress;

// Progress logging request
public record LogProgressRequest(
    [Required] int ExerciseInstanceId,
    [Required] string EventType, // "exercise_completed", "set_completed"
    int? SetsCompleted,
    int? RepsCompleted,
    int? HoldSecondsCompleted,
    int? DifficultyRating, // 1-10 scale
    int? PainLevel, // 1-10 scale
    string? Notes
);

public record LogProgressResponse(
    string Message,
    int ProgressEventId
);

// Adherence summary responses
public record WeeklyAdherence(
    int Year,
    int WeekNumber,
    DateOnly WeekStartDate,
    decimal AdherencePercentage,
    int CompletedCount,
    int PlannedCount
);

public record ClientAdherenceSummaryResponse(
    string ClientAlias,
    WeeklyAdherence[] WeeklyData,
    decimal OverallAdherence
);

public record TrainerClientAdherenceResponse(
    string ClientAlias,
    WeeklyAdherence[] RecentWeeks,
    decimal CurrentWeekAdherence,
    decimal OverallAdherence
);