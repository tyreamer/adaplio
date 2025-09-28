using Adaplio.Frontend.Components.Progress;
using Adaplio.Frontend.Models;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Xunit;

namespace Adaplio.Frontend.Tests.Components.Progress;

public class ProgressLadderTests : TestContext
{
    private readonly Mock<IJSRuntime> _mockJSRuntime;

    public ProgressLadderTests()
    {
        _mockJSRuntime = new Mock<IJSRuntime>();
        Services.AddSingleton(_mockJSRuntime.Object);
    }

    [Fact]
    public void ProgressLadder_ShouldRenderEmptyState_WhenNoTiers()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 0,
            BreakEven = 30,
            Tiers = new List<ProgressTier>()
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var emptyState = component.Find(".empty-state");
        Assert.NotNull(emptyState);
        Assert.Contains("Your ladder appears after your first log", emptyState.TextContent);
    }

    [Fact]
    public void ProgressLadder_ShouldRenderTiers_WhenDataProvided()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 25,
            BreakEven = 30,
            Tiers = new List<ProgressTier>
            {
                new() { Threshold = 10, Label = "Bronze", Reward = new TierReward { Kind = RewardKind.Badge, Value = "Bronze Week" } },
                new() { Threshold = 25, Label = "Silver", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "1.5×" } },
                new() { Threshold = 45, Label = "Gold", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "2×" } }
            }
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var tierRows = component.FindAll(".tier-row");
        Assert.Equal(3, tierRows.Count);

        // Check tier content
        var bronzeTier = tierRows.First(t => t.TextContent.Contains("Bronze"));
        Assert.Contains("10", bronzeTier.TextContent);
        Assert.Contains("Bronze Week", bronzeTier.TextContent);

        var silverTier = tierRows.First(t => t.TextContent.Contains("Silver"));
        Assert.Contains("25", silverTier.TextContent);
        Assert.Contains("1.5×", silverTier.TextContent);
    }

    [Fact]
    public void ProgressLadder_ShouldApplyCorrectTierClasses_BasedOnProgress()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 25,
            BreakEven = 20,
            Tiers = new List<ProgressTier>
            {
                new() { Threshold = 10, Label = "Bronze", Reward = new TierReward { Kind = RewardKind.Badge, Value = "Bronze Week" } },
                new() { Threshold = 25, Label = "Silver", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "1.5×" } },
                new() { Threshold = 45, Label = "Gold", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "2×" } }
            }
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var tierRows = component.FindAll(".tier-row");

        // Bronze tier should be active (25 >= 10)
        var bronzeTier = tierRows.First(t => t.TextContent.Contains("Bronze"));
        Assert.Contains("tier-active", bronzeTier.GetClasses());

        // Silver tier should be active (25 >= 25)
        var silverTier = tierRows.First(t => t.TextContent.Contains("Silver"));
        Assert.Contains("tier-active", silverTier.GetClasses());

        // Gold tier should be next (25 < 45)
        var goldTier = tierRows.First(t => t.TextContent.Contains("Gold"));
        Assert.Contains("tier-next", goldTier.GetClasses());
    }

    [Fact]
    public void ProgressLadder_ShouldRenderProgressMarker_AtCorrectPosition()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 25,
            BreakEven = 30,
            Tiers = new List<ProgressTier>
            {
                new() { Threshold = 50, Label = "Gold", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "2×" } }
            }
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var progressMarker = component.Find(".progress-marker");
        Assert.NotNull(progressMarker);

        var markerValue = component.Find(".marker-value");
        Assert.Equal("25", markerValue.TextContent);

        // Verify marker has correct class based on break-even status
        Assert.Contains("marker-below-break-even", progressMarker.GetClasses());
    }

    [Fact]
    public void ProgressLadder_ShouldRenderBreakEvenMarker_WhenBreakEvenSet()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 20,
            BreakEven = 30,
            Tiers = new List<ProgressTier>
            {
                new() { Threshold = 50, Label = "Gold", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "2×" } }
            }
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var breakEvenMarker = component.Find(".break-even-marker");
        Assert.NotNull(breakEvenMarker);

        var breakEvenPill = component.Find(".break-even-pill");
        Assert.Contains("Break-Even", breakEvenPill.TextContent);
    }

    [Fact]
    public void ProgressLadder_ShouldRenderNextEstimate_WhenProvided()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 20,
            BreakEven = 30,
            Tiers = new List<ProgressTier>
            {
                new() { Threshold = 50, Label = "Gold", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "2×" } }
            },
            NextEstimate = new NextEstimate
            {
                NeededDelta = 30,
                SuggestedAction = "Complete 3 more exercises"
            }
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var nextHint = component.Find(".next-target-hint");
        Assert.NotNull(nextHint);
        Assert.Contains("Complete 3 more exercises", nextHint.TextContent);
        Assert.Contains("+30 to reach next tier", nextHint.TextContent);
    }

    [Fact]
    public void ProgressLadder_ShouldApplyDarkModeClass_WhenIsDarkModeTrue()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 25,
            BreakEven = 30,
            Tiers = new List<ProgressTier>()
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.IsDarkMode, true));

        // Assert
        var ladder = component.Find(".progress-ladder");
        Assert.Contains("dark-mode", ladder.GetClasses());
    }

    [Fact]
    public void ProgressLadder_ShouldApplyCompactClass_WhenIsCompactTrue()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 25,
            BreakEven = 30,
            Tiers = new List<ProgressTier>()
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.IsCompact, true));

        // Assert
        var ladder = component.Find(".progress-ladder");
        Assert.Contains("compact", ladder.GetClasses());
    }

    [Fact]
    public void ProgressLadder_ShouldHaveCorrectAriaAttributes()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 25,
            BreakEven = 30,
            Tiers = new List<ProgressTier>
            {
                new() { Threshold = 50, Label = "Gold", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "2×" } }
            }
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var ladder = component.Find(".progress-ladder");
        Assert.Equal("progressbar", ladder.GetAttribute("role"));
        Assert.Equal("25", ladder.GetAttribute("aria-valuenow"));
        Assert.Equal("0", ladder.GetAttribute("aria-valuemin"));
        Assert.Equal("50", ladder.GetAttribute("aria-valuemax"));
        Assert.Equal("Weekly progress ladder", ladder.GetAttribute("aria-label"));
    }

    [Fact]
    public void ProgressLadder_ShouldCalculateCorrectPositions()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 25,
            BreakEven = 30,
            Tiers = new List<ProgressTier>
            {
                new() { Threshold = 50, Label = "Gold", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "2×" } }
            }
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert - test the position calculation method indirectly
        var progressMarker = component.Find(".progress-marker");
        var style = progressMarker.GetAttribute("style");

        // The marker should be positioned somewhere between 0% and 100%
        Assert.Contains("top:", style);
        Assert.Contains("%", style);
    }

    [Fact]
    public async Task ProgressLadder_ShouldInvokeOnTierClick_WhenTierClicked()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 25,
            BreakEven = 30,
            Tiers = new List<ProgressTier>
            {
                new() { Threshold = 50, Label = "Gold", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "2×" } }
            }
        };

        ProgressTier? clickedTier = null;
        var onTierClick = EventCallback.Factory.Create<ProgressTier>(this, tier => clickedTier = tier);

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data)
            .Add(p => p.OnTierClick, onTierClick));

        var tierRow = component.Find(".tier-row");
        await tierRow.ClickAsync();

        // Assert
        Assert.NotNull(clickedTier);
        Assert.Equal("Gold", clickedTier.Label);
        Assert.Equal(50, clickedTier.Threshold);
    }

    [Fact]
    public async Task ProgressLadder_ShouldCallJSInterop_OnAfterRender()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 25,
            BreakEven = 30,
            Tiers = new List<ProgressTier>
            {
                new() { Threshold = 50, Label = "Gold", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "2×" } }
            }
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data));

        // Allow component to complete its lifecycle
        await Task.Delay(100);

        // Assert
        _mockJSRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
            "progressLadder.initialize",
            It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public void ProgressLadder_ShouldReturnCorrectRewardIcon_ForDifferentRewardKinds()
    {
        // Arrange
        var data = new ProgressLadderInput
        {
            CurrentValue = 50,
            BreakEven = 30,
            Tiers = new List<ProgressTier>
            {
                new() { Threshold = 10, Label = "Multiplier", Reward = new TierReward { Kind = RewardKind.Multiplier, Value = "2×" } },
                new() { Threshold = 25, Label = "Badge", Reward = new TierReward { Kind = RewardKind.Badge, Value = "Gold Badge" } },
                new() { Threshold = 45, Label = "Perk", Reward = new TierReward { Kind = RewardKind.Perk, Value = "Special Perk" } }
            }
        };

        // Act
        var component = RenderComponent<ProgressLadder>(parameters => parameters
            .Add(p => p.Data, data));

        // Assert
        var rewardIcons = component.FindAll(".reward-icon");
        Assert.Equal(3, rewardIcons.Count);

        // Check that different reward kinds get different icons
        var iconTexts = rewardIcons.Select(i => i.TextContent).ToList();
        Assert.Contains("⚡", iconTexts); // Multiplier
        Assert.Contains("🏆", iconTexts); // Badge
        Assert.Contains("🎁", iconTexts); // Perk
    }
}