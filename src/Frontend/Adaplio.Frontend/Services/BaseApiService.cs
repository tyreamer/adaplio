using Adaplio.Frontend.Extensions;

namespace Adaplio.Frontend.Services;

public abstract class BaseApiService
{
    protected readonly IAuthenticatedHttpClient HttpClient;
    protected readonly IErrorHandlingService ErrorHandler;
    protected readonly ILogger Logger;

    protected BaseApiService(IAuthenticatedHttpClient httpClient, IErrorHandlingService errorHandler, ILogger logger)
    {
        HttpClient = httpClient;
        ErrorHandler = errorHandler;
        Logger = logger;
    }

    /// <summary>
    /// Execute a GET request and handle errors consistently
    /// </summary>
    protected async Task<T?> GetAsync<T>(string endpoint, string? context = null)
    {
        var response = await HttpClient.GetAsync<T>(endpoint);

        if (response.IsSuccess)
        {
            return response.Data;
        }

        ErrorHandler.HandleApiError(
            response.ErrorMessage ?? $"Failed to fetch {typeof(T).Name}",
            response.StatusCode,
            context ?? endpoint);

        return default;
    }

    /// <summary>
    /// Execute a POST request and handle errors consistently
    /// </summary>
    protected async Task<T?> PostAsync<T>(string endpoint, object? data = null, string? context = null)
    {
        var response = await HttpClient.PostAsync<T>(endpoint, data);

        if (response.IsSuccess)
        {
            return response.Data;
        }

        ErrorHandler.HandleApiError(
            response.ErrorMessage ?? $"Failed to create {typeof(T).Name}",
            response.StatusCode,
            context ?? endpoint);

        return default;
    }

    /// <summary>
    /// Execute a POST request without expecting a response body
    /// </summary>
    protected async Task<bool> PostAsync(string endpoint, object? data = null, string? context = null)
    {
        var response = await HttpClient.PostAsync(endpoint, data);

        if (response.IsSuccess)
        {
            return true;
        }

        ErrorHandler.HandleApiError(
            response.ErrorMessage ?? "Operation failed",
            response.StatusCode,
            context ?? endpoint);

        return false;
    }

    /// <summary>
    /// Execute a PUT request and handle errors consistently
    /// </summary>
    protected async Task<T?> PutAsync<T>(string endpoint, object data, string? context = null)
    {
        var response = await HttpClient.PutAsync<T>(endpoint, data);

        if (response.IsSuccess)
        {
            return response.Data;
        }

        ErrorHandler.HandleApiError(
            response.ErrorMessage ?? $"Failed to update {typeof(T).Name}",
            response.StatusCode,
            context ?? endpoint);

        return default;
    }

    /// <summary>
    /// Execute a PUT request without expecting a response body
    /// </summary>
    protected async Task<bool> PutAsync(string endpoint, object data, string? context = null)
    {
        var response = await HttpClient.PutAsync(endpoint, data);

        if (response.IsSuccess)
        {
            return true;
        }

        ErrorHandler.HandleApiError(
            response.ErrorMessage ?? "Update failed",
            response.StatusCode,
            context ?? endpoint);

        return false;
    }

    /// <summary>
    /// Execute a DELETE request and handle errors consistently
    /// </summary>
    protected async Task<bool> DeleteAsync(string endpoint, string? context = null)
    {
        var response = await HttpClient.DeleteAsync(endpoint);

        if (response.IsSuccess)
        {
            return true;
        }

        ErrorHandler.HandleApiError(
            response.ErrorMessage ?? "Delete failed",
            response.StatusCode,
            context ?? endpoint);

        return false;
    }

    /// <summary>
    /// Execute a DELETE request with response body
    /// </summary>
    protected async Task<T?> DeleteAsync<T>(string endpoint, string? context = null)
    {
        var response = await HttpClient.DeleteAsync<T>(endpoint);

        if (response.IsSuccess)
        {
            return response.Data;
        }

        ErrorHandler.HandleApiError(
            response.ErrorMessage ?? $"Failed to delete {typeof(T).Name}",
            response.StatusCode,
            context ?? endpoint);

        return default;
    }

    /// <summary>
    /// Execute an operation with consistent error handling and optional success message
    /// </summary>
    protected async Task<bool> ExecuteOperationAsync(Func<Task<bool>> operation, string? successMessage = null, string? context = null)
    {
        try
        {
            var result = await operation();

            if (result && !string.IsNullOrEmpty(successMessage))
            {
                ErrorHandler.ShowSuccess(successMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, null, context);
            return false;
        }
    }

    /// <summary>
    /// Execute an operation that returns data with consistent error handling
    /// </summary>
    protected async Task<T?> ExecuteOperationAsync<T>(Func<Task<T?>> operation, string? context = null)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, null, context);
            return default;
        }
    }
}