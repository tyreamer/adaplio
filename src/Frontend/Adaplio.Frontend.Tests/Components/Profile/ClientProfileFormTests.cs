using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Adaplio.Frontend.Components.Profile;
using Adaplio.Frontend.Services;
using MudBlazor.Services;
using MudBlazor;

namespace Adaplio.Frontend.Tests.Components.Profile;

public class ClientProfileFormTests : TestContext
{
    private Mock<ISnackbar> _mockSnackbar;
    private Mock<ProfileService> _mockProfileService;

    public ClientProfileFormTests()
    {
        _mockSnackbar = new Mock<ISnackbar>();
        _mockProfileService = new Mock<ProfileService>();

        // Register required services
        Services.AddMudServices();
        Services.AddSingleton(_mockSnackbar.Object);
        Services.AddSingleton(_mockProfileService.Object);
    }

    [Fact]
    public void ClientProfileForm_ShouldRenderFormHeader()
    {
        // Act
        var component = RenderComponent<ClientProfileForm>();

        // Assert
        var formTitle = component.Find(".form-title");
        formTitle.Should().NotBeNull();
        formTitle.TextContent.Should().Contain("Personal Information");
    }

    [Fact]
    public void ClientProfileForm_ShouldRenderDisplayNameField()
    {
        // Act
        var component = RenderComponent<ClientProfileForm>();

        // Assert
        var displayNameField = component.FindAll("input").FirstOrDefault(input =>
            input.GetAttribute("placeholder")?.Contains("Display Name") == true ||
            input.GetParent()?.TextContent.Contains("Display Name") == true);

        displayNameField.Should().NotBeNull();
    }

    [Fact]
    public void ClientProfileForm_ShouldRenderTimezoneSelect()
    {
        // Act
        var component = RenderComponent<ClientProfileForm>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("Timezone");
    }

    [Fact]
    public void ClientProfileForm_ShouldHaveBasicInformationSection()
    {
        // Act
        var component = RenderComponent<ClientProfileForm>();

        // Assert
        var sectionTitle = component.FindAll(".section-title")
            .FirstOrDefault(el => el.TextContent.Contains("Basic Information"));
        sectionTitle.Should().NotBeNull();
    }

    [Fact]
    public void ClientProfileForm_ShouldBeContainedInMudForm()
    {
        // Act
        var component = RenderComponent<ClientProfileForm>();

        // Assert
        var mudFormElements = component.FindAll("form");
        mudFormElements.Should().NotBeEmpty();
    }

    [Fact]
    public void ClientProfileForm_ShouldRenderWithMudPaperSections()
    {
        // Act
        var component = RenderComponent<ClientProfileForm>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("pa-6"); // MudPaper class
        componentMarkup.Should().Contain("Variant.Outlined");
    }

    [Fact]
    public void ClientProfileForm_ShouldHaveFormValidation()
    {
        // Act
        var component = RenderComponent<ClientProfileForm>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("Required");
        componentMarkup.Should().Contain("Validation");
    }

    [Fact]
    public void ClientProfileForm_ShouldHaveResponsiveGrid()
    {
        // Act
        var component = RenderComponent<ClientProfileForm>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("xs=\"12\"");
        componentMarkup.Should().Contain("md=\"6\"");
    }

    [Fact]
    public void ClientProfileForm_ShouldHaveHelperTexts()
    {
        // Act
        var component = RenderComponent<ClientProfileForm>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("HelperText");
        componentMarkup.Should().Contain("How others will see your name");
        componentMarkup.Should().Contain("Used for scheduling and reminders");
    }

    [Fact]
    public void ClientProfileForm_ShouldLimitDisplayNameLength()
    {
        // Act
        var component = RenderComponent<ClientProfileForm>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("MaxLength=\"60\"");
    }
}