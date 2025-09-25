using Adaplio.Api.Auth;
using Adaplio.Api.Data;
using Adaplio.Api.Dev;
using Adaplio.Api.Plans;
using Adaplio.Api.Progress;
using Adaplio.Api.Services;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL/SQLite support
var dbProvider = Environment.GetEnvironmentVariable("DB_PROVIDER") ?? "sqlite";
var conn = Environment.GetEnvironmentVariable("DB_CONNECTION") ?? "Data Source=db.sqlite";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider.Equals("pgsql", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(conn);
    }
    else
    {
        options.UseSqlite(conn);
    }
});

// Add authentication services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAliasService, AliasService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IPlanService, PlanService>();

// Add JWT authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "your-256-bit-secret-key-here-make-it-long-enough-for-security";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "adaplio-api",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "adaplio-frontend",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Read token from HttpOnly cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["auth_token"];
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Add CORS
var corsOrigins = Environment.GetEnvironmentVariable("CORS__ORIGINS") ?? "https://localhost:5001,http://localhost:5000";
var allowedOrigins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                               .Select(origin => origin.Trim())
                               .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created and migrated
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment())
    {
        context.Database.EnsureCreated();
    }
    else
    {
        // In production, run migrations
        context.Database.Migrate();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add rate limiting
app.UseIpRateLimiting();

// Add CORS
app.UseCors("AllowFrontend");

// Add authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Map authentication endpoints
app.MapAuthEndpoints();

// Map consent endpoints
app.MapConsentEndpoints();

// Map progress endpoints
app.MapProgressEndpoints();

// Map plan endpoints
app.MapPlanEndpoints();

// Map development endpoints (only in development)
if (app.Environment.IsDevelopment())
{
    app.MapDevEndpoints();
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
