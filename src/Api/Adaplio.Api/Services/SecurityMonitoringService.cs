using System.Collections.Concurrent;
using System.Text.Json;

namespace Adaplio.Api.Services;

public interface ISecurityMonitoringService
{
    Task LogSecurityEventAsync(string eventType, string? userId, string? ipAddress, object? additionalData = null);
    Task<SecurityMetrics> GetSecurityMetricsAsync(TimeSpan period);
    Task<List<SecurityAlert>> GetActiveAlertsAsync();
    Task CheckForThreatsAsync();
}

public class SecurityMonitoringService : ISecurityMonitoringService
{
    private readonly ILogger<SecurityMonitoringService> _logger;
    private static readonly ConcurrentQueue<SecurityEvent> _events = new();
    private static readonly ConcurrentDictionary<string, SecurityAlert> _activeAlerts = new();

    // Threat detection thresholds
    private const int FailedAuthThreshold = 10;  // per IP per hour
    private const int RapidRequestThreshold = 100; // per IP per minute
    private const int UniqueUserThreshold = 50;  // unique users per IP per hour

    public SecurityMonitoringService(ILogger<SecurityMonitoringService> logger)
    {
        _logger = logger;
    }

    public async Task LogSecurityEventAsync(string eventType, string? userId, string? ipAddress, object? additionalData = null)
    {
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            UserId = userId,
            IpAddress = ipAddress,
            Timestamp = DateTimeOffset.UtcNow,
            AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
        };

        _events.Enqueue(securityEvent);

        // Cleanup old events (keep 24 hours)
        await CleanupOldEventsAsync();

        // Log to structured logging
        _logger.LogInformation("Security Event: {EventType} from {IpAddress} for user {UserId} - {AdditionalData}",
            eventType, ipAddress, userId, securityEvent.AdditionalData);

