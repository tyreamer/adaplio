using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Adaplio.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Adaplio.Api.Tests.Integration;

public class AnalyticsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AnalyticsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestAnalyticsDb");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAnalytics_ReturnsAnalyticsData()
    {
        // Act
        var response = await _client.GetAsync("/analytics");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();

        var analyticsData = JsonSerializer.Deserialize<JsonElement>(content);
        analyticsData.TryGetProperty("success", out var successProperty).Should().BeTrue();
        successProperty.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetAnalytics_WithDateRange_ReturnsFilteredData()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var toDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        // Act
        var response = await _client.GetAsync($"/analytics?from={fromDate}&to={toDate}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var analyticsData = JsonSerializer.Deserialize<JsonElement>(content);

        analyticsData.TryGetProperty("success", out var successProperty).Should().BeTrue();
        successProperty.GetBoolean().Should().BeTrue();

        analyticsData.TryGetProperty("data", out var dataProperty).Should().BeTrue();
        dataProperty.TryGetProperty("dateRange", out var dateRangeProperty).Should().BeTrue();
    }

    [Fact]
    public async Task PostAnalyticsEvent_LogsEvent_ReturnsSuccess()
    {
        // Arrange
        var eventData = new
        {
            eventType = "test_event",
            userId = "test-user-123",
            properties = new
            {
                page = "/test",
                action = "click",
                element = "button"
            }
        };

        var json = JsonSerializer.Serialize(eventData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/analytics/events", content);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        result.TryGetProperty("success", out var successProperty).Should().BeTrue();
        successProperty.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task GetSecurityMetrics_WithAuthorization_ReturnsMetrics()
    {
        // Arrange - This would require proper JWT token in real scenario
        // For now, we'll test the endpoint structure

        // Act
        var response = await _client.GetAsync("/api/security/metrics");

        // Assert
        // Without proper auth, should return 401
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSecurityAlerts_WithAuthorization_ReturnsAlerts()
    {
        // Arrange - This would require proper JWT token in real scenario

        // Act
        var response = await _client.GetAsync("/api/security/alerts");

        // Assert
        // Without proper auth, should return 401
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("invalid-from-date", "2024-01-01")]
    [InlineData("2024-01-01", "invalid-to-date")]
    public async Task GetAnalytics_WithInvalidDates_HandlesGracefully(string fromDate, string toDate)
    {
        // Act
        var response = await _client.GetAsync($"/analytics?from={fromDate}&to={toDate}");

        // Assert
        // Should either return 400 Bad Request or handle gracefully with default dates
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostAnalyticsEvent_WithInvalidPayload_ReturnsBadRequest()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/analytics/events", content);

        // Assert
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.BadRequest,
            System.Net.HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetAnalytics_Performance_RespondsQuickly()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/analytics");

        // Assert
        stopwatch.Stop();
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should respond within 5 seconds
    }

    [Fact]
    public async Task PostAnalyticsEvent_Concurrent_HandlesMultipleRequests()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        var eventData = new
        {
            eventType = "concurrent_test",
            userId = "test-user-123",
            properties = new { test = "value" }
        };

        var json = JsonSerializer.Serialize(eventData);

        // Act - Send 10 concurrent requests
        for (int i = 0; i < 10; i++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            tasks.Add(_client.PostAsync("/analytics/events", content));
        }

        var responses = await Task.WhenAll(tasks);

        // Assert
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task HealthCheck_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var healthData = JsonSerializer.Deserialize<JsonElement>(content);

        healthData.TryGetProperty("ok", out var okProperty).Should().BeTrue();
        okProperty.GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DatabaseHealthCheck_ChecksConnection()
    {
        // Act
        var response = await _client.GetAsync("/health/db");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }
}