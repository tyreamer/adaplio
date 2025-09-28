using Microsoft.JSInterop;
using System.Text.Json;

namespace Adaplio.Frontend.Services;

public interface ILocalStorageService
{
    Task<T?> GetItemAsync<T>(string key);
    Task<string?> GetItemAsync(string key);
    Task SetItemAsync<T>(string key, T value);
    Task SetItemAsync(string key, string value);
    Task RemoveItemAsync(string key);
    Task ClearAsync();
    Task<bool> ContainsKeyAsync(string key);
}

public class LocalStorageService : ILocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public LocalStorageService(IJSRuntime jsRuntime, ILogger<LocalStorageService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);

            if (string.IsNullOrEmpty(json))
                return default;

            // Handle primitive types
            if (typeof(T) == typeof(string))
                return (T)(object)json;

            if (typeof(T) == typeof(bool))
            {
                return bool.TryParse(json, out var boolValue) ? (T)(object)boolValue : default;
            }

            if (typeof(T) == typeof(int))
            {
                return int.TryParse(json, out var intValue) ? (T)(object)intValue : default;
            }

            // Handle complex types with JSON deserialization
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve item '{Key}' from localStorage", key);
            return default;
        }
    }

    public async Task<string?> GetItemAsync(string key)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve string item '{Key}' from localStorage", key);
            return null;
        }
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        try
        {
            string json;

            // Handle primitive types
            if (typeof(T) == typeof(string))
            {
                json = value?.ToString() ?? string.Empty;
            }
            else if (typeof(T) == typeof(bool) || typeof(T) == typeof(int))
            {
                json = value?.ToString() ?? string.Empty;
            }
            else
            {
                // Handle complex types with JSON serialization
                json = JsonSerializer.Serialize(value, _jsonOptions);
            }

            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store item '{Key}' in localStorage", key);
        }
    }

    public async Task SetItemAsync(string key, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store string item '{Key}' in localStorage", key);
        }
    }

    public async Task RemoveItemAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove item '{Key}' from localStorage", key);
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.clear");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clear localStorage");
        }
    }

    public async Task<bool> ContainsKeyAsync(string key)
    {
        try
        {
            var value = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check if localStorage contains key '{Key}'", key);
            return false;
        }
    }
}

public static class LocalStorageServiceExtensions
{
    public static IServiceCollection AddLocalStorageService(this IServiceCollection services)
    {
        services.AddScoped<ILocalStorageService, LocalStorageService>();
        return services;
    }
}