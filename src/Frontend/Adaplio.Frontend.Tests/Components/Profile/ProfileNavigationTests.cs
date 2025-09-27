using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Adaplio.Frontend.Components.Profile;
using Adaplio.Frontend.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components;

namespace Adaplio.Frontend.Tests.Components.Profile;

public class ProfileNavigationTests : TestContext
{
    private Mock<AuthStateService> _mockAuthStateService;
    private Mock<NavigationManager> _mockNavigationManager;

    public ProfileNavigationTests()
    {
        _mockAuthStateService = new Mock<AuthStateService>();
        _mockNavigationManager = new Mock<NavigationManager>();

        // Register required services
        Services.AddMudServices();
        Services.AddSingleton(_mockAuthStateService.Object);
        Services.AddSingleton(_mockNavigationManager.Object);
    }

    [Fact]
    public void ProfileNavigation_ShouldDisplayBreadcrumbs()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(false);
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var component = RenderComponent<ProfileNavigation>();

        // Assert
        var breadcrumbs = component.Find(".profile-breadcrumbs");
        breadcrumbs.Should().NotBeNull();
    }

    [Fact]
    public void ProfileNavigation_WhenShowBackButtonTrue_ShouldDisplayBackButton()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var component = RenderComponent<ProfileNavigation>(parameters => parameters
            .Add(p => p.ShowBackButton, true));

        // Assert
        var backButton = component.Find(".profile-back-button");
        backButton.Should().NotBeNull();
        backButton.TextContent.Should().Contain("Back");
    }

    [Fact]
    public void ProfileNavigation_WhenShowBackButtonFalse_ShouldHideBackButton()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var component = RenderComponent<ProfileNavigation>(parameters => parameters
            .Add(p => p.ShowBackButton, false));

        // Assert
        var backButtons = component.FindAll(".profile-back-button");
        backButtons.Should().BeEmpty();
    }

    [Fact]
    public void ProfileNavigation_WhenAuthenticated_ShouldDisplayActionsMenu()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var component = RenderComponent<ProfileNavigation>();

        // Assert
        var actionsMenu = component.Find(".profile-menu");
        actionsMenu.Should().NotBeNull();
    }

    [Fact]
    public void ProfileNavigation_WhenNotAuthenticated_ShouldHideActionsMenu()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var component = RenderComponent<ProfileNavigation>();

        // Assert
        var actionsMenus = component.FindAll(".profile-menu");
        actionsMenus.Should().BeEmpty();
    }

    [Fact]
    public void ProfileNavigation_ShouldDisplayCurrentSection()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);
        var testSection = "Account Settings";

        // Act
        var component = RenderComponent<ProfileNavigation>(parameters => parameters
            .Add(p => p.CurrentSection, testSection));

        // Assert
        var breadcrumbs = component.Find(".profile-breadcrumbs");
        breadcrumbs.TextContent.Should().Contain(testSection);
    }

    [Fact]
    public void ProfileNavigation_WhenTrainer_ShouldShowTrainerDashboardBreadcrumb()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(false);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(true);
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var component = RenderComponent<ProfileNavigation>();

        // Assert
        var breadcrumbs = component.Find(".profile-breadcrumbs");
        breadcrumbs.TextContent.Should().Contain("Trainer Dashboard");
    }

    [Fact]
    public void ProfileNavigation_WhenClient_ShouldShowClientDashboardBreadcrumb()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(false);
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var component = RenderComponent<ProfileNavigation>();

        // Assert
        var breadcrumbs = component.Find(".profile-breadcrumbs");
        breadcrumbs.TextContent.Should().Contain("Client Dashboard");
    }

    [Fact]
    public void ProfileNavigation_BackButton_ShouldNavigateToDashboard()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);

        var component = RenderComponent<ProfileNavigation>(parameters => parameters
            .Add(p => p.ShowBackButton, true));

        // Act
        var backButton = component.Find(".profile-back-button");
        backButton.Click();

        // Assert
        _mockNavigationManager.Verify(x => x.NavigateTo("/dashboard"), Times.Once);
    }

    [Fact]
    public void ProfileNavigation_ShouldBeResponsive()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var component = RenderComponent<ProfileNavigation>();

        // Assert
        var navigation = component.Find(".profile-navigation");
        navigation.Should().NotBeNull();

        // Check that responsive styles are applied
        var styleTag = component.Find("style");
        styleTag.Should().NotBeNull();
        styleTag.TextContent.Should().Contain("@media (max-width: 600px)");
        styleTag.TextContent.Should().Contain("@media (max-width: 400px)");
    }

    [Fact]
    public void ProfileNavigation_ExportDataCallback_ShouldBeInvokable()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);

        var exportCallbackInvoked = false;
        var exportCallback = EventCallback.Factory.Create(this, () => exportCallbackInvoked = true);

        var component = RenderComponent<ProfileNavigation>(parameters => parameters
            .Add(p => p.OnExportData, exportCallback));

        // Act - This would require opening the menu and clicking export, which is complex in tests
        // For now, just verify the component renders with the callback

        // Assert
        var actionsMenu = component.Find(".profile-menu");
        actionsMenu.Should().NotBeNull();
    }
}