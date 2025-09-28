using Adaplio.Frontend.Services;

namespace Adaplio.Frontend.Authorization;

public interface IAuthorizationPolicyProvider
{
    Task<AuthorizationResult> EvaluatePolicyAsync(string policyName);
    Task<AuthorizationResult> EvaluateRoleAsync(string role);
    Task<AuthorizationResult> EvaluateAsync(AuthorizeAttribute authorizeAttribute);
}

public class AuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly AuthStateService _authState;
    private readonly ILogger<AuthorizationPolicyProvider> _logger;

    public AuthorizationPolicyProvider(AuthStateService authState, ILogger<AuthorizationPolicyProvider> logger)
    {
        _authState = authState;
        _logger = logger;
    }

    public async Task<AuthorizationResult> EvaluatePolicyAsync(string policyName)
    {
        await EnsureInitializedAsync();

        return policyName switch
        {
            "RequireAuthentication" => _authState.IsAuthenticated
                ? AuthorizationResult.Success()
                : AuthorizationResult.Unauthorized("Authentication required"),

            "RequireClient" => _authState.IsClient
                ? AuthorizationResult.Success()
                : AuthorizationResult.Forbidden("Client role required"),

            "RequireTrainer" => _authState.IsTrainer
                ? AuthorizationResult.Success()
                : AuthorizationResult.Forbidden("Trainer role required"),

            "ProfileEdit" => await EvaluateProfileEditAsync(),
            "DataExport" => await EvaluateDataExportAsync(),
            "TrainerManagement" => await EvaluateTrainerManagementAsync(),
            "ClientManagement" => await EvaluateClientManagementAsync(),

            _ => AuthorizationResult.Forbidden($"Unknown policy: {policyName}")
        };
    }

    public async Task<AuthorizationResult> EvaluateRoleAsync(string role)
    {
        await EnsureInitializedAsync();

        if (!_authState.IsAuthenticated)
        {
            return AuthorizationResult.Unauthorized("Authentication required");
        }

        var userRole = _authState.UserRole?.ToLowerInvariant();
        var requiredRole = role.ToLowerInvariant();

        if (userRole == requiredRole)
        {
            return AuthorizationResult.Success();
        }

        // Support comma-separated roles
        if (role.Contains(','))
        {
            var roles = role.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(r => r.Trim().ToLowerInvariant());

            if (roles.Contains(userRole))
            {
                return AuthorizationResult.Success();
            }
        }

        return AuthorizationResult.Forbidden($"Role '{role}' required, but user has role '{userRole}'");
    }

    public async Task<AuthorizationResult> EvaluateAsync(AuthorizeAttribute authorizeAttribute)
    {
        if (authorizeAttribute.RequireAuthentication)
        {
            await EnsureInitializedAsync();

            if (!_authState.IsAuthenticated)
            {
                return AuthorizationResult.Unauthorized("Authentication required");
            }
        }

        if (!string.IsNullOrEmpty(authorizeAttribute.Roles))
        {
            var roleResult = await EvaluateRoleAsync(authorizeAttribute.Roles);
            if (!roleResult.IsAuthorized)
            {
                return roleResult;
            }
        }

        if (!string.IsNullOrEmpty(authorizeAttribute.Policy))
        {
            var policyResult = await EvaluatePolicyAsync(authorizeAttribute.Policy);
            if (!policyResult.IsAuthorized)
            {
                return policyResult;
            }
        }

        return AuthorizationResult.Success();
    }

    private async Task EnsureInitializedAsync()
    {
        if (!_authState.IsInitialized)
        {
            await _authState.InitializeAsync();
        }
    }

    private async Task<AuthorizationResult> EvaluateProfileEditAsync()
    {
        await EnsureInitializedAsync();

        if (!_authState.IsAuthenticated)
        {
            return AuthorizationResult.Unauthorized("Authentication required to edit profile");
        }

        return AuthorizationResult.Success();
    }

    private async Task<AuthorizationResult> EvaluateDataExportAsync()
    {
        await EnsureInitializedAsync();

        if (!_authState.IsAuthenticated)
        {
            return AuthorizationResult.Unauthorized("Authentication required to export data");
        }

        // Additional business rules can be added here
        // Example: Rate limiting, account age requirements, etc.

        return AuthorizationResult.Success();
    }

    private async Task<AuthorizationResult> EvaluateTrainerManagementAsync()
    {
        await EnsureInitializedAsync();

        if (!_authState.IsAuthenticated)
        {
            return AuthorizationResult.Unauthorized("Authentication required");
        }

        if (!_authState.IsTrainer)
        {
            return AuthorizationResult.Forbidden("Trainer role required for trainer management");
        }

        return AuthorizationResult.Success();
    }

    private async Task<AuthorizationResult> EvaluateClientManagementAsync()
    {
        await EnsureInitializedAsync();

        if (!_authState.IsAuthenticated)
        {
            return AuthorizationResult.Unauthorized("Authentication required");
        }

        if (!_authState.IsClient)
        {
            return AuthorizationResult.Forbidden("Client role required for client management");
        }

        return AuthorizationResult.Success();
    }
}

public static class AuthorizationPolicyProviderExtensions
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
        return services;
    }
}