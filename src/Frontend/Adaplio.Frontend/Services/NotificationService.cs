using System.Net.Http.Json;
using System.Text.Json;

namespace Adaplio.Frontend.Services;

public class NotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Cache for notification settings
    private NotificationSettings? _cachedSettings;
    private DateTime? _lastFetch;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);

    public NotificationService(HttpClient httpClient, ILogger<NotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<NotificationSettings> GetSettingsAsync(bool forceRefresh = false)
    {
        try
        {
            // Return cached data if available and not expired
            if (!forceRefresh && _cachedSettings != null && _lastFetch.HasValue &&
                DateTime.UtcNow - _lastFetch.Value < _cacheExpiry)
            {
                return _cachedSettings;
            }

            var response = await _httpClient.GetAsync("/api/me/notifications");

            if (response.IsSuccessStatusCode)
            {
                var settings = await response.Content.ReadFromJsonAsync<NotificationSettings>(_jsonOptions);

                // Update cache
                _cachedSettings = settings;
                _lastFetch = DateTime.UtcNow;

                return settings!;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Return default settings if none exist
                var defaultSettings = GetDefaultSettings();
                _cachedSettings = defaultSettings;
                _lastFetch = DateTime.UtcNow;
                return defaultSettings;
            }
            else
            {
                _logger.LogError("Failed to fetch notification settings. Status: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"Failed to fetch notification settings: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notification settings");
            throw;
        }
    }

    public async Task<NotificationSettings> UpdateSettingsAsync(NotificationSettings settings)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("/api/me/notifications", settings, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var updatedSettings = await response.Content.ReadFromJsonAsync<NotificationSettings>(_jsonOptions);

                // Update cache with new data
                _cachedSettings = updatedSettings;
                _lastFetch = DateTime.UtcNow;

                return updatedSettings!;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update notification settings. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update notification settings: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings");
            throw;
        }
    }

    public async Task SendTestNotificationAsync(string channel)
    {
        try
        {
            var request = new { Channel = channel };
            var response = await _httpClient.PostAsJsonAsync("/api/me/notifications/test", request, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send test notification. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to send test notification: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification");
            throw;
        }
    }

    public async Task<bool> CheckPhoneVerificationAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/me/phone/verification-status");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PhoneVerificationStatus>(_jsonOptions);
                return result?.IsVerified ?? false;
            }
            else
            {
                _logger.LogWarning("Failed to check phone verification status. Status: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking phone verification status");
            return false;
        }
    }

    public void InvalidateCache()
    {
        _cachedSettings = null;
        _lastFetch = null;
    }

    private NotificationSettings GetDefaultSettings()
    {
        return new NotificationSettings
        {
            NotificationsEnabled = true,
            PushEnabled = true,
            EmailEnabled = true,
            SmsEnabled = false,
            ReminderTime = new TimeSpan(19, 0, 0),
            QuietHoursEnabled = true,
            QuietHoursStart = new TimeSpan(22, 0, 0),
            QuietHoursEnd = new TimeSpan(7, 0, 0),
            AllowDataAnalytics = true,
            AllowResearchParticipation = false
        };
    }
}

public class NotificationSettings
{
    public bool NotificationsEnabled { get; set; } = true;
    public bool PushEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; } = false;
    public TimeSpan? ReminderTime { get; set; } = new TimeSpan(19, 0, 0);
    public bool QuietHoursEnabled { get; set; } = true;
    public TimeSpan? QuietHoursStart { get; set; } = new TimeSpan(22, 0, 0);
    public TimeSpan? QuietHoursEnd { get; set; } = new TimeSpan(7, 0, 0);
    public bool AllowDataAnalytics { get; set; } = true;
    public bool AllowResearchParticipation { get; set; } = false;
}

public class PhoneVerificationStatus
{
    public bool IsVerified { get; set; }
    public string? PhoneNumber { get; set; }
}