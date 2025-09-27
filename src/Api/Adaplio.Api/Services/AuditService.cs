using Adaplio.Api.Data;
using System.Text.Json;

namespace Adaplio.Api.Services;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(
        AppDbContext context,
        ILogger<AuditService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogProfileChangeAsync(int userId, string field, object? oldValue, object? newValue, string? userType = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = "profile_update",
                Field = field,
                OldValue = SerializeValue(oldValue),
                NewValue = SerializeValue(newValue),
                AdditionalData = userType != null ? JsonSerializer.Serialize(new { userType }) : null,
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent()
            };

            // For now, just log to the console/logger since we don't have an audit table in the DB yet
            // In a full implementation, you would save this to a dedicated audit table
            _logger.LogInformation(
                "Profile update: UserId={UserId}, Field={Field}, OldValue={OldValue}, NewValue={NewValue}, UserType={UserType}",
                userId, field, auditLog.OldValue, auditLog.NewValue, userType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log profile change for user {UserId}", userId);
        }
    }

    public async Task LogSharingScopeChangeAsync(int clientUserId, int trainerUserId, string scope, bool granted)
    {
        try
        {
            var additionalData = JsonSerializer.Serialize(new
            {
                trainerUserId,
                scope,
                granted
            });

            var auditLog = new AuditLog
            {
                UserId = clientUserId,
                Action = granted ? "sharing_scope_granted" : "sharing_scope_revoked",
                Field = scope,
                NewValue = granted.ToString(),
                AdditionalData = additionalData,
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent()
            };

            _logger.LogInformation(
                "Sharing scope change: ClientUserId={ClientUserId}, TrainerUserId={TrainerUserId}, Scope={Scope}, Granted={Granted}",
                clientUserId, trainerUserId, scope, granted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log sharing scope change for client {ClientUserId}", clientUserId);
        }
    }

    public async Task LogTrainerAccessRevokedAsync(int clientUserId, int trainerUserId)
    {
        try
        {
            var additionalData = JsonSerializer.Serialize(new { trainerUserId });

            var auditLog = new AuditLog
            {
                UserId = clientUserId,
                Action = "trainer_access_revoked",
                AdditionalData = additionalData,
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent()
            };

            _logger.LogInformation(
                "Trainer access revoked: ClientUserId={ClientUserId}, TrainerUserId={TrainerUserId}",
                clientUserId, trainerUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log trainer access revocation for client {ClientUserId}", clientUserId);
        }
    }

    public async Task LogUploadAsync(int userId, string uploadType, string fileName, bool success)
    {
        try
        {
            var additionalData = JsonSerializer.Serialize(new
            {
                uploadType,
                fileName,
                success
            });

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = success ? "upload_success" : "upload_failed",
                Field = uploadType,
                AdditionalData = additionalData,
                Timestamp = DateTimeOffset.UtcNow,
                IpAddress = GetClientIpAddress(),
                UserAgent = GetUserAgent()
            };

            _logger.LogInformation(
                "Upload attempt: UserId={UserId}, UploadType={UploadType}, FileName={FileName}, Success={Success}",
                userId, uploadType, fileName, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log upload attempt for user {UserId}", userId);
        }
    }

    private string? SerializeValue(object? value)
    {
        if (value == null) return null;

        try
        {
            if (value is string str)
                return str;

            return JsonSerializer.Serialize(value);
        }
        catch
        {
            return value.ToString();
        }
    }

    private string? GetClientIpAddress()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        // Check for X-Forwarded-For header (common with proxies/load balancers)
        var forwardedHeader = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedHeader))
        {
            // Take the first IP in case of multiple proxies
            return forwardedHeader.Split(',')[0].Trim();
        }

        // Check for X-Real-IP header
        var realIpHeader = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIpHeader))
        {
            return realIpHeader;
        }

        // Fall back to RemoteIpAddress
        return httpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetUserAgent()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        return httpContext?.Request.Headers["User-Agent"].FirstOrDefault();
    }
}