        // Check for immediate threats
        await CheckForImmediateThreatsAsync(securityEvent);
    }

    public async Task<SecurityMetrics> GetSecurityMetricsAsync(TimeSpan period)
    {
        var cutoff = DateTimeOffset.UtcNow - period;
        var recentEvents = GetEventsAfter(cutoff).ToList();

        var metrics = new SecurityMetrics
        {
            Period = period,
            TotalEvents = recentEvents.Count,
            FailedAuthAttempts = recentEvents.Count(e => e.EventType.Contains("auth_failed")),
            RateLimitViolations = recentEvents.Count(e => e.EventType.Contains("rate_limit")),
            SuspiciousActivities = recentEvents.Count(e => e.EventType.Contains("suspicious")),
            UniqueIpAddresses = recentEvents.Where(e => !string.IsNullOrEmpty(e.IpAddress))
                                          .Select(e => e.IpAddress)
                                          .Distinct()
                                          .Count(),
            UniqueUsers = recentEvents.Where(e => !string.IsNullOrEmpty(e.UserId))
                                    .Select(e => e.UserId)
                                    .Distinct()
                                    .Count(),
            TopIpAddresses = recentEvents.Where(e => !string.IsNullOrEmpty(e.IpAddress))
                                       .GroupBy(e => e.IpAddress)
                                       .OrderByDescending(g => g.Count())
                                       .Take(10)
                                       .ToDictionary(g => g.Key!, g => g.Count()),
            EventsByType = recentEvents.GroupBy(e => e.EventType)
                                     .ToDictionary(g => g.Key, g => g.Count())
        };

        return metrics;
    }

    public async Task<List<SecurityAlert>> GetActiveAlertsAsync()
    {
        // Remove expired alerts
        var expiredAlerts = _activeAlerts.Where(kv => kv.Value.ExpiresAt < DateTimeOffset.UtcNow)
                                       .Select(kv => kv.Key)
                                       .ToList();

        foreach (var expiredKey in expiredAlerts)
        {
            _activeAlerts.TryRemove(expiredKey, out _);
        }

        return _activeAlerts.Values.OrderByDescending(a => a.Severity).ToList();
    }

    public async Task CheckForThreatsAsync()
    {
        var oneHourAgo = DateTimeOffset.UtcNow.AddHours(-1);
        var oneMinuteAgo = DateTimeOffset.UtcNow.AddMinutes(-1);
        var recentEvents = GetEventsAfter(oneHourAgo).ToList();

        // Check for brute force attacks (failed auth attempts)
        var failedAuthByIp = recentEvents
            .Where(e => e.EventType.Contains("auth_failed") || e.EventType.Contains("login_failed"))
            .Where(e => !string.IsNullOrEmpty(e.IpAddress))
            .GroupBy(e => e.IpAddress)
            .Where(g => g.Count() >= FailedAuthThreshold);

        foreach (var group in failedAuthByIp)
        {
            await CreateSecurityAlert(
                "brute_force_attack",
                SecuritySeverity.High,
                $"Potential brute force attack from IP {group.Key}: {group.Count()} failed auth attempts in 1 hour",
                new { ipAddress = group.Key, attemptCount = group.Count() }
            );
        }

        // Check for rapid requests (potential DDoS or scraping)
        var rapidRequestsByIp = recentEvents
            .Where(e => e.Timestamp >= oneMinuteAgo)
            .Where(e => !string.IsNullOrEmpty(e.IpAddress))
            .GroupBy(e => e.IpAddress)
            .Where(g => g.Count() >= RapidRequestThreshold);

        foreach (var group in rapidRequestsByIp)
        {
            await CreateSecurityAlert(
                "rapid_requests",
                SecuritySeverity.Medium,
                $"Rapid requests from IP {group.Key}: {group.Count()} requests in 1 minute",
                new { ipAddress = group.Key, requestCount = group.Count() }
            );
        }

        // Check for account sharing (multiple users from same IP)
        var usersByIp = recentEvents
            .Where(e => !string.IsNullOrEmpty(e.IpAddress) && !string.IsNullOrEmpty(e.UserId))
            .GroupBy(e => e.IpAddress)
            .Where(g => g.Select(e => e.UserId).Distinct().Count() >= UniqueUserThreshold);

        foreach (var group in usersByIp)
        {
            var uniqueUsers = group.Select(e => e.UserId).Distinct().Count();
            await CreateSecurityAlert(
                "potential_account_sharing",
                SecuritySeverity.Medium,
                $"Multiple users from IP {group.Key}: {uniqueUsers} unique users in 1 hour",
                new { ipAddress = group.Key, uniqueUserCount = uniqueUsers }
            );
        }

        // Check for privilege escalation attempts
        var privilegeEvents = recentEvents
            .Where(e => e.EventType.Contains("unauthorized") || e.EventType.Contains("forbidden"))
            .GroupBy(e => e.UserId)
            .Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() >= 5);

        foreach (var group in privilegeEvents)
        {
            await CreateSecurityAlert(
                "privilege_escalation_attempt",
                SecuritySeverity.High,
                $"Multiple unauthorized access attempts by user {group.Key}: {group.Count()} attempts",
                new { userId = group.Key, attemptCount = group.Count() }
            );
        }
    }

    private async Task CheckForImmediateThreatsAsync(SecurityEvent securityEvent)
    {
        // Check for immediate high-severity threats that need instant alerts

        // Multiple failed auths from same IP in short time
        if (securityEvent.EventType.Contains("auth_failed") && !string.IsNullOrEmpty(securityEvent.IpAddress))
        {
            var recentFailures = GetEventsAfter(DateTimeOffset.UtcNow.AddMinutes(-5))
                .Count(e => e.EventType.Contains("auth_failed") && e.IpAddress == securityEvent.IpAddress);

            if (recentFailures >= 5)
            {
                await CreateSecurityAlert(
                    "immediate_brute_force",
                    SecuritySeverity.Critical,
                    $"Immediate brute force threat from IP {securityEvent.IpAddress}: {recentFailures} failures in 5 minutes",
                    new { ipAddress = securityEvent.IpAddress, failures = recentFailures }
                );
            }
        }

        // SQL injection or XSS attempt patterns
        if (!string.IsNullOrEmpty(securityEvent.AdditionalData))
        {
            var data = securityEvent.AdditionalData.ToLower();
            var maliciousPatterns = new[] { "select ", "union ", "drop ", "<script", "javascript:", "eval(" };

            if (maliciousPatterns.Any(pattern => data.Contains(pattern)))
            {
                await CreateSecurityAlert(
                    "injection_attempt",
                    SecuritySeverity.High,
                    $"Potential injection attempt detected from {securityEvent.IpAddress}",
                    new { ipAddress = securityEvent.IpAddress, eventType = securityEvent.EventType }
                );
            }
        }
    }

    private async Task CreateSecurityAlert(string alertType, SecuritySeverity severity, string message, object? additionalData = null)
    {
        var alertKey = $"{alertType}_{DateTime.UtcNow:yyyyMMddHH}"; // One alert per type per hour

        var alert = new SecurityAlert
        {
            Id = Guid.NewGuid(),
            AlertType = alertType,
            Severity = severity,
            Message = message,
            Timestamp = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24),
            AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null,
            IsResolved = false
        };

        _activeAlerts.AddOrUpdate(alertKey, alert, (key, existing) =>
        {
            // Update existing alert with latest info
            existing.Message = message;
            existing.Timestamp = DateTimeOffset.UtcNow;
            existing.AdditionalData = alert.AdditionalData;
            return existing;
        });

        _logger.LogWarning("Security Alert Created: {AlertType} - {Message} - {AdditionalData}",
            alertType, message, alert.AdditionalData);
    }

    private IEnumerable<SecurityEvent> GetEventsAfter(DateTimeOffset cutoff)
    {
        return _events.Where(e => e.Timestamp >= cutoff);
    }

    private async Task CleanupOldEventsAsync()
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
        var eventsToKeep = new Queue<SecurityEvent>();

        while (_events.TryDequeue(out var evt))
        {
            if (evt.Timestamp >= cutoff)
            {
                eventsToKeep.Enqueue(evt);
            }
        }

        // Put back the events we want to keep
        foreach (var evt in eventsToKeep)
        {
            _events.Enqueue(evt);
        }
    }
}

public class SecurityEvent
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = "";
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? AdditionalData { get; set; }
}

public class SecurityAlert
{
    public Guid Id { get; set; }
    public string AlertType { get; set; } = "";
    public SecuritySeverity Severity { get; set; }
    public string Message { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string? AdditionalData { get; set; }
    public bool IsResolved { get; set; }
}

public class SecurityMetrics
{
    public TimeSpan Period { get; set; }
    public int TotalEvents { get; set; }
    public int FailedAuthAttempts { get; set; }
    public int RateLimitViolations { get; set; }
    public int SuspiciousActivities { get; set; }
    public int UniqueIpAddresses { get; set; }
    public int UniqueUsers { get; set; }
    public Dictionary<string, int> TopIpAddresses { get; set; } = new();
    public Dictionary<string, int> EventsByType { get; set; } = new();
}

public enum SecuritySeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}