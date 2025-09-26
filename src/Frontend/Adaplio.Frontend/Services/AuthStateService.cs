using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace Adaplio.Frontend.Services;

public class AuthStateService
{
    private readonly HttpClient _httpClient;
    private UserInfo? _currentUser;
    private bool _isInitialized = false;

    public AuthStateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
            var response = await _httpClient.GetAsync("/auth/me");
            if (response.IsSuccessStatusCode)
            {
                var userInfo = await response.Content.ReadFromJsonAsync<UserInfo>();
                _currentUser = userInfo;
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
            _currentUser = null;
            _isInitialized = true;
            NotifyAuthStateChanged();
        }
    }

    public void SetUser(UserInfo userInfo)
    {
        _currentUser = userInfo;
        _isInitialized = true;
        NotifyAuthStateChanged();
    }

    public void ClearUser()
    {
        _currentUser = null;
        _isInitialized = true;
        NotifyAuthStateChanged();
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