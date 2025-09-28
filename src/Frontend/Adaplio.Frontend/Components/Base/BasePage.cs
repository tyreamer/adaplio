using Microsoft.AspNetCore.Components;
using Adaplio.Frontend.Services;

namespace Adaplio.Frontend.Components.Base;

public abstract class BasePage : ComponentBase
{
    [Inject] protected AuthStateService AuthState { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] protected ILogger<BasePage> Logger { get; set; } = default!;
    [Inject] protected IErrorHandlingService ErrorHandler { get; set; } = default!;

    protected bool IsLoading { get; set; } = true;
    protected bool IsInitialized { get; set; } = false;
    protected string? ErrorMessage { get; set; }

    protected virtual bool RequireAuthentication => false;
    protected virtual bool RequireSpecificRole => false;
    protected virtual string? RequiredRole => null;
    protected virtual string? RedirectUrlIfUnauthenticated => "/";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            IsLoading = true;
            StateHasChanged();

            await InitializeAuthenticationAsync();

            if (!ValidateAccess())
            {
                return;
            }

            await OnPageInitializedAsync();

            IsInitialized = true;
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, "Failed to load page", GetType().Name);
            ErrorMessage = "An error occurred while loading the page. Please try again.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected virtual async Task InitializeAuthenticationAsync()
    {
        if (!AuthState.IsInitialized)
        {
            await AuthState.InitializeAsync();
        }
    }

    protected virtual bool ValidateAccess()
    {
        if (RequireAuthentication && !AuthState.IsAuthenticated)
        {
            Navigation.NavigateTo(RedirectUrlIfUnauthenticated ?? "/");
            return false;
        }

        if (RequireSpecificRole && !string.IsNullOrEmpty(RequiredRole))
        {
            if (AuthState.UserRole != RequiredRole)
            {
                Navigation.NavigateTo("/unauthorized");
                return false;
            }
        }

        return true;
    }

    protected virtual async Task OnPageInitializedAsync()
    {
        // Override in derived classes for page-specific initialization
        await Task.CompletedTask;
    }

    protected virtual async Task RefreshPageAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            StateHasChanged();

            await OnPageRefreshAsync();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, "Failed to refresh page", GetType().Name);
            ErrorMessage = "An error occurred while refreshing the page. Please try again.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected virtual async Task OnPageRefreshAsync()
    {
        // Override in derived classes for page-specific refresh logic
        await OnPageInitializedAsync();
    }

    protected virtual void HandleError(Exception ex, string? userMessage = null)
    {
        ErrorHandler.HandleError(ex, userMessage, GetType().Name);
        ErrorMessage = userMessage ?? "An unexpected error occurred. Please try again.";
        StateHasChanged();
    }

    protected virtual async Task WithLoadingAsync(Func<Task> action, string? errorMessage = null)
    {
        try
        {
            IsLoading = true;
            StateHasChanged();

            await action();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, errorMessage, GetType().Name);
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected virtual async Task<T?> WithLoadingAsync<T>(Func<Task<T>> action, string? errorMessage = null)
    {
        try
        {
            IsLoading = true;
            StateHasChanged();

            return await action();
        }
        catch (Exception ex)
        {
            ErrorHandler.HandleError(ex, errorMessage, GetType().Name);
            return default;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }
}

public abstract class AuthenticatedPage : BasePage
{
    protected override bool RequireAuthentication => true;
}

public abstract class ClientPage : AuthenticatedPage
{
    protected override bool RequireSpecificRole => true;
    protected override string RequiredRole => "client";
}

public abstract class TrainerPage : AuthenticatedPage
{
    protected override bool RequireSpecificRole => true;
    protected override string RequiredRole => "trainer";
}