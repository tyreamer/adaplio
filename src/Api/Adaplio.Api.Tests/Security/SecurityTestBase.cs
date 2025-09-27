using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using Xunit;

namespace Adaplio.Api.Tests.Security;

public class SecurityTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;

    public SecurityTestBase(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    protected HttpClient CreateClientWithCustomConfiguration(Action<IServiceCollection>? configureServices = null)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            if (configureServices != null)
            {
                builder.ConfigureServices(configureServices);
            }
        });

        return factory.CreateClient();
    }

    protected static StringContent CreateJsonContent(string json)
    {
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }

    protected static string GenerateJwtToken(string secret, string issuer, string audience, int userId, string userType)
    {
        // This would generate a JWT token for testing
        // For security testing, we need both valid and invalid tokens
        return $"test.jwt.token.{userId}.{userType}";
    }

    protected void Dispose()
    {
        _client?.Dispose();
    }
}