using Adaplio.Api.Services;
using Adaplio.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Adaplio.Api.Tests.Services;

public class ProgressServiceTests : DatabaseTestBase
{
    private readonly ProgressService _progressService;

    public ProgressServiceTests()
    {
        _progressService = new ProgressService(Context);
    }

    [Fact]
    public async Task GetClientAdherenceAsync_ShouldReturnWeeklyAdherence()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var week1 = TestDataBuilder.CreateAdherenceWeek(
            id: 1,
            clientProfileId: client.Id,
            weekStartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-14)));
        week1.TotalExercisesPlanned = 10;
        week1.TotalExercisesCompleted = 8;
        week1.AdherencePercentage = 0.80m;

        var week2 = TestDataBuilder.CreateAdherenceWeek(
            id: 2,
            clientProfileId: client.Id,
            weekStartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-7)));
        week2.TotalExercisesPlanned = 12;
        week2.TotalExercisesCompleted = 10;
        week2.AdherencePercentage = 0.83m;

        Context.ClientProfiles.Add(client);
        Context.AdherenceWeeks.AddRange(week1, week2);
        await SaveChangesAsync();

        // Act
        var result = await _progressService.GetClientAdherenceAsync(client.Id);

        // Assert
        result.Should().HaveCount(2);
        result[0].AdherencePercentage.Should().Be(0.83m); // Most recent first
        result[1].AdherencePercentage.Should().Be(0.80m);
    }

    [Fact]
    public async Task GetClientAdherenceAsync_ShouldLimitResultsWhenSpecified()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        for (int i = 0; i < 10; i++)
        {
            var week = TestDataBuilder.CreateAdherenceWeek(
                id: i + 1,
                clientProfileId: client.Id,
                weekStartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(-7 * i)));
            Context.AdherenceWeeks.Add(week);
        }

        Context.ClientProfiles.Add(client);
        await SaveChangesAsync();

        // Act
        var result = await _progressService.GetClientAdherenceAsync(client.Id, weeks: 4);

        // Assert
        result.Should().HaveCount(4);
    }

    [Fact]
    public async Task CalculateOverallAdherenceAsync_ShouldReturnAverageAdherence()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var week1 = TestDataBuilder.CreateAdherenceWeek(id: 1, clientProfileId: client.Id);
        week1.AdherencePercentage = 0.80m;

        var week2 = TestDataBuilder.CreateAdherenceWeek(id: 2, clientProfileId: client.Id);
        week2.AdherencePercentage = 0.90m;

        var week3 = TestDataBuilder.CreateAdherenceWeek(id: 3, clientProfileId: client.Id);
        week3.AdherencePercentage = 0.70m;

        Context.ClientProfiles.Add(client);
        Context.AdherenceWeeks.AddRange(week1, week2, week3);
        await SaveChangesAsync();

        // Act
        var result = await _progressService.CalculateOverallAdherenceAsync(client.Id);

        // Assert
        result.Should().Be(0.80m); // (0.80 + 0.90 + 0.70) / 3
    }

    [Fact]
    public async Task CalculateOverallAdherenceAsync_ShouldReturnZero_WhenNoWeeks()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        Context.ClientProfiles.Add(client);
        await SaveChangesAsync();

        // Act
        var result = await _progressService.CalculateOverallAdherenceAsync(client.Id);

        // Assert
        result.Should().Be(0m);
    }

    [Fact]
    public async Task UpdateAdherenceWeekAsync_ShouldCreateOrUpdateWeek()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var exercise = TestDataBuilder.CreateExercise();
        var plan = TestDataBuilder.CreatePlanInstance(clientProfileId: client.Id);
        var instance = TestDataBuilder.CreateExerciseInstance(
            planInstanceId: plan.Id,
            exerciseId: exercise.Id,
            clientProfileId: client.Id);
        instance.FrequencyPerWeek = 3;

        var weekStart = DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1));

        // Add progress events for the week
        var event1 = TestDataBuilder.CreateProgressEvent(
            clientProfileId: client.Id,
            exerciseInstanceId: instance.Id,
            eventType: "exercise_completed");
        event1.LoggedAt = weekStart.ToDateTime(TimeOnly.MinValue).AddDays(1);

        Context.ClientProfiles.Add(client);
        Context.Exercises.Add(exercise);
        Context.PlanInstances.Add(plan);
        Context.ExerciseInstances.Add(instance);
        Context.ProgressEvents.Add(event1);
        await SaveChangesAsync();

        // Act
        await _progressService.UpdateAdherenceWeekAsync(client.Id, weekStart);

        // Assert
        var adherenceWeek = await Context.AdherenceWeeks
            .FirstOrDefaultAsync(aw => aw.ClientProfileId == client.Id && aw.WeekStartDate == weekStart);

        adherenceWeek.Should().NotBeNull();
        adherenceWeek!.TotalExercisesPlanned.Should().Be(3);
        adherenceWeek.TotalExercisesCompleted.Should().Be(1);
        adherenceWeek.AdherencePercentage.Should().BeApproximately(0.33m, 0.01m);
    }

    [Fact]
    public async Task UpdateAdherenceWeekAsync_ShouldUpdateExistingWeek()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var weekStart = DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1));
        var existingWeek = TestDataBuilder.CreateAdherenceWeek(
            clientProfileId: client.Id,
            weekStartDate: weekStart);
        existingWeek.TotalExercisesPlanned = 5;
        existingWeek.TotalExercisesCompleted = 2;
        existingWeek.AdherencePercentage = 0.40m;

        Context.ClientProfiles.Add(client);
        Context.AdherenceWeeks.Add(existingWeek);
        await SaveChangesAsync();

        // Act
        await _progressService.UpdateAdherenceWeekAsync(client.Id, weekStart);

        // Assert
        var updatedWeek = await Context.AdherenceWeeks
            .FirstOrDefaultAsync(aw => aw.Id == existingWeek.Id);

        updatedWeek.Should().NotBeNull();
        updatedWeek!.CalculatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetClientAdherenceAsync_ShouldHandleDateOnlyProperly()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var today = DateTime.Today;
        var weekStart = DateOnly.FromDateTime(today.AddDays(-(int)today.DayOfWeek + 1));

        var week = TestDataBuilder.CreateAdherenceWeek(
            clientProfileId: client.Id,
            weekStartDate: weekStart);

        Context.ClientProfiles.Add(client);
        Context.AdherenceWeeks.Add(week);
        await SaveChangesAsync();

        // Act
        var result = await _progressService.GetClientAdherenceAsync(client.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].WeekStartDate.Should().Be(weekStart);
        result[0].WeekStartDate.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public async Task GetClientAdherenceAsync_ShouldOrderByMostRecent()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();

        var week1 = TestDataBuilder.CreateAdherenceWeek(
            id: 1,
            clientProfileId: client.Id,
            weekStartDate: DateOnly.FromDateTime(new DateTime(2024, 1, 1)));
        week1.Year = 2024;
        week1.WeekNumber = 1;

        var week2 = TestDataBuilder.CreateAdherenceWeek(
            id: 2,
            clientProfileId: client.Id,
            weekStartDate: DateOnly.FromDateTime(new DateTime(2024, 1, 8)));
        week2.Year = 2024;
        week2.WeekNumber = 2;

        var week3 = TestDataBuilder.CreateAdherenceWeek(
            id: 3,
            clientProfileId: client.Id,
            weekStartDate: DateOnly.FromDateTime(new DateTime(2024, 1, 15)));
        week3.Year = 2024;
        week3.WeekNumber = 3;

        Context.ClientProfiles.Add(client);
        Context.AdherenceWeeks.AddRange(week1, week2, week3);
        await SaveChangesAsync();

        // Act
        var result = await _progressService.GetClientAdherenceAsync(client.Id);

        // Assert
        result.Should().HaveCount(3);
        result[0].WeekNumber.Should().Be(3);
        result[1].WeekNumber.Should().Be(2);
        result[2].WeekNumber.Should().Be(1);
    }
}
