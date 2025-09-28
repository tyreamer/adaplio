using System.ComponentModel.DataAnnotations;

namespace Adaplio.Frontend.Models;

public class ProgressLadderInput
{
    public ProgressUnit Unit { get; set; } = ProgressUnit.Xp;
    public int CurrentValue { get; set; }
    public int BreakEven { get; set; }
    public List<ProgressTier> Tiers { get; set; } = new();
    public NextEstimate? NextEstimate { get; set; }
}

public class ProgressTier
{
    public int Threshold { get; set; }
    public string? Label { get; set; }
    public TierReward Reward { get; set; } = new();
}

public class TierReward
{
    public RewardKind Kind { get; set; }
    public string Value { get; set; } = "";
}

public class NextEstimate
{
    public int NeededDelta { get; set; }
    public string SuggestedAction { get; set; } = "";
}

public enum ProgressUnit
{
    Xp,
    Percent
}

public enum RewardKind
{
    Multiplier,
    Badge,
    Perk
}

public class WeeklyProgressResponse
{
    public ProgressUnit Unit { get; set; }
    public int CurrentValue { get; set; }
    public int BreakEven { get; set; }
    public List<ProgressTier> Tiers { get; set; } = new();
    public NextEstimate? NextEstimate { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public bool HasCelebration { get; set; }
    public string? CelebrationMessage { get; set; }
}

public class ProgressLadderState
{
    public int CurrentValue { get; set; }
    public int PreviousValue { get; set; }
    public bool IsAnimating { get; set; }
    public int? NewTierThreshold { get; set; }
    public DateTime LastUpdate { get; set; } = DateTime.UtcNow;
}