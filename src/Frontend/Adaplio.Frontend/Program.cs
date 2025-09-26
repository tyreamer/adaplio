using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Adaplio.Frontend;
using Adaplio.Frontend.Theme;
using Adaplio.Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient(new HttpClientHandler()
    {
        UseCookies = true
    })
    {
        BaseAddress = new Uri("https://adaplio.onrender.com") // Production API URL
    };

    // Ensure credentials (cookies) are included in cross-origin requests
    httpClient.DefaultRequestHeaders.Add("credentials", "include");

    return httpClient;
});
builder.Services.AddMudServices();
builder.Services.AddThemeService();

// Add authentication services
builder.Services.AddScoped<AuthStateService>();

await builder.Build().RunAsync();
