using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Adaplio.Frontend;
using Adaplio.Frontend.Theme;
using Adaplio.Frontend.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Custom HttpClient that includes credentials for cross-origin requests
builder.Services.AddScoped<HttpClient>(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri("https://adaplio.onrender.com") };

    // This won't work for Blazor WebAssembly cross-origin, so we'll need to handle this differently
    return httpClient;
});
builder.Services.AddMudServices();
builder.Services.AddLocalStorageService();
builder.Services.AddAuthenticatedHttpClient();
builder.Services.AddErrorHandling();
builder.Services.AddThemeService();

// Add authentication services
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddAuthorizationPolicies();
builder.Services.AddScoped<AuthorizationService>();

// Add profile management services
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<FormValidationService>();
builder.Services.AddScoped<FormStateService>();

await builder.Build().RunAsync();
