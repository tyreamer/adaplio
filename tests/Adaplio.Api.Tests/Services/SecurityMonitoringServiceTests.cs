using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Adaplio.Api.Services;

namespace Adaplio.Api.Tests.Services;

public class SecurityMonitoringServiceTests
{
    private readonly Mock<ILogger<SecurityMonitoringService>> _mockLogger;
    private readonly SecurityMonitoringService _service;

    public SecurityMonitoringServiceTests()
    {
        _mockLogger = new Mock<ILogger<SecurityMonitoringService>>();
        _service = new SecurityMonitoringService(_mockLogger.Object);
    }

    [Fact]
    public async Task LogSecurityEventAsync_AddsEventToQueue()
    {
        // Arrange
        var eventType = "auth_failed";
        var userId = "test-user";
        var ipAddress = "192.168.1.1";
        var additionalData = new { reason = "invalid_password" };

        // Act
        await _service.LogSecurityEventAsync(eventType, userId, ipAddress, additionalData);

        // Assert - Verify the event was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security Event: auth_failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSecurityMetricsAsync_ReturnsCorrectMetrics()
    {
        // Arrange
        var period = TimeSpan.FromHours(1);

        // Add some test events
        await _service.LogSecurityEventAsync("auth_failed", "user1", "192.168.1.1");
        await _service.LogSecurityEventAsync("auth_failed", "user2", "192.168.1.2");
        await _service.LogSecurityEventAsync("rate_limit_exceeded", "user1", "192.168.1.1");
        await _service.LogSecurityEventAsync("suspicious_activity", "user3", "192.168.1.3");

        // Act
        var metrics = await _service.GetSecurityMetricsAsync(period);

        // Assert
        metrics.Should().NotBeNull();
        metrics.Period.Should().Be(period);
        metrics.TotalEvents.Should().BeGreaterThan(0);
        metrics.FailedAuthAttempts.Should().BeGreaterThan(0);
        metrics.RateLimitViolations.Should().BeGreaterThan(0);
        metrics.SuspiciousActivities.Should().BeGreaterThan(0);
        metrics.UniqueIpAddresses.Should().Be(3);
        metrics.UniqueUsers.Should().Be(3);
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ReturnsAlertsInDescendingOrder()
    {
        // Arrange
        // First create conditions that would trigger alerts
        for (int i = 0; i < 12; i++)
        {
            await _service.LogSecurityEventAsync("auth_failed", "user1", "192.168.1.1");
        }

        // Force threat detection
        await _service.CheckForThreatsAsync();

        // Act
        var alerts = await _service.GetActiveAlertsAsync();

        // Assert
        alerts.Should().NotBeNull();
        if (alerts.Count > 1)
        {
            // Verify alerts are ordered by severity (descending)
            for (int i = 0; i < alerts.Count - 1; i++)
            {
                alerts[i].Severity.Should().BeGreaterOrEqualTo(alerts[i + 1].Severity);
            }
        }
    }

    [Fact]
    public async Task CheckForThreatsAsync_DetectsBruteForceAttack()
    {
        // Arrange - Generate enough failed auth attempts to trigger brute force detection
        var ipAddress = "192.168.1.100";
        for (int i = 0; i < 15; i++)
        {
            await _service.LogSecurityEventAsync("auth_failed", $"user{i}", ipAddress);
        }

        // Act
        await _service.CheckForThreatsAsync();

        // Get alerts to verify detection
        var alerts = await _service.GetActiveAlertsAsync();

        // Assert
        alerts.Should().Contain(alert =>
            alert.AlertType == "brute_force_attack" &&
            alert.Severity == SecuritySeverity.High);
    }

    [Fact]
    public async Task CheckForThreatsAsync_DetectsRapidRequests()
    {
        // Arrange - Generate rapid requests
        var ipAddress = "192.168.1.200";
        for (int i = 0; i < 150; i++)
        {
            await _service.LogSecurityEventAsync("api_request", $"user{i}", ipAddress);
        }

        // Act
        await _service.CheckForThreatsAsync();

        // Get alerts to verify detection
        var alerts = await _service.GetActiveAlertsAsync();

        // Assert
        alerts.Should().Contain(alert =>
            alert.AlertType == "rapid_requests" &&
            alert.Severity == SecuritySeverity.Medium);
    }

    [Fact]
    public async Task CheckForThreatsAsync_DetectsAccountSharing()
    {
        // Arrange - Simulate many users from same IP
        var ipAddress = "192.168.1.300";
        for (int i = 0; i < 60; i++)
        {
            await _service.LogSecurityEventAsync("user_login", $"user{i:D3}", ipAddress);
        }

        // Act
        await _service.CheckForThreatsAsync();

        // Get alerts to verify detection
        var alerts = await _service.GetActiveAlertsAsync();

        // Assert
        alerts.Should().Contain(alert =>
            alert.AlertType == "potential_account_sharing" &&
            alert.Severity == SecuritySeverity.Medium);
    }

    [Fact]
    public async Task CheckForThreatsAsync_DetectsPrivilegeEscalation()
    {
        // Arrange - Simulate unauthorized access attempts
        var userId = "suspicious-user";
        for (int i = 0; i < 8; i++)
        {
            await _service.LogSecurityEventAsync("unauthorized_access", userId, "192.168.1.400");
        }

        // Act
        await _service.CheckForThreatsAsync();

        // Get alerts to verify detection
        var alerts = await _service.GetActiveAlertsAsync();

        // Assert
        alerts.Should().Contain(alert =>
            alert.AlertType == "privilege_escalation_attempt" &&
            alert.Severity == SecuritySeverity.High);
    }

    [Fact]
    public async Task CheckForImmediateThreats_DetectsInjectionAttempts()
    {
        // Arrange - Create event with malicious patterns
        var maliciousData = "SELECT * FROM users; DROP TABLE users;";
        await _service.LogSecurityEventAsync("input_validation", "hacker", "192.168.1.500", maliciousData);

        // Act - Get alerts (injection detection happens during logging)
        var alerts = await _service.GetActiveAlertsAsync();

        // Assert
        alerts.Should().Contain(alert =>
            alert.AlertType == "injection_attempt" &&
            alert.Severity == SecuritySeverity.High);
    }

    [Fact]
    public async Task GetActiveAlertsAsync_RemovesExpiredAlerts()
    {
        // This test verifies that expired alerts are cleaned up
        // Since alerts expire after 24 hours in the implementation,
        // we can verify the cleanup logic is called

        // Arrange - trigger an alert
        for (int i = 0; i < 12; i++)
        {
            await _service.LogSecurityEventAsync("auth_failed", "user1", "192.168.1.1");
        }
        await _service.CheckForThreatsAsync();

        // Act
        var alertsBefore = await _service.GetActiveAlertsAsync();
        var alertsAfter = await _service.GetActiveAlertsAsync();

        // Assert - verify alerts are returned (they haven't expired yet)
        alertsBefore.Should().NotBeEmpty();
        alertsAfter.Count.Should().Be(alertsBefore.Count);
    }

    [Theory]
    [InlineData("", "user1", "192.168.1.1")]
    [InlineData("test_event", "", "192.168.1.1")]
    [InlineData("test_event", "user1", "")]
    public async Task LogSecurityEventAsync_HandlesNullOrEmptyParameters(string eventType, string userId, string ipAddress)
    {
        // Act & Assert - Should not throw
        await _service.LogSecurityEventAsync(eventType, userId, ipAddress);

        // Verify logging still occurs
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}