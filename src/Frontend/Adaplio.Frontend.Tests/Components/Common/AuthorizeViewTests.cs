using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Adaplio.Frontend.Components.Common;
using Adaplio.Frontend.Services;
using MudBlazor.Services;

namespace Adaplio.Frontend.Tests.Components.Common;

public class AuthorizeViewTests : TestContext
{
    private Mock<AuthStateService> _mockAuthStateService;

    public AuthorizeViewTests()
    {
        _mockAuthStateService = new Mock<AuthStateService>();

        // Register required services
        Services.AddMudServices();
        Services.AddSingleton(_mockAuthStateService.Object);
    }

    [Fact]
    public void AuthorizeView_WhenAuthenticated_ShouldRenderAuthorizedContent()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var component = RenderComponent<AuthorizeView>(parameters => parameters
            .AddChildContent<Microsoft.AspNetCore.Components.RenderFragment>("Authorized", builder =>
            {
                builder.AddContent(0, "Authorized Content");
            }));

        // Assert
        component.Markup.Should().Contain("Authorized Content");
    }

    [Fact]
    public void AuthorizeView_WhenNotAuthenticated_ShouldRenderNotAuthorizedContent()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var component = RenderComponent<AuthorizeView>(parameters => parameters
            .AddChildContent<Microsoft.AspNetCore.Components.RenderFragment>("NotAuthorized", builder =>
            {
                builder.AddContent(0, "Not Authorized Content");
            }));

        // Assert
        component.Markup.Should().Contain("Not Authorized Content");
    }

    [Fact]
    public void AuthorizeView_WhenNotAuthenticated_ShouldNotRenderAuthorizedContent()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var component = RenderComponent<AuthorizeView>(parameters => parameters
            .AddChildContent<Microsoft.AspNetCore.Components.RenderFragment>("Authorized", builder =>
            {
                builder.AddContent(0, "Authorized Content");
            })
            .AddChildContent<Microsoft.AspNetCore.Components.RenderFragment>("NotAuthorized", builder =>
            {
                builder.AddContent(0, "Not Authorized Content");
            }));

        // Assert
        component.Markup.Should().NotContain("Authorized Content");
        component.Markup.Should().Contain("Not Authorized Content");
    }

    [Fact]
    public void AuthorizeView_WhenAuthenticated_ShouldNotRenderNotAuthorizedContent()
    {
        // Arrange
        _mockAuthStateService.Setup(x => x.IsAuthenticated).Returns(true);

        // Act
        var component = RenderComponent<AuthorizeView>(parameters => parameters
            .AddChildContent<Microsoft.AspNetCore.Components.RenderFragment>("Authorized", builder =>
            {
                builder.AddContent(0, "Authorized Content");
            })
            .AddChildContent<Microsoft.AspNetCore.Components.RenderFragment>("NotAuthorized", builder =>
            {
                builder.AddContent(0, "Not Authorized Content");
            }));

        // Assert
        component.Markup.Should().Contain("Authorized Content");
        component.Markup.Should().NotContain("Not Authorized Content");
    }
}