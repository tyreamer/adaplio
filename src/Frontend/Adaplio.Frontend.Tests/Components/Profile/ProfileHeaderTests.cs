using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Adaplio.Frontend.Components.Profile;
using Adaplio.Frontend.Services;
using MudBlazor.Services;

namespace Adaplio.Frontend.Tests.Components.Profile;

public class ProfileHeaderTests : TestContext
{
    private Mock<AuthStateService> _mockAuthStateService;

    public ProfileHeaderTests()
    {
        _mockAuthStateService = new Mock<AuthStateService>();

        // Register required services
        Services.AddMudServices();
        Services.AddSingleton(_mockAuthStateService.Object);
    }

    [Fact]
    public void ProfileHeader_WhenClientUser_ShouldDisplayClientRole()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(false);

        // Act
        var component = RenderComponent<ProfileHeader>();

        // Assert
        var roleChip = component.Find(".role-chip");
        roleChip.TextContent.Should().Contain("Client");
    }

    [Fact]
    public void ProfileHeader_WhenTrainerUser_ShouldDisplayTrainerRole()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(false);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(true);

        // Act
        var component = RenderComponent<ProfileHeader>();

        // Assert
        var roleChip = component.Find(".role-chip");
        roleChip.TextContent.Should().Contain("Trainer");
    }

    [Fact]
    public void ProfileHeader_ShouldDisplayProfileName()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(false);

        // Act
        var component = RenderComponent<ProfileHeader>();

        // Assert
        var profileName = component.Find(".profile-name");
        profileName.Should().NotBeNull();
        profileName.TextContent.Should().NotBeEmpty();
    }

    [Fact]
    public void ProfileHeader_ShouldDisplayAvatarPlaceholder()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(false);

        // Act
        var component = RenderComponent<ProfileHeader>();

        // Assert
        var avatar = component.Find(".profile-avatar-placeholder");
        avatar.Should().NotBeNull();
    }

    [Fact]
    public void ProfileHeader_ShouldDisplayUploadButton()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(false);

        // Act
        var component = RenderComponent<ProfileHeader>();

        // Assert
        var uploadButton = component.Find(".avatar-upload-btn");
        uploadButton.Should().NotBeNull();
        uploadButton.GetAttribute("aria-label").Should().Contain("Change avatar");
    }

    [Fact]
    public void ProfileHeader_WhenClient_ShouldShowTimezoneInfo()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(false);

        // Act
        var component = RenderComponent<ProfileHeader>();

        // Assert
        var profileInfo = component.Find(".profile-info");
        profileInfo.Should().NotBeNull();
        // Should contain timezone information for clients
    }

    [Fact]
    public void ProfileHeader_WhenTrainer_ShouldShowInviteClientButton()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(false);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(true);

        // Act
        var component = RenderComponent<ProfileHeader>();

        // Assert
        var actions = component.Find(".profile-actions");
        actions.Should().NotBeNull();

        var inviteButton = component.FindAll("a[href='/invite-client']");
        inviteButton.Should().HaveCount(1);
    }

    [Fact]
    public void ProfileHeader_WhenClient_ShouldShowAddTrainerButton()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(false);

        // Act
        var component = RenderComponent<ProfileHeader>();

        // Assert
        var actions = component.Find(".profile-actions");
        actions.Should().NotBeNull();

        var addTrainerButton = component.FindAll("a[href='/add-trainer']");
        addTrainerButton.Should().HaveCount(1);
    }

    [Fact]
    public void ProfileHeader_ShouldDisplayProfileCompletion()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(false);

        // Act
        var component = RenderComponent<ProfileHeader>();

        // Assert
        var completionBar = component.Find(".completion-bar");
        completionBar.Should().NotBeNull();

        var completionText = component.Find(".profile-completion");
        completionText.TextContent.Should().Contain("% complete");
    }

    [Fact]
    public void ProfileHeader_ShouldBeResponsive()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsClient).Returns(true);
        _mockAuthStateService.Setup(x => x.IsTrainer).Returns(false);

        // Act
        var component = RenderComponent<ProfileHeader>();

        // Assert
        var headerContent = component.Find(".profile-header-content");
        headerContent.Should().NotBeNull();

        // Check that responsive styles are applied
        var styleTag = component.Find("style");
        styleTag.Should().NotBeNull();
        styleTag.TextContent.Should().Contain("@media (max-width: 768px)");
        styleTag.TextContent.Should().Contain("@media (max-width: 480px)");
    }
}