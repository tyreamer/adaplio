using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Adaplio.Frontend.Services;

namespace Adaplio.Frontend.Extensions;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static void SetBearerToken(this HttpClient httpClient, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            httpClient.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public static void ClearBearerToken(this HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public static async Task<ApiResponse<T>> GetApiAsync<T>(this HttpClient httpClient, string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(requestUri, cancellationToken);
            return await ProcessApiResponse<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.FromError($"Request failed: {ex.Message}");
        }
    }

    public static async Task<ApiResponse<T>> PostApiAsync<T>(this HttpClient httpClient, string requestUri, object? value = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = value == null
                ? await httpClient.PostAsync(requestUri, null, cancellationToken)
                : await httpClient.PostAsJsonAsync(requestUri, value, DefaultJsonOptions, cancellationToken);

            return await ProcessApiResponse<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.FromError($"Request failed: {ex.Message}");
        }
    }

    public static async Task<ApiResponse<T>> PutApiAsync<T>(this HttpClient httpClient, string requestUri, object value, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(requestUri, value, DefaultJsonOptions, cancellationToken);
            return await ProcessApiResponse<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.FromError($"Request failed: {ex.Message}");
        }
    }

    public static async Task<ApiResponse<T>> DeleteApiAsync<T>(this HttpClient httpClient, string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync(requestUri, cancellationToken);
            return await ProcessApiResponse<T>(response);
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.FromError($"Request failed: {ex.Message}");
        }
    }

    // Overloads for void responses
    public static async Task<ApiResponse> GetApiAsync(this HttpClient httpClient, string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync(requestUri, cancellationToken);
            return await ProcessApiResponse(response);
        }
        catch (Exception ex)
        {
            return ApiResponse.FromError($"Request failed: {ex.Message}");
        }
    }

    public static async Task<ApiResponse> PostApiAsync(this HttpClient httpClient, string requestUri, object? value = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = value == null
                ? await httpClient.PostAsync(requestUri, null, cancellationToken)
                : await httpClient.PostAsJsonAsync(requestUri, value, DefaultJsonOptions, cancellationToken);

            return await ProcessApiResponse(response);
        }
        catch (Exception ex)
        {
            return ApiResponse.FromError($"Request failed: {ex.Message}");
        }
    }

    public static async Task<ApiResponse> PutApiAsync(this HttpClient httpClient, string requestUri, object value, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync(requestUri, value, DefaultJsonOptions, cancellationToken);
            return await ProcessApiResponse(response);
        }
        catch (Exception ex)
        {
            return ApiResponse.FromError($"Request failed: {ex.Message}");
        }
    }

    public static async Task<ApiResponse> DeleteApiAsync(this HttpClient httpClient, string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync(requestUri, cancellationToken);
            return await ProcessApiResponse(response);
        }
        catch (Exception ex)
        {
            return ApiResponse.FromError($"Request failed: {ex.Message}");
        }
    }

    private static async Task<ApiResponse<T>> ProcessApiResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            if (string.IsNullOrEmpty(content))
            {
                return ApiResponse<T>.FromSuccess(default);
            }

            try
            {
                var data = JsonSerializer.Deserialize<T>(content, DefaultJsonOptions);
                return ApiResponse<T>.FromSuccess(data);
            }
            catch (JsonException ex)
            {
                return ApiResponse<T>.FromError($"Failed to deserialize response: {ex.Message}");
            }
        }

        // Try to parse error response
        var errorMessage = await ParseErrorMessage(response, content);
        return ApiResponse<T>.FromError(errorMessage, (int)response.StatusCode);
    }

    private static async Task<ApiResponse> ProcessApiResponse(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return ApiResponse.FromSuccess();
        }

        var content = await response.Content.ReadAsStringAsync();
        var errorMessage = await ParseErrorMessage(response, content);
        return ApiResponse.FromError(errorMessage, (int)response.StatusCode);
    }

    private static async Task<string> ParseErrorMessage(HttpResponseMessage response, string content)
    {
        // Try to parse structured error response
        if (!string.IsNullOrEmpty(content))
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(content, DefaultJsonOptions);
                if (!string.IsNullOrEmpty(errorResponse?.Message))
                {
                    return errorResponse.Message;
                }
            }
            catch
            {
                // If parsing fails, fall back to content or status
            }
        }

        // Fall back to status code-based messages
        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Authentication required. Please sign in again.",
            System.Net.HttpStatusCode.Forbidden => "You don't have permission to access this resource.",
            System.Net.HttpStatusCode.NotFound => "The requested resource was not found.",
            System.Net.HttpStatusCode.InternalServerError => "A server error occurred. Please try again later.",
            System.Net.HttpStatusCode.BadRequest => "Invalid request. Please check your input and try again.",
            _ => $"Request failed with status {response.StatusCode}"
        };
    }

    private class ErrorResponse
    {
        public string? Message { get; set; }
        public string? Error { get; set; }
        public object? Details { get; set; }
    }
}

// Standard API response types
public class ApiResponse
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public int? StatusCode { get; init; }

    protected ApiResponse(bool isSuccess, string? errorMessage = null, int? statusCode = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
    }

    public static ApiResponse FromSuccess() => new(true);
    public static ApiResponse FromError(string errorMessage, int? statusCode = null) => new(false, errorMessage, statusCode);
}

public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }

    private ApiResponse(bool isSuccess, T? data = default, string? errorMessage = null, int? statusCode = null)
        : base(isSuccess, errorMessage, statusCode)
    {
        Data = data;
    }

    public static ApiResponse<T> FromSuccess(T? data) => new(true, data);
    public static new ApiResponse<T> FromError(string errorMessage, int? statusCode = null) => new(false, default, errorMessage, statusCode);
}