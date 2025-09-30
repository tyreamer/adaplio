using Adaplio.Api.Analytics;
using Adaplio.Api.Auth;
using Adaplio.Api.Data;
using Adaplio.Api.Dev;
using Adaplio.Api.Gamification;
using Adaplio.Api.Middleware;
using Adaplio.Api.Plans;
using Adaplio.Api.Profile;
using Adaplio.Api.Progress;
using Adaplio.Api.Services;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with PostgreSQL/SQLite support
var dbProvider = Environment.GetEnvironmentVariable("DB_PROVIDER") ?? "sqlite";
var conn = Environment.GetEnvironmentVariable("DB_CONNECTION") ?? "Data Source=db.sqlite";

// Log connection details (without password) and network capabilities
if (dbProvider.Equals("pgsql", StringComparison.OrdinalIgnoreCase))
{
    var safeConn = System.Text.RegularExpressions.Regex.Replace(conn, @"Password=[^;]+", "Password=***");
    Console.WriteLine($"PostgreSQL Connection: {safeConn}");

    // Check IPv6 support
    Console.WriteLine($"IPv6 Supported: {Socket.OSSupportsIPv6}");
    Console.WriteLine($"IPv4 Supported: {Socket.OSSupportsIPv4}");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider.Equals("pgsql", StringComparison.OrdinalIgnoreCase))
    {
        // Use simple connection string for Supabase
        options.UseNpgsql(conn, npgsqlOptions =>
        {
            // Set command timeout
            npgsqlOptions.CommandTimeout(60);

            // Enable retry on failure for transient errors
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        });
    }
    else
    {
        options.UseSqlite(conn);
    }
});

// Add authentication services
builder.Services.AddScoped<IJwtService, JwtService>();

// Add HTTP clients with proper service registration
builder.Services.AddHttpClient<IEmailService, EmailService>();
builder.Services.AddHttpClient<ISMSService, SMSService>();
builder.Services.AddScoped<IAliasService, AliasService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IInputSanitizer, InputSanitizer>();
builder.Services.AddScoped<ISecurityMonitoringService, SecurityMonitoringService>();

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

    // Read token from Authorization header or HttpOnly cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // First try Authorization header (standard Bearer token)
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                context.Token = authHeader.Substring("Bearer ".Length).Trim();
            }
            // Fallback to cookie for backward compatibility
            else if (context.Request.Cookies.ContainsKey("auth_token"))
            {
                context.Token = context.Request.Cookies["auth_token"];
            }

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

Console.WriteLine($"CORS Origins: {string.Join(", ", allowedOrigins)}");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Add rate limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Add HttpContextAccessor for audit logging
builder.Services.AddHttpContextAccessor();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure the port from environment variable (for Railway/Render deployment)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
Console.WriteLine($"Starting server on port {port}");

var app = builder.Build();

