using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Adaplio.Api.Services;

namespace Adaplio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SecurityController : ControllerBase
{
    private readonly ISecurityMonitoringService _securityMonitoring;
    private readonly ILogger<SecurityController> _logger;

    public SecurityController(ISecurityMonitoringService securityMonitoring, ILogger<SecurityController> logger)
    {
        _securityMonitoring = securityMonitoring;
        _logger = logger;
    }

    [HttpGet("metrics")]
    [Authorize(Roles = "admin,trainer")]
    public async Task<IActionResult> GetSecurityMetrics([FromQuery] int? hours = 24)
    {
        try
        {
            var period = TimeSpan.FromHours(hours ?? 24);
            var metrics = await _securityMonitoring.GetSecurityMetricsAsync(period);

            return Ok(new
            {
                success = true,
                data = metrics,
                generated_at = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security metrics");
            return StatusCode(500, new { error = "Failed to retrieve security metrics" });
        }
    }

    [HttpGet("alerts")]
    [Authorize(Roles = "admin,trainer")]
    public async Task<IActionResult> GetActiveAlerts()
    {
        try
        {
            var alerts = await _securityMonitoring.GetActiveAlertsAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    alerts = alerts,
                    count = alerts.Count,
                    critical_count = alerts.Count(a => a.Severity == SecuritySeverity.Critical),
                    high_count = alerts.Count(a => a.Severity == SecuritySeverity.High)
                },
                generated_at = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security alerts");
            return StatusCode(500, new { error = "Failed to retrieve security alerts" });
        }
    }

    [HttpPost("check-threats")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> RunThreatDetection()
    {
        try
        {
            await _securityMonitoring.CheckForThreatsAsync();

            return Ok(new
            {
                success = true,
                message = "Threat detection completed",
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run threat detection");
            return StatusCode(500, new { error = "Failed to run threat detection" });
        }
    }

    [HttpPost("log-event")]
    [Authorize(Roles = "admin,trainer")]
    public async Task<IActionResult> LogSecurityEvent([FromBody] LogSecurityEventRequest request)
    {
        try
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _securityMonitoring.LogSecurityEventAsync(
                request.EventType,
                userId,
                ipAddress,
                request.AdditionalData
            );

            return Ok(new
            {
                success = true,
                message = "Security event logged",
                timestamp = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event");
            return StatusCode(500, new { error = "Failed to log security event" });
        }
    }
}

public class LogSecurityEventRequest
{
    public string EventType { get; set; } = "";
    public object? AdditionalData { get; set; }
}