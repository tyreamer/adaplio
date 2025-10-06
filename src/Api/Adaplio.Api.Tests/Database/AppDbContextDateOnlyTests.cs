using Adaplio.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Adaplio.Api.Tests.Database;

public class AppDbContextDateOnlyTests : DatabaseTestBase
{
    [Fact]
    public async Task PlanInstance_ShouldPersistDateOnlyFields()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var startDate = DateOnly.FromDateTime(new DateTime(2024, 1, 15));
        var plannedEndDate = DateOnly.FromDateTime(new DateTime(2024, 3, 15));

        var plan = TestDataBuilder.CreatePlanInstance(clientProfileId: client.Id);
        plan.StartDate = startDate;
        plan.PlannedEndDate = plannedEndDate;
        plan.ActualEndDate = null;

        Context.ClientProfiles.Add(client);
        Context.PlanInstances.Add(plan);
        await SaveChangesAsync();

        // Act
        var savedPlan = await Context.PlanInstances
            .FirstOrDefaultAsync(p => p.Id == plan.Id);

        // Assert
        savedPlan.Should().NotBeNull();
        savedPlan!.StartDate.Should().Be(startDate);
        savedPlan.StartDate.Year.Should().Be(2024);
        savedPlan.StartDate.Month.Should().Be(1);
        savedPlan.StartDate.Day.Should().Be(15);

        savedPlan.PlannedEndDate.Should().Be(plannedEndDate);
        savedPlan.PlannedEndDate!.Value.Year.Should().Be(2024);
        savedPlan.PlannedEndDate.Value.Month.Should().Be(3);
        savedPlan.PlannedEndDate.Value.Day.Should().Be(15);

