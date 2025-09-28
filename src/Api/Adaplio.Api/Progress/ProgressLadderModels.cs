namespace Adaplio.Api.Progress;

public class WeeklyProgressRequest
{
    public DateTime? WeekStart { get; set; }
}

public class WeeklyProgressResponse
{
    public string Unit { get; set; } = "xp";
    public int CurrentValue { get; set; }
    public int BreakEven { get; set; }
    public List<ProgressTier> Tiers { get; set; } = new();
    public NextEstimate? NextEstimate { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public bool HasCelebration { get; set; }
    public string? CelebrationMessage { get; set; }
}

public class ProgressTier
{
    public int Threshold { get; set; }
    public string? Label { get; set; }
    public TierReward Reward { get; set; } = new();
}

public class TierReward
{
    public string Kind { get; set; } = "multiplier";
    public string Value { get; set; } = "";
}

public class NextEstimate
{
    public int NeededDelta { get; set; }
    public string SuggestedAction { get; set; } = "";
}