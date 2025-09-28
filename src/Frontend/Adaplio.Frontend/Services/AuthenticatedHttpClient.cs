using Adaplio.Frontend.Extensions;

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
            _httpClient.SetBearerToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set authentication token");
        }
    }
}

public static class AuthenticatedHttpClientExtensions
{
    public static IServiceCollection AddAuthenticatedHttpClient(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticatedHttpClient, AuthenticatedHttpClient>();
        return services;
    }
}