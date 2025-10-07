using System.Net.Http;

namespace Adaplio.RuntimeTests;

/// <summary>
/// Test fixture that provides a configured HttpClient for runtime API testing.
/// This connects to a running instance of the API.
/// </summary>
public class ApiTestFixture : IDisposable
{
    public HttpClient Client { get; }
    public string BaseUrl { get; }

    public ApiTestFixture()
    {
        // Use environment variable or default to local dev
        BaseUrl = Environment.GetEnvironmentVariable("API_TEST_URL") ?? "http://localhost:8080";

        Client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        Client.DefaultRequestHeaders.Add("User-Agent", "Adaplio-RuntimeTests/1.0");
    }

    public void Dispose()
    {
        Client?.Dispose();
    }
}
