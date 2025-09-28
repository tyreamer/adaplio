using MudBlazor;

namespace Adaplio.Frontend.Services;

public interface IErrorHandlingService
{
    void HandleError(Exception exception, string? userMessage = null, string? context = null);
    void HandleApiError(string errorMessage, int? statusCode = null, string? context = null);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
    void ShowSuccess(string message);
    Task<T?> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string? errorMessage = null, string? context = null);
    Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string? errorMessage = null, string? context = null);
}

public class ErrorHandlingService : IErrorHandlingService
{
    private readonly ISnackbar _snackbar;
    private readonly ILogger<ErrorHandlingService> _logger;

    public ErrorHandlingService(ISnackbar snackbar, ILogger<ErrorHandlingService> logger)
    {
        _snackbar = snackbar;
        _logger = logger;
    }

    public void HandleError(Exception exception, string? userMessage = null, string? context = null)
    {
        // Log the full exception details
        _logger.LogError(exception, "Error occurred in context: {Context}", context ?? "Unknown");

        // Determine user-friendly message
        var displayMessage = GetUserFriendlyMessage(exception, userMessage);

        // Show to user
        _snackbar.Add(displayMessage, Severity.Error);
    }

    public void HandleApiError(string errorMessage, int? statusCode = null, string? context = null)
    {
        // Log the API error
        _logger.LogWarning("API error in context: {Context}, Status: {StatusCode}, Message: {Message}",
            context ?? "Unknown", statusCode, errorMessage);

        // Determine user-friendly message based on status code
        var displayMessage = GetUserFriendlyApiMessage(errorMessage, statusCode);

        // Show to user
        _snackbar.Add(displayMessage, Severity.Error);
    }

    public void ShowError(string message)
    {
        _snackbar.Add(message, Severity.Error);
    }

    public void ShowWarning(string message)
    {
        _snackbar.Add(message, Severity.Warning);
    }

    public void ShowInfo(string message)
    {
        _snackbar.Add(message, Severity.Info);
    }

    public void ShowSuccess(string message)
    {
        _snackbar.Add(message, Severity.Success);
    }

    public async Task<T?> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string? errorMessage = null, string? context = null)
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            HandleError(ex, errorMessage, context);
            return default;
        }
    }

    public async Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string? errorMessage = null, string? context = null)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            HandleError(ex, errorMessage, context);
        }
    }

    private string GetUserFriendlyMessage(Exception exception, string? userMessage)
    {
        // Use provided user message if available
        if (!string.IsNullOrEmpty(userMessage))
        {
            return userMessage;
        }

        // Generate user-friendly message based on exception type
        return exception switch
        {
            TaskCanceledException => "The operation was cancelled or timed out. Please try again.",
            HttpRequestException => "Network error occurred. Please check your connection and try again.",
            UnauthorizedAccessException => "You don't have permission to perform this action.",
            ArgumentException => "Invalid input provided. Please check your data and try again.",
            InvalidOperationException => "This operation cannot be performed at this time. Please try again later.",
            NotSupportedException => "This operation is not supported in your current environment.",
            _ => "An unexpected error occurred. Please try again or contact support if the problem persists."
        };
    }

    private string GetUserFriendlyApiMessage(string errorMessage, int? statusCode)
    {
        // First try to use the server-provided error message if it's user-friendly
        if (!string.IsNullOrEmpty(errorMessage) && IsUserFriendlyMessage(errorMessage))
        {
            return errorMessage;
        }

        // Fall back to status code-based messages
        return statusCode switch
        {
            400 => "Invalid request. Please check your input and try again.",
            401 => "Your session has expired. Please sign in again.",
            403 => "You don't have permission to perform this action.",
            404 => "The requested resource was not found.",
            409 => "This action conflicts with the current state. Please refresh and try again.",
            422 => "The provided data is invalid. Please check your input.",
            429 => "Too many requests. Please wait a moment before trying again.",
            500 => "A server error occurred. Please try again later.",
            502 or 503 or 504 => "The service is temporarily unavailable. Please try again later.",
            _ => !string.IsNullOrEmpty(errorMessage)
                ? errorMessage
                : "An error occurred while processing your request. Please try again."
        };
    }

    private static bool IsUserFriendlyMessage(string message)
    {
        // Check if the message looks like a user-friendly error message
        // Avoid technical terms, stack traces, etc.
        var technicalTerms = new[] { "Exception", "Error:", "Stack trace", "at ", "System.", "Microsoft.", "null reference" };

        return !technicalTerms.Any(term => message.Contains(term, StringComparison.OrdinalIgnoreCase)) &&
               message.Length < 200 && // Reasonable length
               !message.Contains('\n'); // No multi-line messages
    }
}

public static class ErrorHandlingServiceExtensions
{
    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
        return services;
    }
}