        savedPlan.ActualEndDate.Should().BeNull();
    }

    [Fact]
    public async Task PlanInstance_ShouldHandleActualEndDate()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var plan = TestDataBuilder.CreatePlanInstance(clientProfileId: client.Id);
        var actualEndDate = DateOnly.FromDateTime(new DateTime(2024, 2, 28));
        plan.ActualEndDate = actualEndDate;

        Context.ClientProfiles.Add(client);
        Context.PlanInstances.Add(plan);
        await SaveChangesAsync();

        // Act
        var savedPlan = await Context.PlanInstances.FindAsync(plan.Id);

        // Assert
        savedPlan!.ActualEndDate.Should().NotBeNull();
        savedPlan.ActualEndDate.Should().Be(actualEndDate);
    }

    [Fact]
    public async Task AdherenceWeek_ShouldPersistWeekStartDate()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var weekStart = DateOnly.FromDateTime(new DateTime(2024, 1, 1)); // Monday

        var adherence = TestDataBuilder.CreateAdherenceWeek(
            clientProfileId: client.Id,
            weekStartDate: weekStart);

        Context.ClientProfiles.Add(client);
        Context.AdherenceWeeks.Add(adherence);
        await SaveChangesAsync();

        // Act
        var saved = await Context.AdherenceWeeks.FindAsync(adherence.Id);

        // Assert
        saved.Should().NotBeNull();
        saved!.WeekStartDate.Should().Be(weekStart);
        saved.WeekStartDate.Year.Should().Be(2024);
        saved.WeekStartDate.Month.Should().Be(1);
        saved.WeekStartDate.Day.Should().Be(1);
        saved.WeekStartDate.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public async Task Gamification_ShouldPersistLastActivityDate()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var lastActivity = DateOnly.FromDateTime(new DateTime(2024, 6, 15));

        var gamification = TestDataBuilder.CreateGamification(clientProfileId: client.Id);
        gamification.LastActivityDate = lastActivity;

        Context.ClientProfiles.Add(client);
        Context.Gamifications.Add(gamification);
        await SaveChangesAsync();

        // Act
        var saved = await Context.Gamifications.FindAsync(gamification.Id);

        // Assert
        saved.Should().NotBeNull();
        saved!.LastActivityDate.Should().NotBeNull();
        saved.LastActivityDate.Should().Be(lastActivity);
        saved.LastActivityDate!.Value.Year.Should().Be(2024);
        saved.LastActivityDate.Value.Month.Should().Be(6);
        saved.LastActivityDate.Value.Day.Should().Be(15);
    }

    [Fact]
    public async Task Gamification_ShouldHandleNullLastActivityDate()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var gamification = TestDataBuilder.CreateGamification(clientProfileId: client.Id);
        gamification.LastActivityDate = null;

        Context.ClientProfiles.Add(client);
        Context.Gamifications.Add(gamification);
        await SaveChangesAsync();

        // Act
        var saved = await Context.Gamifications.FindAsync(gamification.Id);

        // Assert
        saved.Should().NotBeNull();
        saved!.LastActivityDate.Should().BeNull();
    }

    [Fact]
    public async Task PlanProposal_ShouldPersistStartsOnDate()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile();
        var client = TestDataBuilder.CreateClientProfile();
        var startsOn = DateOnly.FromDateTime(new DateTime(2024, 7, 1));

        var proposal = TestDataBuilder.CreatePlanProposal(
            trainerProfileId: trainer.Id,
            clientProfileId: client.Id);
        proposal.StartsOn = startsOn;

        Context.TrainerProfiles.Add(trainer);
        Context.ClientProfiles.Add(client);
        Context.PlanProposals.Add(proposal);
        await SaveChangesAsync();

        // Act
        var saved = await Context.PlanProposals.FindAsync(proposal.Id);

        // Assert
        saved.Should().NotBeNull();
        saved!.StartsOn.Should().NotBeNull();
        saved.StartsOn.Should().Be(startsOn);
        saved.StartsOn!.Value.Year.Should().Be(2024);
        saved.StartsOn.Value.Month.Should().Be(7);
        saved.StartsOn.Value.Day.Should().Be(1);
    }

    [Fact]
    public async Task PlanProposal_ShouldHandleNullStartsOn()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile();
        var client = TestDataBuilder.CreateClientProfile();
        var proposal = TestDataBuilder.CreatePlanProposal(
            trainerProfileId: trainer.Id,
            clientProfileId: client.Id);
        proposal.StartsOn = null;

        Context.TrainerProfiles.Add(trainer);
        Context.ClientProfiles.Add(client);
        Context.PlanProposals.Add(proposal);
        await SaveChangesAsync();

        // Act
        var saved = await Context.PlanProposals.FindAsync(proposal.Id);

        // Assert
        saved.Should().NotBeNull();
        saved!.StartsOn.Should().BeNull();
    }

    [Fact]
    public async Task DateOnly_ShouldSupportQuerying()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var targetDate = DateOnly.FromDateTime(new DateTime(2024, 6, 15));

        var plan1 = TestDataBuilder.CreatePlanInstance(id: 1, clientProfileId: client.Id);
        plan1.StartDate = DateOnly.FromDateTime(new DateTime(2024, 6, 10));

        var plan2 = TestDataBuilder.CreatePlanInstance(id: 2, clientProfileId: client.Id);
        plan2.StartDate = targetDate;

        var plan3 = TestDataBuilder.CreatePlanInstance(id: 3, clientProfileId: client.Id);
        plan3.StartDate = DateOnly.FromDateTime(new DateTime(2024, 6, 20));

        Context.ClientProfiles.Add(client);
        Context.PlanInstances.AddRange(plan1, plan2, plan3);
        await SaveChangesAsync();

        // Act - Query for plans starting on specific date
        var result = await Context.PlanInstances
            .Where(p => p.StartDate == targetDate)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(plan2.Id);
    }

    [Fact]
    public async Task DateOnly_ShouldSupportRangeQuerying()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var rangeStart = DateOnly.FromDateTime(new DateTime(2024, 6, 10));
        var rangeEnd = DateOnly.FromDateTime(new DateTime(2024, 6, 20));

        var plan1 = TestDataBuilder.CreatePlanInstance(id: 1, clientProfileId: client.Id);
        plan1.StartDate = DateOnly.FromDateTime(new DateTime(2024, 6, 5)); // Before range

        var plan2 = TestDataBuilder.CreatePlanInstance(id: 2, clientProfileId: client.Id);
        plan2.StartDate = DateOnly.FromDateTime(new DateTime(2024, 6, 15)); // In range

        var plan3 = TestDataBuilder.CreatePlanInstance(id: 3, clientProfileId: client.Id);
        plan3.StartDate = DateOnly.FromDateTime(new DateTime(2024, 6, 25)); // After range

        Context.ClientProfiles.Add(client);
        Context.PlanInstances.AddRange(plan1, plan2, plan3);
        await SaveChangesAsync();

        // Act
        var result = await Context.PlanInstances
            .Where(p => p.StartDate >= rangeStart && p.StartDate <= rangeEnd)
            .ToListAsync();

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(plan2.Id);
    }

    [Fact]
    public async Task AdherenceWeek_ShouldQueryByWeekStartDate()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var week1Start = DateOnly.FromDateTime(new DateTime(2024, 1, 1));
        var week2Start = DateOnly.FromDateTime(new DateTime(2024, 1, 8));

        var adherence1 = TestDataBuilder.CreateAdherenceWeek(id: 1, clientProfileId: client.Id, weekStartDate: week1Start);
        var adherence2 = TestDataBuilder.CreateAdherenceWeek(id: 2, clientProfileId: client.Id, weekStartDate: week2Start);

        Context.ClientProfiles.Add(client);
        Context.AdherenceWeeks.AddRange(adherence1, adherence2);
        await SaveChangesAsync();

        // Act
        var result = await Context.AdherenceWeeks
            .Where(aw => aw.WeekStartDate == week2Start)
            .FirstOrDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(adherence2.Id);
        result.WeekStartDate.Should().Be(week2Start);
    }

    [Fact]
    public async Task MultipleDateOnly_ShouldAllWorkTogether()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var trainer = TestDataBuilder.CreateTrainerProfile();

        var proposalStartDate = DateOnly.FromDateTime(new DateTime(2024, 7, 1));
        var planStartDate = DateOnly.FromDateTime(new DateTime(2024, 7, 1));
        var planEndDate = DateOnly.FromDateTime(new DateTime(2024, 9, 30));
        var weekStartDate = DateOnly.FromDateTime(new DateTime(2024, 7, 1));
        var lastActivityDate = DateOnly.FromDateTime(new DateTime(2024, 7, 15));

        var proposal = TestDataBuilder.CreatePlanProposal(trainerProfileId: trainer.Id, clientProfileId: client.Id);
        proposal.StartsOn = proposalStartDate;

        var plan = TestDataBuilder.CreatePlanInstance(clientProfileId: client.Id);
        plan.StartDate = planStartDate;
        plan.PlannedEndDate = planEndDate;

        var adherence = TestDataBuilder.CreateAdherenceWeek(clientProfileId: client.Id, weekStartDate: weekStartDate);

        var gamification = TestDataBuilder.CreateGamification(clientProfileId: client.Id);
        gamification.LastActivityDate = lastActivityDate;

        Context.TrainerProfiles.Add(trainer);
        Context.ClientProfiles.Add(client);
        Context.PlanProposals.Add(proposal);
        Context.PlanInstances.Add(plan);
        Context.AdherenceWeeks.Add(adherence);
        Context.Gamifications.Add(gamification);
        await SaveChangesAsync();

        // Act - Clear context and reload
        Context.ChangeTracker.Clear();

        var savedProposal = await Context.PlanProposals.FindAsync(proposal.Id);
        var savedPlan = await Context.PlanInstances.FindAsync(plan.Id);
        var savedAdherence = await Context.AdherenceWeeks.FindAsync(adherence.Id);
        var savedGamification = await Context.Gamifications.FindAsync(gamification.Id);

        // Assert
        savedProposal!.StartsOn.Should().Be(proposalStartDate);
        savedPlan!.StartDate.Should().Be(planStartDate);
        savedPlan.PlannedEndDate.Should().Be(planEndDate);
        savedAdherence!.WeekStartDate.Should().Be(weekStartDate);
        savedGamification!.LastActivityDate.Should().Be(lastActivityDate);
    }
}
