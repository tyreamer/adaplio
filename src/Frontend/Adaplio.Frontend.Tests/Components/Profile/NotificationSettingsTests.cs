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

public class NotificationSettingsTests : TestContext
{
    private Mock<ISnackbar> _mockSnackbar;
    private Mock<NotificationService> _mockNotificationService;

    public NotificationSettingsTests()
    {
        _mockSnackbar = new Mock<ISnackbar>();
        _mockNotificationService = new Mock<NotificationService>();

        // Register required services
        Services.AddMudServices();
        Services.AddSingleton(_mockSnackbar.Object);
        Services.AddSingleton(_mockNotificationService.Object);
    }

    [Fact]
    public void NotificationSettings_ShouldRenderHeader()
    {
        // Act
        var component = RenderComponent<NotificationSettings>();

        // Assert
        var formTitle = component.Find(".form-title");
        formTitle.Should().NotBeNull();
        formTitle.TextContent.Should().Contain("Notification Preferences");
    }

    [Fact]
    public void NotificationSettings_ShouldRenderGeneralNotificationsSection()
    {
        // Act
        var component = RenderComponent<NotificationSettings>();

        // Assert
        var sectionTitle = component.FindAll(".section-title")
            .FirstOrDefault(el => el.TextContent.Contains("General Notifications"));
        sectionTitle.Should().NotBeNull();
    }

    [Fact]
    public void NotificationSettings_ShouldHaveReminderTimePicker()
    {
        // Act
        var component = RenderComponent<NotificationSettings>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("Reminder Time");
        componentMarkup.Should().Contain("Daily reminder time");
    }

    [Fact]
    public void NotificationSettings_ShouldHaveNotificationsToggle()
    {
        // Act
        var component = RenderComponent<NotificationSettings>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("Enable Notifications");
        componentMarkup.Should().Contain("MudSwitch");
    }

    [Fact]
    public void NotificationSettings_ShouldHaveNotificationChannelsSection()
    {
        // Act
        var component = RenderComponent<NotificationSettings>();

        // Assert
        var sectionTitle = component.FindAll(".section-title")
            .FirstOrDefault(el => el.TextContent.Contains("Notification Channels"));
        sectionTitle.Should().NotBeNull();
    }

    [Fact]
    public void NotificationSettings_ShouldUseTimePickerWithAmPm()
    {
        // Act
        var component = RenderComponent<NotificationSettings>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("AmPm=\"true\"");
    }

    [Fact]
    public void NotificationSettings_ShouldHaveMudPaperSections()
    {
        // Act
        var component = RenderComponent<NotificationSettings>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("pa-6"); // MudPaper padding class
        componentMarkup.Should().Contain("Variant.Outlined");
    }

    [Fact]
    public void NotificationSettings_ShouldHaveResponsiveGrid()
    {
        // Act
        var component = RenderComponent<NotificationSettings>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("xs=\"12\"");
        componentMarkup.Should().Contain("md=\"6\"");
    }

    [Fact]
    public void NotificationSettings_ShouldHaveDescriptiveHelperText()
    {
        // Act
        var component = RenderComponent<NotificationSettings>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("Manage how and when you receive notifications");
    }

    [Fact]
    public void NotificationSettings_ShouldUseIconsForVisualCues()
    {
        // Act
        var component = RenderComponent<NotificationSettings>();

        // Assert
        var componentMarkup = component.Markup;
        componentMarkup.Should().Contain("Icons.Material.Filled.Notifications");
    }
}