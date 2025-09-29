using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Net.Http.Headers;
using Adaplio.Frontend.Extensions;

namespace Adaplio.Frontend.Services;

public class AuthStateService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private UserInfo? _currentUser;
    private bool _isInitialized = false;
    private string? _authToken;

    public AuthStateService(HttpClient httpClient, ILocalStorageService localStorage)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
    }

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated => _currentUser != null;
    public string? UserRole => _currentUser?.UserType;
    public string? UserId => _currentUser?.UserId;
    public string? Email => _currentUser?.Email;
    public string? Alias => _currentUser?.Alias;
    public string? DisplayName => _currentUser?.DisplayName;
    public string? FullName => _currentUser?.FullName;
    public bool IsClient => UserRole == "client";
    public bool IsTrainer => UserRole == "trainer";
    public bool IsInitialized => _isInitialized;
    public UserInfo? User => _currentUser;

    public async Task<bool> InitializeAsync()
    {
        if (_isInitialized)
            return IsAuthenticated;

        try
        {
            // Try to get token from localStorage
            _authToken = await _localStorage.GetItemAsync<string>("auth_token");

            if (!string.IsNullOrEmpty(_authToken))
            {
                _httpClient.SetBearerToken(_authToken);

                var response = await _httpClient.GetApiAsync<UserInfo>("/auth/me");
                if (response.IsSuccess && response.Data != null)
                {
                    _currentUser = response.Data;
                }
                else
                {
                    // Token is invalid or API call failed, clear it
                    await ClearAuthenticationAsync();
                }
            }
            else
            {
                // No token found
                await ClearAuthenticationAsync();
            }
        }
        catch (Exception)
        {
            // Any initialization error, clear auth state
            await ClearAuthenticationAsync();
        }

        _isInitialized = true;
        NotifyAuthStateChanged();
        return IsAuthenticated;
    }

    private async Task ClearAuthenticationAsync()
    {
        try
        {
            await _localStorage.RemoveItemAsync("auth_token");
        }
        catch { }

        _authToken = null;
        _httpClient.ClearBearerToken();
        _currentUser = null;
    }

    public async Task<bool> RefreshUserAsync()
    {
        _isInitialized = false;
        return await InitializeAsync();
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _httpClient.PostAsync("/auth/logout", null);
        }
        catch
        {
            // Ignore logout errors
        }
        finally
        {
            await _localStorage.RemoveItemAsync("auth_token");
            _authToken = null;
            _httpClient.ClearBearerToken();
            _currentUser = null;
            _isInitialized = true;
            NotifyAuthStateChanged();
        }
    }

    public async Task SetUserAsync(UserInfo userInfo, string? token = null)
    {
        _currentUser = userInfo;

        if (!string.IsNullOrEmpty(token))
        {
            _authToken = token;
            await _localStorage.SetItemAsync("auth_token", token);
            _httpClient.SetBearerToken(token);
        }

        _isInitialized = true;
        NotifyAuthStateChanged();
    }

    public void SetUser(UserInfo userInfo)
    {
        _currentUser = userInfo;
        _isInitialized = true;
        NotifyAuthStateChanged();
    }

    public async Task ClearUserAsync()
    {
        await _localStorage.RemoveItemAsync("auth_token");
        _authToken = null;
        _httpClient.ClearBearerToken();
        _currentUser = null;
        _isInitialized = true;
        NotifyAuthStateChanged();
    }

    public void ClearUser()
    {
        _currentUser = null;
        _isInitialized = true;
        NotifyAuthStateChanged();
    }



    public async Task<bool> UpdateProfileAsync(string? displayName)
    {
        try
        {
            var request = new { DisplayName = displayName };
            var response = await _httpClient.PutAsJsonAsync("/auth/profile", request);

            if (response.IsSuccessStatusCode)
            {
                // Update the current user info
                if (_currentUser != null)
                {
                    _currentUser.DisplayName = displayName;
                    NotifyAuthStateChanged();
                }
                return true;
            }
        }
        catch
        {
            // Ignore errors for now
        }

        return false;
    }

    public async Task SetPreferredRoleAsync(string preferredRole)
    {
        await _localStorage.SetItemAsync("preferred_role", preferredRole);
    }

    public async Task<string?> GetPreferredRoleAsync()
    {
        return await _localStorage.GetItemAsync("preferred_role");
    }

    public async Task ClearPreferredRoleAsync()
    {
        await _localStorage.RemoveItemAsync("preferred_role");
    }

    public async Task<bool> SetUserRoleAsync(string role)
    {
        try
        {
            var request = new { Role = role };
            var response = await _httpClient.PostAsJsonAsync("/auth/role", request);

            if (response.IsSuccessStatusCode)
            {
                // Update the current user info
                if (_currentUser != null)
                {
                    _currentUser.UserType = role;
                    NotifyAuthStateChanged();
                }
                return true;
            }
        }
        catch
        {
            // Ignore errors for now
        }

        return false;
    }

    private void NotifyAuthStateChanged()
    {
        OnAuthStateChanged?.Invoke();
    }
}

public class UserInfo
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string UserType { get; set; } = "";
    public string? Alias { get; set; }
    public string? DisplayName { get; set; }
    public string? FullName { get; set; }
    public string? PracticeName { get; set; }
    public bool IsVerified { get; set; }
}