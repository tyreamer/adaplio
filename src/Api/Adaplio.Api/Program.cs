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
using Npgsql;

AppContext.SetSwitch("Npgsql.EnableLegacyIPv6Resolver", true);

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

// Add HTTP client for Resend
builder.Services.AddHttpClient<EmailService>();
builder.Services.AddHttpClient<SMSService>();

// Add authentication services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISMSService, SMSService>();
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

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

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
}

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
