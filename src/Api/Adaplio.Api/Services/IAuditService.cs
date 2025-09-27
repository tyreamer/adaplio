namespace Adaplio.Api.Services;

public interface IAuditService
{
    Task LogProfileChangeAsync(int userId, string field, object? oldValue, object? newValue, string? userType = null);
    Task LogSharingScopeChangeAsync(int clientUserId, int trainerUserId, string scope, bool granted);
    Task LogTrainerAccessRevokedAsync(int clientUserId, int trainerUserId);
    Task LogUploadAsync(int userId, string uploadType, string fileName, bool success);
}

public class AuditLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Field { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? AdditionalData { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}