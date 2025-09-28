using Adaplio.Frontend.Models;
using Adaplio.Frontend.Extensions;
using System.Net.Http.Json;
using System.Text.Json;

namespace Adaplio.Frontend.Services;

public class ProfileService : BaseApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfileService> _logger;

    // Cache for profile data
    private ProfileResponse? _cachedProfile;
    private DateTime? _lastFetch;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public ProfileService(IAuthenticatedHttpClient httpClient, HttpClient rawHttpClient, IErrorHandlingService errorHandler, ILogger<ProfileService> logger)
        : base(httpClient, errorHandler, logger)
    {
        _httpClient = rawHttpClient;
        _logger = logger;
    }

    public async Task<ProfileResponse?> GetProfileAsync(bool forceRefresh = false)
    {
        // Return cached data if available and not expired
        if (!forceRefresh && _cachedProfile != null && _lastFetch.HasValue &&
            DateTime.UtcNow - _lastFetch.Value < _cacheExpiry)
        {
            return _cachedProfile;
        }

        var profile = await GetAsync<ProfileResponse>("/api/me/profile", "GetProfile");

        if (profile != null)
        {
            // Update cache
            _cachedProfile = profile;
            _lastFetch = DateTime.UtcNow;
        }

        return profile;
    }

    public async Task<ProfileResponse?> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var profile = await PutAsync<ProfileResponse>("/api/me/profile", request, "UpdateProfile");

        if (profile != null)
        {
            // Update cache with new data
            _cachedProfile = profile;
            _lastFetch = DateTime.UtcNow;
        }

        return profile;
    }

    public async Task<string?> GetUploadUrlAsync(string fileName, string contentType, long fileSize)
    {
        var request = new
        {
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize
        };

        var result = await PostAsync<UploadUrlResponse>("/api/upload/presigned-url", request, "GetUploadUrl");
        return result?.UploadUrl;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            // Get pre-signed URL
            var uploadUrl = await GetUploadUrlAsync(fileName, contentType, fileStream.Length);

            // Upload file to the pre-signed URL
            using var content = new StreamContent(fileStream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            var uploadResponse = await _httpClient.PutAsync(uploadUrl, content);

            if (uploadResponse.IsSuccessStatusCode)
            {
                // Return the public URL (extract from the upload URL or construct it)
                var publicUrl = uploadUrl != null ? ExtractPublicUrl(uploadUrl) : null;
                return publicUrl;
            }
            else
            {
                _logger.LogError("Failed to upload file. Status: {StatusCode}", uploadResponse.StatusCode);
                throw new HttpRequestException($"Failed to upload file: {uploadResponse.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            throw;
        }
    }

    public async Task<bool> UpdateTrainerSharingAsync(int trainerId, TrainerSharingScope scope)
    {
        var request = new { Scope = scope };
        var success = await PutAsync($"/api/client/trainers/{trainerId}/scope", request, "UpdateTrainerSharing");

        if (success)
        {
            // Invalidate cache since sharing settings changed
            InvalidateCache();
        }

        return success;
    }

    public async Task<bool> RemoveTrainerAsync(int trainerId)
    {
        var success = await DeleteAsync($"/api/client/trainers/{trainerId}", "RemoveTrainer");

        if (success)
        {
            // Invalidate cache since trainer relationship changed
            InvalidateCache();
        }

        return success;
    }

    public async Task<byte[]?> ExportDataAsync()
    {
        return await GetAsync<byte[]>("/api/me/export", "ExportData");
    }

    public void InvalidateCache()
    {
        _cachedProfile = null;
        _lastFetch = null;
    }

    private string ExtractPublicUrl(string uploadUrl)
    {
        // For pre-signed URLs, extract the base URL without query parameters
        var uri = new Uri(uploadUrl);
        return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
    }
}

// DTOs for API communication
public class ProfileResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Timezone { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActive { get; set; }

    // Client-specific data
    public ClientProfileData? ClientProfile { get; set; }

    // Trainer-specific data
    public TrainerProfileData? TrainerProfile { get; set; }

    // Connected trainers (for clients)
    public List<ConnectedTrainerData>? ConnectedTrainers { get; set; }
}

public class ClientProfileData
{
    public string? Injury { get; set; }
    public string? AffectedSide { get; set; }
    public DateTime? InjuryDate { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool HighContrast { get; set; }
    public bool LargeText { get; set; }
    public bool ReducedMotion { get; set; }
}

public class TrainerProfileData
{
    public string? Credentials { get; set; }
    public string? LicenseNumber { get; set; }
    public string? ClinicName { get; set; }
    public string? Location { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Bio { get; set; }
    public List<string> Specialties { get; set; } = new();
    public TimeSpan? DefaultReminderTime { get; set; }
    public string? LogoUrl { get; set; }
}

public class ConnectedTrainerData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Clinic { get; set; }
    public TrainerSharingScope Scope { get; set; }
    public DateTime ConnectedAt { get; set; }
}

public class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
    public string? Timezone { get; set; }
    public string? AvatarUrl { get; set; }
    public ClientProfileData? ClientProfile { get; set; }
    public TrainerProfileData? TrainerProfile { get; set; }
}

public class UploadUrlResponse
{
    public string UploadUrl { get; set; } = "";
    public string PublicUrl { get; set; } = "";
}

public enum TrainerSharingScope
{
    None = 0,
    Summary = 1,
    Detailed = 2
}