// Ensure database is created and migrated (run in background to not block startup)
_ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Check if we should skip migrations (useful for deployment issues)
        var skipMigrations = Environment.GetEnvironmentVariable("SKIP_MIGRATIONS")?.ToLower() == "true";

        if (skipMigrations)
        {
            Console.WriteLine("Skipping database migrations (SKIP_MIGRATIONS=true)");
            return;
        }

        if (app.Environment.IsDevelopment())
        {
            Console.WriteLine("Development mode: Ensuring database created");
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            // In production, run migrations with retry logic
            Console.WriteLine("Production mode: Running database migrations");
            var maxRetries = 3;
            var retryDelay = TimeSpan.FromSeconds(5);

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await context.Database.MigrateAsync();
                    Console.WriteLine("Database migrations completed successfully");
                    break;
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    Console.WriteLine($"Migration attempt {i + 1} failed: {ex.Message}");
                    Console.WriteLine($"Retrying in {retryDelay.TotalSeconds} seconds...");
                    await Task.Delay(retryDelay);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to run migrations after {maxRetries} attempts: {ex.Message}");
                    Console.WriteLine("Application started without migrations. Database may not be in correct state.");
                    break;
                }
            }

        // Fix PostgreSQL identity columns if they don't exist
        if (dbProvider.Equals("pgsql", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Fix auto-increment for all primary key columns AND fix timestamp column types
                var sqlCommands = new[]
                {
                    // Fix identity columns first
                    "ALTER TABLE app_user ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE magic_link ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE grant_code ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE client_profile ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE trainer_profile ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE consent_grant ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE media_asset ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE transcript ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE extraction_result ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE exercise ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE plan_template ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE plan_template_item ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE plan_proposal ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE plan_instance ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE exercise_instance ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE plan_item_acceptance ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE progress_event ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE adherence_week ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE gamification ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",
                    "ALTER TABLE xp_award ALTER COLUMN id ADD GENERATED BY DEFAULT AS IDENTITY;",

                    // Fix timestamp column types (convert text to timestamptz)
                    "ALTER TABLE magic_link ALTER COLUMN expires_at TYPE timestamptz USING expires_at::timestamptz;",
                    "ALTER TABLE magic_link ALTER COLUMN used_at TYPE timestamptz USING used_at::timestamptz;",
                    "ALTER TABLE magic_link ALTER COLUMN created_at TYPE timestamptz USING created_at::timestamptz;",
                    "ALTER TABLE grant_code ALTER COLUMN expires_at TYPE timestamptz USING expires_at::timestamptz;",
                    "ALTER TABLE grant_code ALTER COLUMN used_at TYPE timestamptz USING used_at::timestamptz;",
                    "ALTER TABLE grant_code ALTER COLUMN created_at TYPE timestamptz USING created_at::timestamptz;",
                    "ALTER TABLE app_user ALTER COLUMN created_at TYPE timestamptz USING created_at::timestamptz;",
                    "ALTER TABLE app_user ALTER COLUMN updated_at TYPE timestamptz USING updated_at::timestamptz;",
                    "ALTER TABLE client_profile ALTER COLUMN created_at TYPE timestamptz USING created_at::timestamptz;",
                    "ALTER TABLE client_profile ALTER COLUMN updated_at TYPE timestamptz USING updated_at::timestamptz;",
                    "ALTER TABLE trainer_profile ALTER COLUMN created_at TYPE timestamptz USING created_at::timestamptz;",
                    "ALTER TABLE trainer_profile ALTER COLUMN updated_at TYPE timestamptz USING updated_at::timestamptz;",
                    "ALTER TABLE consent_grant ALTER COLUMN created_at TYPE timestamptz USING created_at::timestamptz;",
                    "ALTER TABLE consent_grant ALTER COLUMN revoked_at TYPE timestamptz USING revoked_at::timestamptz;",

                    // Additional timestamp columns that might be stored as text
                    "ALTER TABLE consent_grant ALTER COLUMN granted_at TYPE timestamptz USING granted_at::timestamptz;",
                    "ALTER TABLE transcript ALTER COLUMN created_at TYPE timestamptz USING created_at::timestamptz;",
                    "ALTER TABLE plan_item_acceptance ALTER COLUMN accepted_at TYPE timestamptz USING accepted_at::timestamptz;",
                    "ALTER TABLE exercise ALTER COLUMN created_at TYPE timestamptz USING created_at::timestamptz;",
                    "ALTER TABLE exercise ALTER COLUMN updated_at TYPE timestamptz USING updated_at::timestamptz;",
                    "ALTER TABLE plan_template ALTER COLUMN created_at TYPE timestamptz USING created_at::timestamptz;",
                    "ALTER TABLE plan_template ALTER COLUMN updated_at TYPE timestamptz USING updated_at::timestamptz;",
                    "ALTER TABLE plan_template_item ALTER COLUMN created_at TYPE timestamptz USING created_at::timestamptz;",
                    "ALTER TABLE plan_proposal ALTER COLUMN proposed_at TYPE timestamptz USING proposed_at::timestamptz;",
                    "ALTER TABLE plan_proposal ALTER COLUMN expires_at TYPE timestamptz USING expires_at::timestamptz;",
                    "ALTER TABLE plan_proposal ALTER COLUMN responded_at TYPE timestamptz USING responded_at::timestamptz;"
                };

                foreach (var sql in sqlCommands)
                {
                    try
                    {
                        context.Database.ExecuteSqlRaw(sql);
                        Console.WriteLine($"Successfully executed: {sql}");
                    }
                    catch (Exception ex)
                    {
                        // Column might already be set up correctly, or table might not exist yet
                        Console.WriteLine($"Skipped (already configured or table doesn't exist): {sql} - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up PostgreSQL identity columns: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Critical error during database initialization: {ex}");
    }
});

Console.WriteLine("Application startup complete - migrations running in background");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add rate limiting
app.UseIpRateLimiting();

// Add security middleware stack
app.UseMiddleware<SecurityRateLimitingMiddleware>();
app.UseMiddleware<SecurityAuditMiddleware>();

// Add profile-specific rate limiting (for backward compatibility)
app.UseMiddleware<ProfileRateLimitingMiddleware>();

// Add CORS
app.UseCors("AllowFrontend");

// Add authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

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

// Map invite endpoints
app.MapInviteEndpoints();

// Map onboarding endpoints
app.MapOnboardingEndpoints();

// Map progress endpoints
app.MapProgressEndpoints();

// Map plan endpoints
app.MapPlanEndpoints();

// Map gamification endpoints
app.MapGamificationEndpoints();

// Map analytics endpoints
app.MapAnalyticsEndpoints();

// Map profile endpoints
app.MapProfileEndpoints();

// Map controller routes
app.MapControllers();

app.MapMethods("/health", new[] { "GET", "HEAD", "OPTIONS" }, () => Results.Ok(new { ok = true }));

app.MapGet("/health/db", async () =>
{
    try
    {
        await using var con = new Npgsql.NpgsqlConnection(
            Environment.GetEnvironmentVariable("DB_CONNECTION")
        );
        await con.OpenAsync();
        await con.CloseAsync();
        return Results.Ok(new { db = "ok" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"db error: {ex.Message}");
    }
});

app.MapGet("/", () => "Adaplio API is running!");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

// Make Program class accessible for testing
public partial class Program { }
