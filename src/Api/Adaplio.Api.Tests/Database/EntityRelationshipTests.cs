using Adaplio.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Adaplio.Api.Tests.Database;

public class EntityRelationshipTests : DatabaseTestBase
{
    [Fact]
    public async Task PlanInstance_ShouldLoadExerciseInstances()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var exercise = TestDataBuilder.CreateExercise();
        var plan = TestDataBuilder.CreatePlanInstance(clientProfileId: client.Id);
        var instance1 = TestDataBuilder.CreateExerciseInstance(id: 1, planInstanceId: plan.Id, exerciseId: exercise.Id, clientProfileId: client.Id);
        var instance2 = TestDataBuilder.CreateExerciseInstance(id: 2, planInstanceId: plan.Id, exerciseId: exercise.Id, clientProfileId: client.Id);

        Context.ClientProfiles.Add(client);
        Context.Exercises.Add(exercise);
        Context.PlanInstances.Add(plan);
        Context.ExerciseInstances.AddRange(instance1, instance2);
        await SaveChangesAsync();

        // Act
        var loaded = await Context.PlanInstances
            .Include(p => p.ExerciseInstances)
            .FirstOrDefaultAsync(p => p.Id == plan.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.ExerciseInstances.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConsentGrant_ShouldLinkTrainerAndClient()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile();
        var client = TestDataBuilder.CreateClientProfile();
        var consent = TestDataBuilder.CreateConsentGrant(
            trainerProfileId: trainer.Id,
            clientProfileId: client.Id);

        Context.TrainerProfiles.Add(trainer);
        Context.ClientProfiles.Add(client);
        Context.ConsentGrants.Add(consent);
        await SaveChangesAsync();

        // Act
        var loaded = await Context.ConsentGrants
            .Include(cg => cg.TrainerProfile)
            .Include(cg => cg.ClientProfile)
            .FirstOrDefaultAsync(cg => cg.Id == consent.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.TrainerProfile.Should().NotBeNull();
        loaded.ClientProfile.Should().NotBeNull();
        loaded.TrainerProfileId.Should().Be(trainer.Id);
        loaded.ClientProfileId.Should().Be(client.Id);
    }

    [Fact]
    public async Task ProgressEvent_ShouldLinkToExerciseInstance()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var exercise = TestDataBuilder.CreateExercise();
        var plan = TestDataBuilder.CreatePlanInstance(clientProfileId: client.Id);
        var instance = TestDataBuilder.CreateExerciseInstance(
            planInstanceId: plan.Id,
            exerciseId: exercise.Id,
            clientProfileId: client.Id);
        var progressEvent = TestDataBuilder.CreateProgressEvent(
            clientProfileId: client.Id,
            exerciseInstanceId: instance.Id);

        Context.ClientProfiles.Add(client);
        Context.Exercises.Add(exercise);
        Context.PlanInstances.Add(plan);
        Context.ExerciseInstances.Add(instance);
        Context.ProgressEvents.Add(progressEvent);
        await SaveChangesAsync();

        // Act
        var loaded = await Context.ProgressEvents
            .Include(pe => pe.ExerciseInstance)
            .ThenInclude(ei => ei!.Exercise)
            .FirstOrDefaultAsync(pe => pe.Id == progressEvent.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.ExerciseInstance.Should().NotBeNull();
        loaded.ExerciseInstance!.Exercise.Should().NotBeNull();
        loaded.ExerciseInstance.Exercise.Name.Should().Be(exercise.Name);
    }

    [Fact]
    public async Task Gamification_ShouldBeUniquePerClient()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var gamification = TestDataBuilder.CreateGamification(clientProfileId: client.Id);

        Context.ClientProfiles.Add(client);
        Context.Gamifications.Add(gamification);
        await SaveChangesAsync();

        // Act & Assert
        var duplicateGamification = TestDataBuilder.CreateGamification(clientProfileId: client.Id);
        Context.Gamifications.Add(duplicateGamification);

        // Should throw due to unique constraint on ClientProfileId
        var act = async () => await SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task XpAward_ShouldLinkToProgressEvent()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var progressEvent = TestDataBuilder.CreateProgressEvent(clientProfileId: client.Id);
        var xpAward = TestDataBuilder.CreateXpAward(
            userId: client.Id,
            progressEventId: progressEvent.Id);

        Context.ClientProfiles.Add(client);
        Context.ProgressEvents.Add(progressEvent);
        Context.XpAwards.Add(xpAward);
        await SaveChangesAsync();

        // Act
        var loaded = await Context.XpAwards
            .Include(xa => xa.ProgressEvent)
            .FirstOrDefaultAsync(xa => xa.Id == xpAward.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.ProgressEvent.Should().NotBeNull();
        loaded.ProgressEventId.Should().Be(progressEvent.Id);
    }

    [Fact]
    public async Task PlanTemplate_ShouldLoadItems_WithExercises()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile();
        var exercise1 = TestDataBuilder.CreateExercise(id: 1, name: "Exercise 1");
        var exercise2 = TestDataBuilder.CreateExercise(id: 2, name: "Exercise 2");
        var template = TestDataBuilder.CreatePlanTemplate(trainerProfileId: trainer.Id);
        var item1 = TestDataBuilder.CreatePlanTemplateItem(id: 1, planTemplateId: template.Id, exerciseId: exercise1.Id);
        var item2 = TestDataBuilder.CreatePlanTemplateItem(id: 2, planTemplateId: template.Id, exerciseId: exercise2.Id);

        Context.TrainerProfiles.Add(trainer);
        Context.Exercises.AddRange(exercise1, exercise2);
        Context.PlanTemplates.Add(template);
        Context.PlanTemplateItems.AddRange(item1, item2);
        await SaveChangesAsync();

        // Act
        var loaded = await Context.PlanTemplates
            .Include(pt => pt.PlanTemplateItems)
            .ThenInclude(pti => pti.Exercise)
            .FirstOrDefaultAsync(pt => pt.Id == template.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.PlanTemplateItems.Should().HaveCount(2);
        loaded.PlanTemplateItems.Should().OnlyContain(pti => pti.Exercise != null);
    }

    [Fact]
    public async Task MagicLink_ShouldExpireCorrectly()
    {
        // Arrange
        var expiredLink = TestDataBuilder.CreateMagicLink(id: 1, email: "expired@test.com");
        expiredLink.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        var validLink = TestDataBuilder.CreateMagicLink(id: 2, email: "valid@test.com");
        validLink.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10);

        Context.MagicLinks.AddRange(expiredLink, validLink);
        await SaveChangesAsync();

        // Act
        var now = DateTimeOffset.UtcNow;
        var activeLinks = await Context.MagicLinks
            .Where(ml => ml.ExpiresAt > now && !ml.IsUsed)
            .ToListAsync();

        // Assert
        activeLinks.Should().HaveCount(1);
        activeLinks[0].Email.Should().Be("valid@test.com");
    }

    [Fact]
    public async Task AdherenceWeek_ShouldQueryByYearAndWeek()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var week1 = TestDataBuilder.CreateAdherenceWeek(id: 1, clientProfileId: client.Id);
        week1.Year = 2024;
        week1.WeekNumber = 10;

        var week2 = TestDataBuilder.CreateAdherenceWeek(id: 2, clientProfileId: client.Id);
        week2.Year = 2024;
        week2.WeekNumber = 11;

        Context.ClientProfiles.Add(client);
        Context.AdherenceWeeks.AddRange(week1, week2);
        await SaveChangesAsync();

        // Act
        var result = await Context.AdherenceWeeks
            .Where(aw => aw.Year == 2024 && aw.WeekNumber == 11)
            .FirstOrDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(week2.Id);
    }
}
