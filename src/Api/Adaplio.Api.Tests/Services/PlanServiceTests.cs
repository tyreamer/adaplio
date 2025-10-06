using Adaplio.Api.Domain;
using Adaplio.Api.Plans;
using Adaplio.Api.Services;
using Adaplio.Api.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Adaplio.Api.Tests.Services;

public class PlanServiceTests : DatabaseTestBase
{
    private readonly PlanService _planService;

    public PlanServiceTests()
    {
        _planService = new PlanService(Context);
    }

    #region Template Tests

    [Fact]
    public async Task GetTrainerTemplatesAsync_ShouldReturnTemplates_WhenExist()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile(id: 1, userId: 200);
        var exercise1 = TestDataBuilder.CreateExercise(id: 1, name: "Squat");
        var exercise2 = TestDataBuilder.CreateExercise(id: 2, name: "Push-up");
        var template = TestDataBuilder.CreatePlanTemplate(id: 1, trainerProfileId: trainer.Id, name: "Strength Plan");
        var item1 = TestDataBuilder.CreatePlanTemplateItem(id: 1, planTemplateId: template.Id, exerciseId: exercise1.Id);
        var item2 = TestDataBuilder.CreatePlanTemplateItem(id: 2, planTemplateId: template.Id, exerciseId: exercise2.Id);

        Context.TrainerProfiles.Add(trainer);
        Context.Exercises.AddRange(exercise1, exercise2);
        Context.PlanTemplates.Add(template);
        Context.PlanTemplateItems.AddRange(item1, item2);
        await SaveChangesAsync();

        // Act
        var result = await _planService.GetTrainerTemplatesAsync(trainer.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Strength Plan");
        result[0].Items.Should().HaveCount(2);
        result[0].Items.Should().Contain(i => i.ExerciseName == "Squat");
        result[0].Items.Should().Contain(i => i.ExerciseName == "Push-up");
    }

    [Fact]
    public async Task GetTrainerTemplatesAsync_ShouldNotReturnDeletedTemplates()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile();
        var template1 = TestDataBuilder.CreatePlanTemplate(id: 1, trainerProfileId: trainer.Id, name: "Active Template");
        var template2 = TestDataBuilder.CreatePlanTemplate(id: 2, trainerProfileId: trainer.Id, name: "Deleted Template");
        template2.IsDeleted = true;

        Context.TrainerProfiles.Add(trainer);
        Context.PlanTemplates.AddRange(template1, template2);
        await SaveChangesAsync();

        // Act
        var result = await _planService.GetTrainerTemplatesAsync(trainer.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Active Template");
    }

    [Fact]
    public async Task CreateTemplateAsync_ShouldCreateTemplate_WithExercises()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile();
        var exercise1 = TestDataBuilder.CreateExercise(id: 1, name: "Squat");
        var exercise2 = TestDataBuilder.CreateExercise(id: 2, name: "Push-up");

        Context.TrainerProfiles.Add(trainer);
        Context.Exercises.AddRange(exercise1, exercise2);
        await SaveChangesAsync();

        var request = new CreateTemplateRequest(
            Name: "New Template",
            Description: "Test description",
            Category: "Strength",
            DurationWeeks: 8,
            IsPublic: false,
            Items: new[]
            {
                new CreateTemplateItemRequest(
                    ExerciseId: exercise1.Id,
                    TargetSets: 3,
                    TargetReps: 10,
                    HoldSeconds: null,
                    FrequencyPerWeek: 3,
                    Days: new[] { "Monday", "Wednesday", "Friday" },
                    Notes: "Progressive overload"
                ),
                new CreateTemplateItemRequest(
                    ExerciseId: exercise2.Id,
                    TargetSets: 3,
                    TargetReps: 15,
                    HoldSeconds: null,
                    FrequencyPerWeek: 2,
                    Days: new[] { "Tuesday", "Thursday" },
                    Notes: null
                )
            }
        );

