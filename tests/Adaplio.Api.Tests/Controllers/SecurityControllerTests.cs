using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using System.Text.Json;
using System.Net.Http.Headers;
using Adaplio.Api.Services;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Adaplio.Api.Controllers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Adaplio.Api.Tests.Controllers;

public class SecurityControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<ISecurityMonitoringService> _mockSecurityService;

    public SecurityControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _mockSecurityService = new Mock<ISecurityMonitoringService>();
    }

    [Fact]
    public async Task GetSecurityMetrics_ReturnsMetrics_WhenUserIsAuthorized()
    {
        // Arrange
        var mockMetrics = new SecurityMetrics
        {
            Period = TimeSpan.FromHours(24),
            TotalEvents = 100,
            FailedAuthAttempts = 10,
            RateLimitViolations = 5,
            SuspiciousActivities = 2,
            UniqueIpAddresses = 15,
            UniqueUsers = 8
        };

        _mockSecurityService.Setup(x => x.GetSecurityMetricsAsync(It.IsAny<TimeSpan>()))
            .ReturnsAsync(mockMetrics);

        var controller = new SecurityController(_mockSecurityService.Object, Mock.Of<ILogger<SecurityController>>());

        // Set up authorized user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "admin"),
            new Claim("sub", "test-user-id")
        }, "test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await controller.GetSecurityMetrics(24);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();

        _mockSecurityService.Verify(x => x.GetSecurityMetricsAsync(TimeSpan.FromHours(24)), Times.Once);
    }

    [Fact]
    public async Task GetActiveAlerts_ReturnsAlerts_WhenUserIsAuthorized()
    {
        // Arrange
        var mockAlerts = new List<SecurityAlert>
        {
            new SecurityAlert
            {
                Id = Guid.NewGuid(),
                AlertType = "brute_force_attack",
                Severity = SecuritySeverity.High,
                Message = "Multiple failed login attempts detected",
                Timestamp = DateTimeOffset.UtcNow
            },
            new SecurityAlert
            {
                Id = Guid.NewGuid(),
                AlertType = "rapid_requests",
                Severity = SecuritySeverity.Medium,
                Message = "Rapid API requests detected",
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        _mockSecurityService.Setup(x => x.GetActiveAlertsAsync())
            .ReturnsAsync(mockAlerts);

        var controller = new SecurityController(_mockSecurityService.Object, Mock.Of<ILogger<SecurityController>>());

        // Set up authorized user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "trainer"),
            new Claim("sub", "test-user-id")
        }, "test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await controller.GetActiveAlerts();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();

        _mockSecurityService.Verify(x => x.GetActiveAlertsAsync(), Times.Once);
    }

    [Fact]
    public async Task RunThreatDetection_CompletesSuccessfully_WhenUserIsAdmin()
    {
        // Arrange
        _mockSecurityService.Setup(x => x.CheckForThreatsAsync())
            .Returns(Task.CompletedTask);

        var controller = new SecurityController(_mockSecurityService.Object, Mock.Of<ILogger<SecurityController>>());

        // Set up admin user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "admin"),
            new Claim("sub", "admin-user-id")
        }, "test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await controller.RunThreatDetection();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockSecurityService.Verify(x => x.CheckForThreatsAsync(), Times.Once);
    }

    [Fact]
    public async Task LogSecurityEvent_LogsEvent_WhenUserIsAuthorized()
    {
        // Arrange
        var request = new LogSecurityEventRequest
        {
            EventType = "test_event",
            AdditionalData = new { test = "data" }
        };

        _mockSecurityService.Setup(x => x.LogSecurityEventAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        var controller = new SecurityController(_mockSecurityService.Object, Mock.Of<ILogger<SecurityController>>());

        // Set up authorized user context with IP address
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "trainer"),
            new Claim("sub", "trainer-user-id")
        }, "test"));

        var httpContext = new DefaultHttpContext
        {
            User = user,
            Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1") }
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = await controller.LogSecurityEvent(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockSecurityService.Verify(x => x.LogSecurityEventAsync(
            "test_event",
            "trainer-user-id",
            "192.168.1.1",
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetSecurityMetrics_HandlesException_ReturnsInternalServerError()
    {
        // Arrange
        _mockSecurityService.Setup(x => x.GetSecurityMetricsAsync(It.IsAny<TimeSpan>()))
            .ThrowsAsync(new Exception("Database error"));

        var mockLogger = new Mock<ILogger<SecurityController>>();
        var controller = new SecurityController(_mockSecurityService.Object, mockLogger.Object);

        // Set up authorized user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "admin"),
            new Claim("sub", "test-user-id")
        }, "test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Act
        var result = await controller.GetSecurityMetrics(24);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }
}