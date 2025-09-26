using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.JSInterop;
using System.Net.Http.Headers;

namespace Adaplio.Frontend.Services;

public class AuthStateService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private UserInfo? _currentUser;
    private bool _isInitialized = false;
    private string? _authToken;

    public AuthStateService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated => _currentUser != null;
    public string? UserRole => _currentUser?.UserType;
    public string? UserId => _currentUser?.UserId;
    public string? Email => _currentUser?.Email;
    public string? Alias => _currentUser?.Alias;
    public string? FullName => _currentUser?.FullName;
    public bool IsClient => UserRole == "client";
    public bool IsTrainer => UserRole == "trainer";

    public async Task<bool> InitializeAsync()
    {
        if (_isInitialized)
            return IsAuthenticated;

        try
        {
            // Try to get token from localStorage
            _authToken = await GetTokenFromStorage();

            if (!string.IsNullOrEmpty(_authToken))
            {
                SetAuthorizationHeader(_authToken);

                var response = await _httpClient.GetAsync("/auth/me");
                if (response.IsSuccessStatusCode)
                {
                    var userInfo = await response.Content.ReadFromJsonAsync<UserInfo>();
                    _currentUser = userInfo;
                }
                else
                {
                    // Token is invalid, clear it
                    await ClearTokenFromStorage();
                    _authToken = null;
                    ClearAuthorizationHeader();
                    _currentUser = null;
                }
            }
            else
            {
                _currentUser = null;
            }
        }
        catch
        {
            _currentUser = null;
        }

        _isInitialized = true;
        NotifyAuthStateChanged();
        return IsAuthenticated;
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
            await ClearTokenFromStorage();
            _authToken = null;
            ClearAuthorizationHeader();
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
            await SaveTokenToStorage(token);
            SetAuthorizationHeader(token);
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
        await ClearTokenFromStorage();
        _authToken = null;
        ClearAuthorizationHeader();
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

    private async Task<string?> GetTokenFromStorage()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "auth_token");
        }
        catch
        {
            return null;
        }
    }

    private async Task SaveTokenToStorage(string token)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "auth_token", token);
        }
        catch
        {
            // Ignore storage errors
        }
    }

    private async Task ClearTokenFromStorage()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_token");
        }
        catch
        {
            // Ignore storage errors
        }
    }

    private void SetAuthorizationHeader(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void ClearAuthorizationHeader()
    {
        _httpClient.DefaultRequestHeaders.Authorization = null;
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
    public string? FullName { get; set; }
    public string? PracticeName { get; set; }
    public bool IsVerified { get; set; }
}