        // Act
        var result = await _planService.CreateTemplateAsync(trainer.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Template");
        result.Description.Should().Be("Test description");
        result.Items.Should().HaveCount(2);

        var savedTemplate = await Context.PlanTemplates
            .Include(pt => pt.PlanTemplateItems)
            .FirstOrDefaultAsync(pt => pt.Id == result.Id);

        savedTemplate.Should().NotBeNull();
        savedTemplate!.PlanTemplateItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateTemplateAsync_ShouldUpdateTemplate()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile();
        var exercise = TestDataBuilder.CreateExercise();
        var template = TestDataBuilder.CreatePlanTemplate(trainerProfileId: trainer.Id, name: "Old Name");

        Context.TrainerProfiles.Add(trainer);
        Context.Exercises.Add(exercise);
        Context.PlanTemplates.Add(template);
        await SaveChangesAsync();

        var request = new UpdateTemplateRequest(
            Name: "Updated Name",
            Description: "Updated description",
            Category: "Cardio",
            DurationWeeks: 12,
            IsPublic: true,
            Items: new[]
            {
                new CreateTemplateItemRequest(
                    ExerciseId: exercise.Id,
                    TargetSets: 4,
                    TargetReps: 12,
                    HoldSeconds: null,
                    FrequencyPerWeek: 4,
                    Days: new[] { "Monday", "Tuesday", "Thursday", "Friday" },
                    Notes: "Increase intensity"
                )
            }
        );

        // Act
        var result = await _planService.UpdateTemplateAsync(trainer.Id, template.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated description");
        result.Category.Should().Be("Cardio");
        result.DurationWeeks.Should().Be(12);
    }

    [Fact]
    public async Task DeleteTemplateAsync_ShouldSoftDeleteTemplate()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile();
        var template = TestDataBuilder.CreatePlanTemplate(trainerProfileId: trainer.Id);

        Context.TrainerProfiles.Add(trainer);
        Context.PlanTemplates.Add(template);
        await SaveChangesAsync();

        // Act
        var result = await _planService.DeleteTemplateAsync(trainer.Id, template.Id);

        // Assert
        result.Should().BeTrue();

        var deletedTemplate = await Context.PlanTemplates.FindAsync(template.Id);
        deletedTemplate.Should().NotBeNull();
        deletedTemplate!.IsDeleted.Should().BeTrue();
    }

    #endregion

    #region Proposal Tests

    [Fact]
    public async Task CreateProposalAsync_ShouldCreateProposal_FromTemplate()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile(id: 1, userId: 200);
        var client = TestDataBuilder.CreateClientProfile(id: 1, userId: 100);
        var exercise = TestDataBuilder.CreateExercise();
        var template = TestDataBuilder.CreatePlanTemplate(trainerProfileId: trainer.Id);
        var item = TestDataBuilder.CreatePlanTemplateItem(planTemplateId: template.Id, exerciseId: exercise.Id);

        Context.TrainerProfiles.Add(trainer);
        Context.ClientProfiles.Add(client);
        Context.Exercises.Add(exercise);
        Context.PlanTemplates.Add(template);
        Context.PlanTemplateItems.Add(item);
        await SaveChangesAsync();

        var request = new CreateProposalRequest(
            ClientProfileId: client.Id,
            PlanTemplateId: template.Id,
            CustomPlanJson: null,
            StartsOn: DateOnly.FromDateTime(DateTime.Today.AddDays(7))
        );

        // Act
        var result = await _planService.CreateProposalAsync(trainer.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.TrainerProfileId.Should().Be(trainer.Id);
        result.ClientProfileId.Should().Be(client.Id);
        result.Status.Should().Be("pending");
        result.StartsOn.Should().Be(DateOnly.FromDateTime(DateTime.Today.AddDays(7)));
    }

    [Fact]
    public async Task GetClientProposalsAsync_ShouldReturnPendingProposals()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile();
        var client = TestDataBuilder.CreateClientProfile();
        var proposal1 = TestDataBuilder.CreatePlanProposal(id: 1, trainerProfileId: trainer.Id, clientProfileId: client.Id, status: "pending");
        var proposal2 = TestDataBuilder.CreatePlanProposal(id: 2, trainerProfileId: trainer.Id, clientProfileId: client.Id, status: "accepted");

        Context.TrainerProfiles.Add(trainer);
        Context.ClientProfiles.Add(client);
        Context.PlanProposals.AddRange(proposal1, proposal2);
        await SaveChangesAsync();

