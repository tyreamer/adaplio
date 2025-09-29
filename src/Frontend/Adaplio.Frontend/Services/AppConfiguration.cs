using Microsoft.Extensions.Configuration;

namespace Adaplio.Frontend.Services;

public class AppConfiguration
{
    private readonly IConfiguration _configuration;

    public AppConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string ApiBaseUrl => _configuration["ApiSettings:BaseUrl"] ?? "https://adaplio.onrender.com";
    public bool EnableAnalytics => _configuration.GetValue<bool>("Features:EnableAnalytics", true);
    public bool EnableErrorReporting => _configuration.GetValue<bool>("Features:EnableErrorReporting", true);
}

public static class ConfigurationExtensions
{
    public static IServiceCollection AddAppConfiguration(this IServiceCollection services)
    {
        services.AddScoped<AppConfiguration>();
        return services;
    }
}