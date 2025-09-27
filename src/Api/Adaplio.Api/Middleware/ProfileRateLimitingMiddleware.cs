using System.Collections.Concurrent;
using System.Security.Claims;

namespace Adaplio.Api.Middleware;

public class ProfileRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProfileRateLimitingMiddleware> _logger;

    // In-memory storage for rate limiting (in production, use Redis or similar)
    private static readonly ConcurrentDictionary<string, UserRateLimit> _userLimits = new();

    // Rate limits: 10 profile updates per minute per user
    private const int MaxProfileUpdatesPerMinute = 10;
    private const int WindowSizeMinutes = 1;

    public ProfileRateLimitingMiddleware(RequestDelegate next, ILogger<ProfileRateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply rate limiting to profile update endpoints
        if (!IsProfileUpdateEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var userId = GetUserId(context);
        if (userId == null)
        {
            await _next(context);
            return;
        }

        var userKey = $"profile_updates_{userId}";
        var userLimit = _userLimits.GetOrAdd(userKey, _ => new UserRateLimit());

        if (!userLimit.CanMakeRequest())
        {
            _logger.LogWarning("Rate limit exceeded for user {UserId} on profile updates", userId);

            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.Headers.Add("Retry-After", "60");
            await context.Response.WriteAsync("Rate limit exceeded. Maximum 10 profile updates per minute.");
            return;
        }

        userLimit.RegisterRequest();
        await _next(context);
    }

    private bool IsProfileUpdateEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLower();
        if (pathValue == null) return false;

        return pathValue.Contains("/api/me/profile") &&
               (pathValue.EndsWith("profile") || pathValue.Contains("/scope"));
    }

    private string? GetUserId(HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private class UserRateLimit
    {
        private readonly Queue<DateTime> _requests = new();
        private readonly object _lock = new();

        public bool CanMakeRequest()
        {
            lock (_lock)
            {
                CleanOldRequests();
                return _requests.Count < MaxProfileUpdatesPerMinute;
            }
        }

        public void RegisterRequest()
        {
            lock (_lock)
            {
                CleanOldRequests();
                _requests.Enqueue(DateTime.UtcNow);
            }
        }

        private void CleanOldRequests()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-WindowSizeMinutes);
            while (_requests.Count > 0 && _requests.Peek() < cutoff)
            {
                _requests.Dequeue();
            }
        }
    }
}