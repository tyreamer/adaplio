using System.ComponentModel.DataAnnotations;

namespace Adaplio.Api.Analytics;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var analyticsGroup = app.MapGroup("/api/analytics").WithTags("Analytics");

        // Track events endpoint (public)
        analyticsGroup.MapPost("/events", TrackEvent)
            .WithName("TrackEvent");
    }

    private static async Task<IResult> TrackEvent(
        AnalyticsEventRequest request,
        ILogger<AnalyticsEventRequest> logger)
    {
        try
        {
            // Log the analytics event
            logger.LogInformation("Analytics Event: {Event} | Method: {Method} | Timestamp: {Timestamp}",
                request.Event, request.Method, request.Timestamp);

            // In a real implementation, you would:
            // 1. Store in analytics database
            // 2. Send to analytics service (Google Analytics, Mixpanel, etc.)
            // 3. Update metrics dashboards

            return Results.Ok(new { message = "Event tracked successfully" });
        }
        catch (Exception ex)
        {
            // Don't let analytics failures break the main application flow
            return Results.Ok(new { message = "Event tracking skipped" });
        }
    }
}

// DTOs
public record AnalyticsEventRequest(
    [Required] string Event,
    string? Method = null,
    DateTimeOffset? Timestamp = null,
    Dictionary<string, object>? Properties = null
);