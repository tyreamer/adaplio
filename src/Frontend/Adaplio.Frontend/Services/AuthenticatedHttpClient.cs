using Adaplio.Frontend.Extensions;
using System.IdentityModel.Tokens.Jwt;

namespace Adaplio.Frontend.Services;

public interface IAuthenticatedHttpClient
{
    Task<ApiResponse<T>> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default);
    Task<ApiResponse> GetAsync(string requestUri, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> PostAsync<T>(string requestUri, object? value = null, CancellationToken cancellationToken = default);
    Task<ApiResponse> PostAsync(string requestUri, object? value = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> PutAsync<T>(string requestUri, object value, CancellationToken cancellationToken = default);
    Task<ApiResponse> PutAsync(string requestUri, object value, CancellationToken cancellationToken = default);
    Task<ApiResponse<T>> DeleteAsync<T>(string requestUri, CancellationToken cancellationToken = default);
    Task<ApiResponse> DeleteAsync(string requestUri, CancellationToken cancellationToken = default);
}

public class AuthenticatedHttpClient : IAuthenticatedHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly ILogger<AuthenticatedHttpClient> _logger;
    private readonly SemaphoreSlim _refreshSemaphore = new(1, 1);
    private bool _isRefreshing = false;

    public AuthenticatedHttpClient(HttpClient httpClient, ILocalStorageService localStorage, ILogger<AuthenticatedHttpClient> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _logger = logger;
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync();
        return await _httpClient.GetApiAsync<T>(requestUri, cancellationToken);
    }

    public async Task<ApiResponse> GetAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync();
        return await _httpClient.GetApiAsync(requestUri, cancellationToken);
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string requestUri, object? value = null, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync();
        return await _httpClient.PostApiAsync<T>(requestUri, value, cancellationToken);
    }

    public async Task<ApiResponse> PostAsync(string requestUri, object? value = null, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync();
        return await _httpClient.PostApiAsync(requestUri, value, cancellationToken);
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string requestUri, object value, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync();
        return await _httpClient.PutApiAsync<T>(requestUri, value, cancellationToken);
    }

    public async Task<ApiResponse> PutAsync(string requestUri, object value, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync();
        return await _httpClient.PutApiAsync(requestUri, value, cancellationToken);
    }

    public async Task<ApiResponse<T>> DeleteAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync();
        return await _httpClient.DeleteApiAsync<T>(requestUri, cancellationToken);
    }

    public async Task<ApiResponse> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync();
        return await _httpClient.DeleteApiAsync(requestUri, cancellationToken);
    }

    private async Task EnsureAuthenticatedAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync("auth_token");

            // Check if token is expired or close to expiry (within 5 minutes)
            if (!string.IsNullOrEmpty(token) && IsTokenExpiringSoon(token))
            {
                _logger.LogInformation("Access token is expiring soon, attempting silent refresh");

                // Attempt silent refresh
                var refreshed = await TryRefreshTokenAsync();

                if (refreshed)
                {
                    // Get the new token
                    token = await _localStorage.GetItemAsync("auth_token");
                }
                else
                {
                    _logger.LogWarning("Failed to refresh token, user may need to login again");
                }
            }

            _httpClient.SetBearerToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set authentication token");
        }
    }

    private bool IsTokenExpiringSoon(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // Check if token expires within 5 minutes
            var expiryTime = jwtToken.ValidTo;
            var timeUntilExpiry = expiryTime - DateTime.UtcNow;

            return timeUntilExpiry.TotalMinutes < 5;
        }
        catch
        {
            // If we can't parse the token, assume it's expired
            return true;
        }
    }

    private async Task<bool> TryRefreshTokenAsync()
    {
        // Prevent concurrent refresh requests
        await _refreshSemaphore.WaitAsync();

        try
        {
            // Check if another thread already refreshed the token
            if (_isRefreshing)
            {
                return true;
            }

            _isRefreshing = true;

            _logger.LogInformation("Attempting to refresh access token");

            // Call the refresh endpoint (cookies are sent automatically)
            var response = await _httpClient.PostAsync("/auth/refresh", null);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var authResponse = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(content, new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (authResponse?.Token != null)
                {
                    // Store the new token
                    await _localStorage.SetItemAsync("auth_token", authResponse.Token);
                    _logger.LogInformation("Access token refreshed successfully");
                    return true;
                }
            }

            _logger.LogWarning("Token refresh failed with status code: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return false;
        }
        finally
        {
            _isRefreshing = false;
            _refreshSemaphore.Release();
        }
    }
}

// DTO for auth response
public class AuthResponse
{
    public string? Message { get; set; }
    public string? UserType { get; set; }
    public string? UserId { get; set; }
    public string? Alias { get; set; }
    public string? Token { get; set; }
}

public static class AuthenticatedHttpClientExtensions
{
    public static IServiceCollection AddAuthenticatedHttpClient(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticatedHttpClient, AuthenticatedHttpClient>();
        return services;
    }
}