using System.Collections.Concurrent;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Adaplio.Api.Services;

namespace Adaplio.Api.Middleware;

public class SecurityRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityRateLimitingMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    // In-memory storage for rate limiting (in production, use Redis or similar)
    private static readonly ConcurrentDictionary<string, EndpointRateLimit> _rateLimits = new();
    private static readonly ConcurrentDictionary<string, SuspiciousActivity> _suspiciousActivity = new();

    // Rate limit configurations per endpoint type
    private static readonly Dictionary<string, RateLimitConfig> _rateLimitConfigs = new()
    {
        // Authentication endpoints
        { "auth_login", new RateLimitConfig(5, 15, 30) },        // 5 attempts per 15 min, lockout 30 min
        { "auth_register", new RateLimitConfig(3, 60, 120) },    // 3 attempts per hour, lockout 2 hours
        { "auth_password_reset", new RateLimitConfig(3, 60, 60) }, // 3 attempts per hour

        // API endpoints
        { "api_general", new RateLimitConfig(100, 1, 5) },       // 100 requests per minute
        { "api_upload", new RateLimitConfig(10, 5, 15) },        // 10 uploads per 5 minutes
        { "api_invite", new RateLimitConfig(20, 60, 60) },       // 20 invites per hour
        { "api_profile", new RateLimitConfig(30, 10, 10) },      // 30 profile ops per 10 minutes

        // Global IP-based limits
        { "global_ip", new RateLimitConfig(500, 1, 60) }         // 500 requests per minute per IP
    };

    public SecurityRateLimitingMiddleware(RequestDelegate next, ILogger<SecurityRateLimitingMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = GetEndpointCategory(context.Request.Path, context.Request.Method);
        var userId = GetUserId(context);
        var ipAddress = GetClientIpAddress(context);

        // Check IP-based rate limits first
        if (!await CheckRateLimit(context, "global_ip", ipAddress, endpoint))
            return;

        // Check user-specific rate limits if authenticated
        if (userId != null)
        {
            if (!await CheckRateLimit(context, endpoint, userId, endpoint))
                return;
        }

        // Check for suspicious activity patterns
        await CheckSuspiciousActivity(context, userId, ipAddress, endpoint);

        await _next(context);
    }

    private async Task<bool> CheckRateLimit(HttpContext context, string category, string identifier, string endpoint)
    {
        if (!_rateLimitConfigs.TryGetValue(category, out var config))
        {
            config = _rateLimitConfigs["api_general"];
        }

        var key = $"{category}_{identifier}";
        var rateLimit = _rateLimits.GetOrAdd(key, _ => new EndpointRateLimit());

        if (!rateLimit.CanMakeRequest(config))
        {
            await LogSecurityEvent(context, "rate_limit_exceeded", new
            {
                category,
                identifier,
                endpoint,
                limit = config.MaxRequests,
                window = config.WindowMinutes
            });

            context.Response.StatusCode = 429;
            context.Response.Headers.Add("Retry-After", (config.LockoutMinutes * 60).ToString());
            context.Response.Headers.Add("X-Rate-Limit-Category", category);

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "Rate limit exceeded",
                category,
                retryAfter = config.LockoutMinutes * 60,
                message = $"Too many requests. Limit: {config.MaxRequests} per {config.WindowMinutes} minutes."
            }));

            return false;
        }

        rateLimit.RegisterRequest(config);
        return true;
    }

    private async Task CheckSuspiciousActivity(HttpContext context, string? userId, string? ipAddress, string endpoint)
    {
        var key = userId ?? ipAddress ?? "unknown";
        var activity = _suspiciousActivity.GetOrAdd(key, _ => new SuspiciousActivity());

        // Check for suspicious patterns
        var isSuspicious = false;
        var reason = "";

        // Rapid-fire requests (more than 1 request per second)
        if (activity.GetRecentRequestCount(TimeSpan.FromSeconds(1)) > 1)
        {
            isSuspicious = true;
            reason = "rapid_fire_requests";
        }

        // Failed auth attempts from same IP
        if (endpoint.StartsWith("auth_") && activity.GetRecentFailureCount(TimeSpan.FromMinutes(5)) > 3)
        {
            isSuspicious = true;
            reason = "multiple_auth_failures";
        }

        // Scanning multiple endpoints rapidly
        if (activity.GetUniqueEndpointCount(TimeSpan.FromMinutes(1)) > 10)
        {
            isSuspicious = true;
            reason = "endpoint_scanning";
        }

        if (isSuspicious)
        {
            await LogSecurityEvent(context, "suspicious_activity_detected", new
            {
                userId,
                ipAddress,
                endpoint,
                reason,
                requestCount = activity.GetRecentRequestCount(TimeSpan.FromMinutes(1)),
                failureCount = activity.GetRecentFailureCount(TimeSpan.FromMinutes(5)),
                uniqueEndpoints = activity.GetUniqueEndpointCount(TimeSpan.FromMinutes(1))
            });
        }

        activity.RegisterRequest(endpoint, context.Response.StatusCode >= 400);
    }

    private string GetEndpointCategory(PathString path, string method)
    {
        var pathValue = path.Value?.ToLower() ?? "";

        // Authentication endpoints
        if (pathValue.Contains("/auth/"))
        {
            if (pathValue.Contains("login")) return "auth_login";
            if (pathValue.Contains("register")) return "auth_register";
            if (pathValue.Contains("password") || pathValue.Contains("reset")) return "auth_password_reset";
        }

        // API endpoints
        if (pathValue.StartsWith("/api/"))
        {
            if (pathValue.Contains("upload")) return "api_upload";
            if (pathValue.Contains("invite")) return "api_invite";
            if (pathValue.Contains("profile") || pathValue.Contains("/me/")) return "api_profile";
            return "api_general";
        }

        return "api_general";
    }

    private string? GetUserId(HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string? GetClientIpAddress(HttpContext context)
    {
        // Check for X-Forwarded-For header (common with proxies/load balancers)
        var forwardedHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedHeader))
        {
            return forwardedHeader.Split(',')[0].Trim();
        }

        // Check for X-Real-IP header
        var realIpHeader = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIpHeader))
        {
            return realIpHeader;
        }

        // Fall back to RemoteIpAddress
        return context.Connection.RemoteIpAddress?.ToString();
    }

    private async Task LogSecurityEvent(HttpContext context, string eventType, object additionalData)
    {
        var logData = new
        {
            timestamp = DateTimeOffset.UtcNow,
            eventType,
            ipAddress = GetClientIpAddress(context),
            userId = GetUserId(context),
            userAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
            path = context.Request.Path.Value,
            method = context.Request.Method,
            additionalData
        };

        _logger.LogWarning("Security Event: {EventType} - {Data}", eventType, JsonSerializer.Serialize(logData));

        // Also log to security monitoring service
        using var scope = _serviceProvider.CreateScope();
        var securityMonitoring = scope.ServiceProvider.GetRequiredService<ISecurityMonitoringService>();

        await securityMonitoring.LogSecurityEventAsync(
            eventType,
            GetUserId(context),
            GetClientIpAddress(context),
            additionalData
        );
    }

    private class RateLimitConfig
    {
        public int MaxRequests { get; }
        public int WindowMinutes { get; }
        public int LockoutMinutes { get; }

        public RateLimitConfig(int maxRequests, int windowMinutes, int lockoutMinutes)
        {
            MaxRequests = maxRequests;
            WindowMinutes = windowMinutes;
            LockoutMinutes = lockoutMinutes;
        }
    }

    private class EndpointRateLimit
    {
        private readonly Queue<DateTime> _requests = new();
        private DateTime? _lockoutUntil = null;
        private readonly object _lock = new();

        public bool CanMakeRequest(RateLimitConfig config)
        {
            lock (_lock)
            {
                // Check if still in lockout period
                if (_lockoutUntil.HasValue && DateTime.UtcNow < _lockoutUntil.Value)
                {
                    return false;
                }

                CleanOldRequests(config.WindowMinutes);

                if (_requests.Count >= config.MaxRequests)
                {
                    // Trigger lockout
                    _lockoutUntil = DateTime.UtcNow.AddMinutes(config.LockoutMinutes);
                    return false;
                }

                return true;
            }
        }

        public void RegisterRequest(RateLimitConfig config)
        {
            lock (_lock)
            {
                CleanOldRequests(config.WindowMinutes);
                _requests.Enqueue(DateTime.UtcNow);
            }
        }

        private void CleanOldRequests(int windowMinutes)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-windowMinutes);
            while (_requests.Count > 0 && _requests.Peek() < cutoff)
            {
                _requests.Dequeue();
            }
        }
    }

    private class SuspiciousActivity
    {
        private readonly Queue<ActivityEvent> _events = new();
        private readonly object _lock = new();

        public void RegisterRequest(string endpoint, bool isFailed)
        {
            lock (_lock)
            {
                CleanOldEvents();
                _events.Enqueue(new ActivityEvent
                {
                    Timestamp = DateTime.UtcNow,
                    Endpoint = endpoint,
                    IsFailed = isFailed
                });
            }
        }

        public int GetRecentRequestCount(TimeSpan window)
        {
            lock (_lock)
            {
                CleanOldEvents();
                var cutoff = DateTime.UtcNow - window;
                return _events.Count(e => e.Timestamp >= cutoff);
            }
        }

        public int GetRecentFailureCount(TimeSpan window)
        {
            lock (_lock)
            {
                CleanOldEvents();
                var cutoff = DateTime.UtcNow - window;
                return _events.Count(e => e.Timestamp >= cutoff && e.IsFailed);
            }
        }

        public int GetUniqueEndpointCount(TimeSpan window)
        {
            lock (_lock)
            {
                CleanOldEvents();
                var cutoff = DateTime.UtcNow - window;
                return _events.Where(e => e.Timestamp >= cutoff)
                             .Select(e => e.Endpoint)
                             .Distinct()
                             .Count();
            }
        }

        private void CleanOldEvents()
        {
            var cutoff = DateTime.UtcNow.AddHours(-1); // Keep 1 hour of history
            while (_events.Count > 0 && _events.Peek().Timestamp < cutoff)
            {
                _events.Dequeue();
            }
        }

        private class ActivityEvent
        {
            public DateTime Timestamp { get; set; }
            public string Endpoint { get; set; } = "";
            public bool IsFailed { get; set; }
        }
    }
}