        // Act
        var result = await _planService.GetClientProposalsAsync(client.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Status == "pending");
        result.Should().Contain(p => p.Status == "accepted");
    }

    [Fact]
    public async Task AcceptProposalAsync_ShouldCreatePlanInstance()
    {
        // Arrange
        var trainer = TestDataBuilder.CreateTrainerProfile();
        var client = TestDataBuilder.CreateClientProfile();
        var exercise = TestDataBuilder.CreateExercise();
        var template = TestDataBuilder.CreatePlanTemplate(trainerProfileId: trainer.Id);
        var item = TestDataBuilder.CreatePlanTemplateItem(planTemplateId: template.Id, exerciseId: exercise.Id);
        var proposal = TestDataBuilder.CreatePlanProposal(
            trainerProfileId: trainer.Id,
            clientProfileId: client.Id,
            status: "pending");
        proposal.PlanTemplateId = template.Id;

        Context.TrainerProfiles.Add(trainer);
        Context.ClientProfiles.Add(client);
        Context.Exercises.Add(exercise);
        Context.PlanTemplates.Add(template);
        Context.PlanTemplateItems.Add(item);
        Context.PlanProposals.Add(proposal);
        await SaveChangesAsync();

        var request = new AcceptProposalRequest(
            ItemSelections: new Dictionary<int, bool> { { item.Id, true } }
        );

        // Act
        var result = await _planService.AcceptProposalAsync(client.Id, proposal.Id, request);

        // Assert
        result.Should().NotBeNull();
        result.PlanInstanceId.Should().BeGreaterThan(0);
        result.AcceptedItems.Should().HaveCount(1);

        var planInstance = await Context.PlanInstances
            .Include(pi => pi.ExerciseInstances)
            .FirstOrDefaultAsync(pi => pi.Id == result.PlanInstanceId);

        planInstance.Should().NotBeNull();
        planInstance!.ClientProfileId.Should().Be(client.Id);
        planInstance.ExerciseInstances.Should().HaveCount(1);

        var updatedProposal = await Context.PlanProposals.FindAsync(proposal.Id);
        updatedProposal!.Status.Should().Be("accepted");
    }

    #endregion

    #region Client Board Tests

    [Fact]
    public async Task GetClientBoardAsync_ShouldReturnActivePlans_ForWeek()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var exercise1 = TestDataBuilder.CreateExercise(id: 1, name: "Squat");
        var exercise2 = TestDataBuilder.CreateExercise(id: 2, name: "Push-up");
        var plan = TestDataBuilder.CreatePlanInstance(clientProfileId: client.Id, name: "Active Plan");
        var instance1 = TestDataBuilder.CreateExerciseInstance(id: 1, planInstanceId: plan.Id, exerciseId: exercise1.Id, clientProfileId: client.Id);
        var instance2 = TestDataBuilder.CreateExerciseInstance(id: 2, planInstanceId: plan.Id, exerciseId: exercise2.Id, clientProfileId: client.Id);

        instance1.FrequencyPerWeek = 3;
        instance1.DaysJson = "[\"Monday\",\"Wednesday\",\"Friday\"]";
        instance2.FrequencyPerWeek = 2;
        instance2.DaysJson = "[\"Tuesday\",\"Thursday\"]";

        Context.ClientProfiles.Add(client);
        Context.Exercises.AddRange(exercise1, exercise2);
        Context.PlanInstances.Add(plan);
        Context.ExerciseInstances.AddRange(instance1, instance2);
        await SaveChangesAsync();

        var weekStart = DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1));

        // Act
        var result = await _planService.GetClientBoardAsync(client.Id, weekStart);

        // Assert
        result.Should().NotBeNull();
        result.Plans.Should().HaveCount(1);
        result.Plans[0].Name.Should().Be("Active Plan");
        result.Plans[0].ExerciseInstances.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetClientBoardAsync_ShouldIncludeCompletionStatus()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var exercise = TestDataBuilder.CreateExercise();
        var plan = TestDataBuilder.CreatePlanInstance(clientProfileId: client.Id);
        var instance = TestDataBuilder.CreateExerciseInstance(planInstanceId: plan.Id, exerciseId: exercise.Id, clientProfileId: client.Id);

        var weekStart = DateOnly.FromDateTime(DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1));
        var progressEvent = TestDataBuilder.CreateProgressEvent(
            clientProfileId: client.Id,
            exerciseInstanceId: instance.Id,
            eventType: "set_completed");
        progressEvent.LoggedAt = weekStart.ToDateTime(TimeOnly.MinValue).AddDays(1);

        Context.ClientProfiles.Add(client);
        Context.Exercises.Add(exercise);
        Context.PlanInstances.Add(plan);
        Context.ExerciseInstances.Add(instance);
        Context.ProgressEvents.Add(progressEvent);
        await SaveChangesAsync();

        // Act
        var result = await _planService.GetClientBoardAsync(client.Id, weekStart);

        // Assert
        result.Should().NotBeNull();
        result.Plans[0].ExerciseInstances.Should().HaveCount(1);
    }

    #endregion

    #region Plan Instance Tests

    [Fact]
    public async Task GetClientPlansAsync_ShouldReturnActivePlans()
    {
        // Arrange
        var client = TestDataBuilder.CreateClientProfile();
        var plan1 = TestDataBuilder.CreatePlanInstance(id: 1, clientProfileId: client.Id, name: "Active Plan");
        var plan2 = TestDataBuilder.CreatePlanInstance(id: 2, clientProfileId: client.Id, name: "Completed Plan");
        plan2.Status = "completed";
        plan2.ActualEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-10));

        Context.ClientProfiles.Add(client);
        Context.PlanInstances.AddRange(plan1, plan2);
        await SaveChangesAsync();

        // Act
        var result = await _planService.GetClientPlansAsync(client.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Status == "active");
        result.Should().Contain(p => p.Status == "completed");
    }

    #endregion
}
