using Adaplio.Frontend.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Adaplio.Frontend.Services;

public class ProfileService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProfileService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Cache for profile data
    private ProfileResponse? _cachedProfile;
    private DateTime? _lastFetch;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public ProfileService(HttpClient httpClient, ILogger<ProfileService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<ProfileResponse?> GetProfileAsync(bool forceRefresh = false)
    {
        try
        {
            // Return cached data if available and not expired
            if (!forceRefresh && _cachedProfile != null && _lastFetch.HasValue &&
                DateTime.UtcNow - _lastFetch.Value < _cacheExpiry)
            {
                return _cachedProfile;
            }

            var response = await _httpClient.GetAsync("/api/me/profile");

            if (response.IsSuccessStatusCode)
            {
                var profile = await response.Content.ReadFromJsonAsync<ProfileResponse>(_jsonOptions);

                // Update cache
                _cachedProfile = profile;
                _lastFetch = DateTime.UtcNow;

                return profile;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Profile doesn't exist yet, return null
                return null;
            }
            else
            {
                _logger.LogError("Failed to fetch profile. Status: {StatusCode}", response.StatusCode);
                throw new HttpRequestException($"Failed to fetch profile: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching profile");
            throw;
        }
    }

    public async Task<ProfileResponse> UpdateProfileAsync(UpdateProfileRequest request)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync("/api/me/profile", request, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var updatedProfile = await response.Content.ReadFromJsonAsync<ProfileResponse>(_jsonOptions);

                // Update cache with new data
                _cachedProfile = updatedProfile;
                _lastFetch = DateTime.UtcNow;

                return updatedProfile!;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update profile. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update profile: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            throw;
        }
    }

    public async Task<string> GetUploadUrlAsync(string fileName, string contentType, long fileSize)
    {
        try
        {
            var request = new
            {
                FileName = fileName,
                ContentType = contentType,
                FileSize = fileSize
            };

            var response = await _httpClient.PostAsJsonAsync("/api/upload/presigned-url", request, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<UploadUrlResponse>(_jsonOptions);
                return result!.UploadUrl;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get upload URL. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to get upload URL: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upload URL");
            throw;
        }
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
                var publicUrl = ExtractPublicUrl(uploadUrl);
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

    public async Task UpdateTrainerSharingAsync(int trainerId, TrainerSharingScope scope)
    {
        try
        {
            var request = new { Scope = scope };
            var response = await _httpClient.PatchAsJsonAsync($"/api/client/trainers/{trainerId}/scope", request, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to update trainer sharing. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to update trainer sharing: {response.StatusCode}");
            }

            // Invalidate cache since sharing settings changed
            InvalidateCache();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating trainer sharing");
            throw;
        }
    }

    public async Task RemoveTrainerAsync(int trainerId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/client/trainers/{trainerId}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to remove trainer. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to remove trainer: {response.StatusCode}");
            }

            // Invalidate cache since trainer relationship changed
            InvalidateCache();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing trainer");
            throw;
        }
    }

    public async Task<byte[]> ExportDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/me/export");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to export data. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to export data: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data");
            throw;
        }
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