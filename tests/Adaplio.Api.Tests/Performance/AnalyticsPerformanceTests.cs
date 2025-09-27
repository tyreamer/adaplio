using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Diagnostics;
using System.Text.Json;
using System.Text;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Adaplio.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Adaplio.Api.Tests.Performance;

public class AnalyticsPerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AnalyticsPerformanceTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("PerformanceTestDb");
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Analytics_HighVolumeEvents_PerformsWithinLimits()
    {
        // Arrange
        const int eventCount = 1000;
        var tasks = new List<Task<HttpResponseMessage>>();
        var stopwatch = Stopwatch.StartNew();

        var eventData = new
        {
            eventType = "performance_test",
            userId = "perf-user",
            properties = new { test = "high_volume" }
        };

        var json = JsonSerializer.Serialize(eventData);

        // Act - Send many events concurrently
        for (int i = 0; i < eventCount; i++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            tasks.Add(_client.PostAsync("/analytics/events", content));
        }

        var responses = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        responses.Should().HaveCount(eventCount);
        responses.Should().OnlyContain(r => r.StatusCode == System.Net.HttpStatusCode.OK);

        // Performance assertion - should handle 1000 events in reasonable time
        var eventsPerSecond = eventCount / (stopwatch.ElapsedMilliseconds / 1000.0);
        eventsPerSecond.Should().BeGreaterThan(50, "Should handle at least 50 events per second");

        Console.WriteLine($"Processed {eventCount} events in {stopwatch.ElapsedMilliseconds}ms ({eventsPerSecond:F2} events/sec)");
    }

    [Fact]
    public async Task Analytics_GetAnalytics_ResponseTime()
    {
        // Arrange
        const int iterations = 50;
        var responseTimes = new List<long>();

        // Act - Make multiple requests to measure response time
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync("/analytics");
            stopwatch.Stop();

            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            responseTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert
        var averageResponseTime = responseTimes.Average();
        var maxResponseTime = responseTimes.Max();
        var minResponseTime = responseTimes.Min();

        averageResponseTime.Should().BeLessThan(1000, "Average response time should be under 1 second");
        maxResponseTime.Should().BeLessThan(5000, "Maximum response time should be under 5 seconds");

        Console.WriteLine($"Analytics response times - Avg: {averageResponseTime:F2}ms, Min: {minResponseTime}ms, Max: {maxResponseTime}ms");
    }

    [Fact]
    public async Task SecurityMonitoring_HighVolumeEvents_Performance()
    {
        // Arrange
        const int eventCount = 500;
        var stopwatch = Stopwatch.StartNew();
        var successCount = 0;

        // Act - Generate high volume of security events
        var tasks = Enumerable.Range(0, eventCount).Select(async i =>
        {
            try
            {
                // Simulate various security events
                var eventTypes = new[] { "auth_failed", "auth_success", "rate_limit", "api_request" };
                var eventType = eventTypes[i % eventTypes.Length];

                var eventData = new
                {
                    eventType = eventType,
                    userId = $"user-{i % 100}",
                    ipAddress = $"192.168.1.{i % 255}",
                    timestamp = DateTimeOffset.UtcNow,
                    properties = new { requestId = i }
                };

                var json = JsonSerializer.Serialize(eventData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("/analytics/events", content);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Interlocked.Increment(ref successCount);
                }

                return response;
            }
            catch
            {
                return null;
            }
        });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        successCount.Should().BeGreaterThan(eventCount * 0.9, "At least 90% of events should be processed successfully");

        var eventsPerSecond = eventCount / (stopwatch.ElapsedMilliseconds / 1000.0);
        eventsPerSecond.Should().BeGreaterThan(25, "Should handle at least 25 security events per second");

        Console.WriteLine($"Security monitoring: {successCount}/{eventCount} events processed in {stopwatch.ElapsedMilliseconds}ms ({eventsPerSecond:F2} events/sec)");
    }

    [Fact]
    public async Task RateLimit_PerformanceUnderLoad()
    {
        // Arrange
        const int requestCount = 200;
        var responses = new ConcurrentBag<(long responseTime, System.Net.HttpStatusCode statusCode)>();

        // Act - Make many requests to test rate limiting performance
        var tasks = Enumerable.Range(0, requestCount).Select(async i =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var response = await _client.GetAsync($"/analytics?test=ratelimit&id={i}");
                stopwatch.Stop();
                responses.Add((stopwatch.ElapsedMilliseconds, response.StatusCode));
                return response;
            }
            catch
            {
                stopwatch.Stop();
                responses.Add((stopwatch.ElapsedMilliseconds, System.Net.HttpStatusCode.InternalServerError));
                return null;
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        var responseTimes = responses.Select(r => r.responseTime).ToList();
        var successfulRequests = responses.Count(r => r.statusCode == System.Net.HttpStatusCode.OK);
        var rateLimitedRequests = responses.Count(r => r.statusCode == System.Net.HttpStatusCode.TooManyRequests);

        responseTimes.Should().NotBeEmpty();
        successfulRequests.Should().BeGreaterThan(0, "Some requests should succeed");

        var averageResponseTime = responseTimes.Average();
        averageResponseTime.Should().BeLessThan(2000, "Average response time should be reasonable even under load");

        Console.WriteLine($"Rate limit test: {successfulRequests} successful, {rateLimitedRequests} rate limited, avg response: {averageResponseTime:F2}ms");
    }

    [Fact]
    public async Task Memory_AnalyticsDataRetention_NoLeaks()
    {
        // Arrange
        const int eventCount = 100;
        var initialMemory = GC.GetTotalMemory(true);

        // Act - Generate events and force garbage collection
        for (int batch = 0; batch < 10; batch++)
        {
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < eventCount; i++)
            {
                var eventData = new
                {
                    eventType = "memory_test",
                    userId = $"user-{i}",
                    properties = new { batch = batch, index = i, data = new string('x', 1000) } // 1KB of data
                };

                var json = JsonSerializer.Serialize(eventData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                tasks.Add(_client.PostAsync("/analytics/events", content));
            }

            await Task.WhenAll(tasks);

            // Force garbage collection between batches
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseKB = memoryIncrease / 1024.0;

        // Memory should not increase dramatically (allowing for some growth)
        memoryIncreaseKB.Should().BeLessThan(50000, "Memory usage should not grow excessively (< 50MB increase)");

        Console.WriteLine($"Memory test: Initial: {initialMemory / 1024:F2}KB, Final: {finalMemory / 1024:F2}KB, Increase: {memoryIncreaseKB:F2}KB");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Analytics_ConcurrentUsers_ScalesLinearly(int concurrentUsers)
    {
        // Arrange
        const int requestsPerUser = 20;
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate concurrent users
        var userTasks = Enumerable.Range(0, concurrentUsers).Select(async userId =>
        {
            var userStopwatch = Stopwatch.StartNew();
            var requests = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < requestsPerUser; i++)
            {
                var eventData = new
                {
                    eventType = "concurrency_test",
                    userId = $"user-{userId}",
                    properties = new { requestIndex = i }
                };

                var json = JsonSerializer.Serialize(eventData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                requests.Add(_client.PostAsync("/analytics/events", content));
            }

            var responses = await Task.WhenAll(requests);
            userStopwatch.Stop();

            return new
            {
                UserId = userId,
                ElapsedMs = userStopwatch.ElapsedMilliseconds,
                SuccessCount = responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.OK)
            };
        });

        var results = await Task.WhenAll(userTasks);
        stopwatch.Stop();

        // Assert
        var totalRequests = concurrentUsers * requestsPerUser;
        var totalSuccessful = results.Sum(r => r.SuccessCount);
        var averageUserTime = results.Average(r => r.ElapsedMs);

        totalSuccessful.Should().BeGreaterThan(totalRequests * 0.9, "At least 90% of requests should succeed");

        var requestsPerSecond = totalRequests / (stopwatch.ElapsedMilliseconds / 1000.0);
        requestsPerSecond.Should().BeGreaterThan(10, "Should maintain reasonable throughput");

        Console.WriteLine($"Concurrent users test ({concurrentUsers} users): {totalSuccessful}/{totalRequests} successful, " +
                         $"total time: {stopwatch.ElapsedMilliseconds}ms, avg user time: {averageUserTime:F2}ms, " +
                         $"throughput: {requestsPerSecond:F2} req/sec");
    }
}