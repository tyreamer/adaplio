using Microsoft.JSInterop;
using MudBlazor;

namespace Adaplio.Frontend.Theme;

public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode;
    private bool _isInitialized;

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsDarkMode => _isDarkMode;

    public MudTheme CurrentTheme => _isDarkMode ? AdaplioTheme.DarkTheme : AdaplioTheme.LightTheme;

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            // Check if user has a stored preference
            var storedTheme = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "theme");

            if (string.IsNullOrEmpty(storedTheme))
            {
                // If no stored preference, check system preference
                var prefersDark = await _jsRuntime.InvokeAsync<bool>("window.matchMedia", "(prefers-color-scheme: dark)");
                _isDarkMode = prefersDark;
            }
            else
            {
                _isDarkMode = storedTheme == "dark";
            }

            await ApplyThemeAsync();
            _isInitialized = true;
        }
        catch
        {
            // Fallback to light mode if anything fails
            _isDarkMode = false;
            _isInitialized = true;
        }
    }

    public async Task ToggleThemeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await SaveThemePreferenceAsync();
        await ApplyThemeAsync();
        OnThemeChanged?.Invoke();
    }

    public async Task SetThemeAsync(bool isDarkMode)
    {
        if (_isDarkMode == isDarkMode) return;

        _isDarkMode = isDarkMode;
        await SaveThemePreferenceAsync();
        await ApplyThemeAsync();
        OnThemeChanged?.Invoke();
    }

    private async Task SaveThemePreferenceAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "theme", _isDarkMode ? "dark" : "light");
        }
        catch
        {
            // Ignore localStorage errors
        }
    }

    private async Task ApplyThemeAsync()
    {
        try
        {
            // Apply theme to document root for CSS custom properties
            await _jsRuntime.InvokeVoidAsync("document.documentElement.setAttribute", "data-theme", _isDarkMode ? "dark" : "light");
        }
        catch
        {
            // Ignore DOM manipulation errors
        }
    }
}

public static class ThemeServiceExtensions
{
    public static IServiceCollection AddThemeService(this IServiceCollection services)
    {
        services.AddScoped<ThemeService>();
        return services;
    }
}