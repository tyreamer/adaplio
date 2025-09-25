using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Adaplio.Frontend;
using Adaplio.Frontend.Theme;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient {
    BaseAddress = new Uri("https://adaplio.onrender.com") // Production API URL
});
builder.Services.AddMudServices();
builder.Services.AddThemeService();

await builder.Build().RunAsync();
