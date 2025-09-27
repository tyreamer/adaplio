using Adaplio.Frontend.Services;
using System.Security.Claims;

namespace Adaplio.Frontend.Services;

public class AuthorizationService
{
    private readonly AuthStateService _authState;
    private readonly ILogger<AuthorizationService> _logger;

    public AuthorizationService(AuthStateService authState, ILogger<AuthorizationService> logger)
    {
        _authState = authState;
        _logger = logger;
    }

    // Profile access permissions
    public async Task<bool> CanEditProfileAsync()
    {
        return await IsAuthenticatedAsync();
    }

    public async Task<bool> CanViewProfileAsync()
    {
        return await IsAuthenticatedAsync();
    }

    public async Task<bool> CanDeleteAccountAsync()
    {
        var isAuthenticated = await IsAuthenticatedAsync();

        // Additional business rules can be added here
        // For example: account must be older than 30 days, no active subscriptions, etc.

        return isAuthenticated;
    }

    // Data management permissions
    public async Task<bool> CanExportDataAsync()
    {
        return await IsAuthenticatedAsync();
    }

    public async Task<bool> CanManageNotificationsAsync()
    {
        return await IsAuthenticatedAsync();
    }

    // Privacy and sharing permissions
    public async Task<bool> CanManagePrivacySettingsAsync()
    {
        // Only clients can manage trainer sharing settings
        return await IsClientAsync();
    }

    public async Task<bool> CanViewConnectedTrainersAsync()
    {
        return await IsClientAsync();
    }

    public async Task<bool> CanRemoveTrainerAccessAsync()
    {
        return await IsClientAsync();
    }

    // Trainer-specific permissions
    public async Task<bool> CanManageClinicInfoAsync()
    {
        return await IsTrainerAsync();
    }

    public async Task<bool> CanManageSpecialtiesAsync()
    {
        return await IsTrainerAsync();
    }

    public async Task<bool> CanUploadLogoAsync()
    {
        return await IsTrainerAsync();
    }

    public async Task<bool> CanSetDefaultReminderTimeAsync()
    {
        return await IsTrainerAsync();
    }

    // Client-specific permissions
    public async Task<bool> CanManageHealthInfoAsync()
    {
        return await IsClientAsync();
    }

    public async Task<bool> CanManageEmergencyContactAsync()
    {
        return await IsClientAsync();
    }

    public async Task<bool> CanManageAccessibilitySettingsAsync()
    {
        return await IsClientAsync();
    }

    // Security permissions
    public async Task<bool> CanEnable2FAAsync()
    {
        // 2FA is typically available for trainers only (higher privilege accounts)
        return await IsTrainerAsync();
    }

    public async Task<bool> CanManagePasskeysAsync()
    {
        return await IsAuthenticatedAsync();
    }

    public async Task<bool> CanChangeEmailAsync()
    {
        return await IsAuthenticatedAsync();
    }

    // Navigation permissions
    public async Task<bool> CanAccessProfilePageAsync()
    {
        return await IsAuthenticatedAsync();
    }

    public async Task<bool> CanAccessAccountSettingsAsync()
    {
        return await IsAuthenticatedAsync();
    }

    public async Task<bool> CanAccessNotificationSettingsAsync()
    {
        return await IsAuthenticatedAsync();
    }

    public async Task<bool> CanAccessPrivacySettingsAsync()
    {
        return await IsClientAsync();
    }

    // Helper methods
    private async Task<bool> IsAuthenticatedAsync()
    {
        if (!_authState.IsInitialized)
        {
            await _authState.InitializeAsync();
        }

        return _authState.IsAuthenticated;
    }

    private async Task<bool> IsClientAsync()
    {
        if (!await IsAuthenticatedAsync())
            return false;

        return _authState.IsClient;
    }

    private async Task<bool> IsTrainerAsync()
    {
        if (!await IsAuthenticatedAsync())
            return false;

        return _authState.IsTrainer;
    }

    // Business rule validations
    public async Task<AuthorizationResult> ValidateProfileEditAsync(string? userId = null)
    {
        if (!await IsAuthenticatedAsync())
        {
            return AuthorizationResult.Unauthorized("User must be authenticated to edit profile");
        }

        // Check if user is editing their own profile
        if (!string.IsNullOrEmpty(userId) && _authState.User?.UserId != userId)
        {
            return AuthorizationResult.Forbidden("Users can only edit their own profile");
        }

        return AuthorizationResult.Success();
    }

    public async Task<AuthorizationResult> ValidateTrainerConnectionAsync(int trainerId)
    {
        if (!await IsClientAsync())
        {
            return AuthorizationResult.Forbidden("Only clients can manage trainer connections");
        }

        // Additional validation could include:
        // - Check if trainer exists
        // - Check if client is already connected to trainer
        // - Check trainer's availability for new clients

        return AuthorizationResult.Success();
    }

    public async Task<AuthorizationResult> ValidateDataExportAsync()
    {
        if (!await IsAuthenticatedAsync())
        {
            return AuthorizationResult.Unauthorized("User must be authenticated to export data");
        }

        // Additional business rules could include:
        // - Rate limiting (max 1 export per day)
        // - Account age requirements
        // - Verification requirements

        return AuthorizationResult.Success();
    }

    // Security-sensitive operations
    public async Task<AuthorizationResult> ValidateAccountDeletionAsync()
    {
        if (!await IsAuthenticatedAsync())
        {
            return AuthorizationResult.Unauthorized("User must be authenticated to delete account");
        }

        // Business rules for account deletion
        var user = _authState.User;
        if (user == null)
        {
            return AuthorizationResult.Forbidden("User information not available");
        }

        // Example business rules:
        // - Account must be older than 7 days
        // - No active subscriptions
        // - No pending payments

        return AuthorizationResult.Success();
    }
}

public class AuthorizationResult
{
    public bool IsAuthorized { get; }
    public string? ErrorMessage { get; }

    private AuthorizationResult(bool isAuthorized, string? errorMessage = null)
    {
        IsAuthorized = isAuthorized;
        ErrorMessage = errorMessage;
    }

    public static AuthorizationResult Success() => new(true);
    public static AuthorizationResult Unauthorized(string message) => new(false, message);
    public static AuthorizationResult Forbidden(string message) => new(false, message);
}