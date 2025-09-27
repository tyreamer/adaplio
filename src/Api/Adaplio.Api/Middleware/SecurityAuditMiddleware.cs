using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Adaplio.Api.Services;

namespace Adaplio.Api.Middleware;

public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityAuditMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger, IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        // Only audit sensitive endpoints to avoid performance impact
        var shouldAudit = ShouldAuditRequest(context.Request.Path, context.Request.Method);

        string? requestBody = null;
        string? responseBody = null;

        if (shouldAudit)
        {
            // Capture request body for sensitive operations
            requestBody = await CaptureRequestBody(context);

            // Capture response body
            using var responseMemoryStream = new MemoryStream();
            context.Response.Body = responseMemoryStream;

            await _next(context);

            responseBody = await CaptureResponseBody(context, responseMemoryStream, originalBodyStream);
        }
        else
        {
            await _next(context);
        }

        stopwatch.Stop();

        if (shouldAudit)
        {
            await LogSecurityAudit(context, stopwatch.ElapsedMilliseconds, requestBody, responseBody);

            // Log security event to monitoring service
            using var scope = _serviceProvider.CreateScope();
            var securityMonitoring = scope.ServiceProvider.GetRequiredService<ISecurityMonitoringService>();

            var eventType = context.Response.StatusCode >= 400 ? "audit_failed" : "audit_success";
            await securityMonitoring.LogSecurityEventAsync(
                eventType,
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                GetClientIpAddress(context),
                new {
                    path = context.Request.Path.Value,
                    method = context.Request.Method,
                    statusCode = context.Response.StatusCode,
                    elapsedMs = stopwatch.ElapsedMilliseconds
                }
            );
        }
    }

    private bool ShouldAuditRequest(PathString path, string method)
    {
        var pathValue = path.Value?.ToLower() ?? "";

        // Always audit authentication attempts
        if (pathValue.Contains("/auth/"))
            return true;

        // Audit sensitive API operations
        if (pathValue.StartsWith("/api/"))
        {
            // Profile operations
            if (pathValue.Contains("profile") || pathValue.Contains("/me/"))
                return true;

            // Invite operations
            if (pathValue.Contains("invite") || pathValue.Contains("grant"))
                return true;

            // Upload operations
            if (pathValue.Contains("upload"))
                return true;

            // Administrative operations
            if (pathValue.Contains("admin") || pathValue.Contains("manage"))
                return true;

            // Consent and privacy operations
            if (pathValue.Contains("consent") || pathValue.Contains("privacy"))
                return true;
        }

        return false;
    }

    private async Task<string?> CaptureRequestBody(HttpContext context)
    {
        try
        {
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Sanitize sensitive data
            return SanitizeRequestData(body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture request body for audit");
            return null;
        }
    }

    private async Task<string?> CaptureResponseBody(HttpContext context, MemoryStream responseMemoryStream, Stream originalBodyStream)
    {
        try
        {
            responseMemoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseMemoryStream).ReadToEndAsync();

            responseMemoryStream.Seek(0, SeekOrigin.Begin);
            await responseMemoryStream.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;

            // Sanitize sensitive response data
            return SanitizeResponseData(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture response body for audit");
            return null;
        }
    }

    private string SanitizeRequestData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        try
        {
            var json = JsonDocument.Parse(data);
            var sanitized = SanitizeJsonElement(json.RootElement);
            return JsonSerializer.Serialize(sanitized);
        }
        catch
        {
            // If not valid JSON, just mask potential sensitive patterns
            return MaskSensitivePatterns(data);
        }
    }

    private string SanitizeResponseData(string data)
    {
        if (string.IsNullOrEmpty(data))
            return data;

        try
        {
            var json = JsonDocument.Parse(data);
            var sanitized = SanitizeJsonElement(json.RootElement);
            return JsonSerializer.Serialize(sanitized);
        }
        catch
        {
            // If not valid JSON, just mask potential sensitive patterns
            return MaskSensitivePatterns(data);
        }
    }

    private object SanitizeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    var key = property.Name.ToLower();

                    // Mask sensitive fields
                    if (IsSensitiveField(key))
                    {
                        obj[property.Name] = "***REDACTED***";
                    }
                    else
                    {
                        obj[property.Name] = SanitizeJsonElement(property.Value);
                    }
                }
                return obj;

            case JsonValueKind.Array:
                var array = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(SanitizeJsonElement(item));
                }
                return array;

            case JsonValueKind.String:
                var stringValue = element.GetString() ?? "";
                return MaskSensitivePatterns(stringValue);

            case JsonValueKind.Number:
                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return element.ToString();
        }
    }

    private bool IsSensitiveField(string fieldName)
    {
        var sensitiveFields = new[]
        {
            "password", "token", "secret", "key", "auth", "credential",
            "ssn", "social", "dob", "dateofbirth", "birthdate",
            "medicalrecord", "diagnosis", "medication", "allergy",
            "phonenumber", "phone", "email", "address", "zip", "postal",
            "creditcard", "payment", "bank", "account"
        };

        return sensitiveFields.Any(field => fieldName.Contains(field));
    }

    private string MaskSensitivePatterns(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Mask potential email patterns
        input = System.Text.RegularExpressions.Regex.Replace(
            input, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", "***EMAIL***");

        // Mask potential phone patterns
        input = System.Text.RegularExpressions.Regex.Replace(
            input, @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", "***PHONE***");

        // Mask potential SSN patterns
        input = System.Text.RegularExpressions.Regex.Replace(
            input, @"\b\d{3}-?\d{2}-?\d{4}\b", "***SSN***");

        return input;
    }

    private async Task LogSecurityAudit(HttpContext context, long elapsedMs, string? requestBody, string? responseBody)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value;
        var userType = context.User.FindFirst("user_type")?.Value;

        var auditData = new
        {
            // Request details
            timestamp = DateTimeOffset.UtcNow,
            method = context.Request.Method,
            path = context.Request.Path.Value,
            queryString = context.Request.QueryString.Value,
            userAgent = context.Request.Headers["User-Agent"].FirstOrDefault(),
            ipAddress = GetClientIpAddress(context),

            // User details
            userId,
            userEmail,
            userType,
            isAuthenticated = context.User.Identity?.IsAuthenticated ?? false,

            // Response details
            statusCode = context.Response.StatusCode,
            elapsedMs,

            // Request/Response bodies (sanitized)
            requestBody = requestBody?.Length > 1000 ? requestBody[..1000] + "..." : requestBody,
            responseBody = responseBody?.Length > 1000 ? responseBody[..1000] + "..." : responseBody,

            // Security indicators
            isSuccessful = context.Response.StatusCode < 400,
            isSensitiveEndpoint = true,

            // Additional headers
            contentType = context.Request.ContentType,
            acceptLanguage = context.Request.Headers["Accept-Language"].FirstOrDefault(),
            referer = context.Request.Headers["Referer"].FirstOrDefault()
        };

        // Different log levels based on outcome
        if (context.Response.StatusCode >= 400)
        {
            _logger.LogWarning("Security Audit - Failed Request: {AuditData}", JsonSerializer.Serialize(auditData));
        }
        else if (IsHighRiskOperation(context.Request.Path, context.Request.Method))
        {
            _logger.LogWarning("Security Audit - High Risk Operation: {AuditData}", JsonSerializer.Serialize(auditData));
        }
        else
        {
            _logger.LogInformation("Security Audit - Request: {AuditData}", JsonSerializer.Serialize(auditData));
        }
    }

    private bool IsHighRiskOperation(PathString path, string method)
    {
        var pathValue = path.Value?.ToLower() ?? "";

        // High-risk operations that warrant special attention
        var highRiskPatterns = new[]
        {
            "/auth/login",
            "/auth/register",
            "/api/me/profile",
            "/api/upload",
            "/api/trainer/grants",
            "/api/invites/accept",
            "/api/consent",
            "/api/privacy"
        };

        return highRiskPatterns.Any(pattern => pathValue.Contains(pattern)) ||
               (method == "DELETE" && pathValue.StartsWith("/api/"));
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
}