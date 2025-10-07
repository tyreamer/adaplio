using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace Adaplio.Api.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var authGroup = app.MapGroup("/auth").WithTags("Authentication");

        // Client magic link endpoints
        authGroup.MapPost("/client/magic-link", SendMagicLink);
        authGroup.MapPost("/client/verify", VerifyMagicLink);

        // Trainer auth endpoints
        authGroup.MapPost("/trainer/register", RegisterTrainer);
        authGroup.MapPost("/trainer/login", LoginTrainer);

        // Token refresh endpoint
        authGroup.MapPost("/refresh", RefreshAccessToken);

        // Get current user info endpoint
        authGroup.MapGet("/me", GetCurrentUser).RequireAuthorization();

        // Update profile endpoint
        authGroup.MapPut("/profile", UpdateProfile).RequireAuthorization();

        // Set user role endpoint (for first-time users)
        authGroup.MapPost("/role", SetUserRole).RequireAuthorization();

        // Logout endpoint
        authGroup.MapPost("/logout", Logout);
    }

    private static async Task<IResult> SendMagicLink(
        ClientMagicLinkRequest request,
        AppDbContext context,
        IEmailService emailService,
        HttpContext httpContext,
        ILogger<Program> logger)
    {
        try
        {
            // Clean up old expired links for this email (client-side evaluation)
            var now = DateTimeOffset.UtcNow;
            var allLinksForEmail = await context.MagicLinks
                .Where(ml => ml.Email == request.Email.ToLowerInvariant())
                .ToListAsync();

            var expiredLinks = allLinksForEmail.Where(ml => ml.ExpiresAt < now).ToList();
            context.MagicLinks.RemoveRange(expiredLinks);
            await context.SaveChangesAsync();

            // Generate unique 6-digit code with retry logic
            string code;
            int maxRetries = 10;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                code = GenerateSecureCode();

                // Check if code already exists in database
                var existingCode = await context.MagicLinks
                    .AnyAsync(ml => ml.Code == code && ml.UsedAt == null);

                if (!existingCode)
                {
                    // Code is unique, proceed
                    var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

                    var magicLink = new MagicLink
                    {
                        Email = request.Email.ToLowerInvariant(),
                        Code = code,
                        ExpiresAt = expiresAt,
                        IpAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                    };

                    context.MagicLinks.Add(magicLink);
                    await context.SaveChangesAsync();

                    // Send email (will log code to console if email service not configured)
                    await emailService.SendMagicLinkAsync(request.Email, code);

                    return Results.Ok(new ClientMagicLinkResponse(
                        "Magic link sent successfully. Please check your email.",
                        expiresAt));
                }

                retryCount++;
                logger.LogWarning("Duplicate magic link code generated, retrying... (attempt {Retry}/{Max})", retryCount, maxRetries);
            }

            // If we couldn't generate a unique code after max retries
            logger.LogError("Failed to generate unique magic link code after {MaxRetries} attempts", maxRetries);
            return Results.Problem("Failed to send magic link. Please try again later.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send magic link for email: {Email}", request.Email);
            return Results.Problem("Failed to send magic link. Please try again later.");
        }
    }

    private static async Task<IResult> VerifyMagicLink(
        ClientVerifyRequest request,
        AppDbContext context,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        HttpContext httpContext)
    {
        try
        {
            // Find valid magic link (client-side date evaluation)
            var now = DateTimeOffset.UtcNow;
            var magicLinks = await context.MagicLinks
                .Where(ml => ml.Code == request.Code && ml.UsedAt == null)
                .ToListAsync();

            var magicLink = magicLinks.FirstOrDefault(ml => ml.ExpiresAt > now);

            if (magicLink == null)
            {
                return Results.BadRequest(new AuthResponse("Invalid or expired code."));
            }

            // Mark as used
            magicLink.UsedAt = DateTimeOffset.UtcNow;

            // Create or get user
            var user = await context.AppUsers
                .Include(u => u.ClientProfile)
                .FirstOrDefaultAsync(u => u.Email == magicLink.Email);

            if (user == null)
            {
                // Create new client user (magic link is client-specific)
                user = new AppUser
                {
                    Email = magicLink.Email,
                    UserType = "client", // Automatically set as client
                    IsVerified = true
                };

                context.AppUsers.Add(user);
                await context.SaveChangesAsync();

                // Create client profile with alias
                var clientProfile = new ClientProfile
                {
                    UserId = user.Id,
                    Alias = GenerateClientAlias()
                };

                context.ClientProfiles.Add(clientProfile);
                await context.SaveChangesAsync();

                // Reload user with profile for JWT generation
                user = await context.AppUsers
                    .Include(u => u.ClientProfile)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);
            }
            else
            {
                // Update existing user
                user.IsVerified = true;
                user.UpdatedAt = DateTimeOffset.UtcNow;
                await context.SaveChangesAsync();
            }

            // Generate JWT
            var claims = new JwtClaims(
                UserId: user.Id.ToString(),
                Email: user.Email,
                UserType: user.UserType,
                Alias: user.ClientProfile?.Alias
            );

            var token = jwtService.GenerateToken(claims);

            // Generate refresh token
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var refreshToken = await refreshTokenService.GenerateRefreshTokenAsync(user.Id, ipAddress, userAgent);

            // Set access token cookie (short-lived)
            httpContext.Response.Cookies.Append("auth_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            // Set refresh token cookie (long-lived)
            httpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return Results.Ok(new AuthResponse(
                "Login successful",
                UserType: user.UserType,
                UserId: user.Id.ToString(),
                Alias: user.ClientProfile?.Alias,
                Token: token,
                RefreshToken: refreshToken
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Verification failed. Please try again.");
        }
    }

    private static async Task<IResult> RegisterTrainer(
        TrainerRegisterRequest request,
        AppDbContext context,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        HttpContext httpContext)
    {
        try
        {
            Console.WriteLine($"Trainer registration attempt for email: {request.Email}");

            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                Console.WriteLine("Registration failed: Empty email");
                return Results.BadRequest(new AuthResponse("Email is required."));
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                Console.WriteLine("Registration failed: Empty password");
                return Results.BadRequest(new AuthResponse("Password is required."));
            }

            // Check if email already exists
            var existingUser = await context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant());

            if (existingUser != null)
            {
                Console.WriteLine($"Registration failed: Email already exists - {request.Email}");
                return Results.Conflict(new AuthResponse("Email already registered."));
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create trainer user
            var user = new AppUser
            {
                Email = request.Email.ToLowerInvariant(),
                UserType = "trainer",
                PasswordHash = passwordHash,
                IsVerified = true
            };

            context.AppUsers.Add(user);
            await context.SaveChangesAsync();

            Console.WriteLine($"Created trainer user with ID: {user.Id}");

            // Create trainer profile
            var trainerProfile = new TrainerProfile
            {
                UserId = user.Id,
                FullName = request.FullName,
                PracticeName = request.PracticeName
            };

            context.TrainerProfiles.Add(trainerProfile);
            await context.SaveChangesAsync();

            Console.WriteLine($"Created trainer profile for user: {user.Id}");

            // Generate JWT
            var claims = new JwtClaims(
                UserId: user.Id.ToString(),
                Email: user.Email,
                UserType: user.UserType
            );

            var token = jwtService.GenerateToken(claims);

            // Generate refresh token
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var refreshToken = await refreshTokenService.GenerateRefreshTokenAsync(user.Id, ipAddress, userAgent);

            // Set access token cookie (short-lived)
            httpContext.Response.Cookies.Append("auth_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            // Set refresh token cookie (long-lived)
            httpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return Results.Ok(new AuthResponse(
                "Trainer registered successfully.",
                UserType: user.UserType,
                UserId: user.Id.ToString(),
                Token: token,
                RefreshToken: refreshToken
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration failed for email: {request.Email}. Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return Results.Problem($"Registration failed: {ex.Message}");
        }
    }

    private static async Task<IResult> LoginTrainer(
        TrainerLoginRequest request,
        AppDbContext context,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        HttpContext httpContext)
    {
        try
        {
            // Find trainer user
            var user = await context.AppUsers
                .Include(u => u.TrainerProfile)
                .FirstOrDefaultAsync(u =>
                    u.Email == request.Email.ToLowerInvariant() &&
                    u.UserType == "trainer");

            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                return Results.BadRequest(new AuthResponse("Invalid email or password."));
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Results.BadRequest(new AuthResponse("Invalid email or password."));
            }

            // Generate JWT
            var claims = new JwtClaims(
                UserId: user.Id.ToString(),
                Email: user.Email,
                UserType: user.UserType
            );

            var token = jwtService.GenerateToken(claims);

            // Generate refresh token
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var refreshToken = await refreshTokenService.GenerateRefreshTokenAsync(user.Id, ipAddress, userAgent);

            // Set access token cookie (short-lived)
            httpContext.Response.Cookies.Append("auth_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            // Set refresh token cookie (long-lived)
            httpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return Results.Ok(new AuthResponse(
                "Login successful",
                UserType: user.UserType,
                UserId: user.Id.ToString(),
                Token: token,
                RefreshToken: refreshToken
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Login failed. Please try again.");
        }
    }

    private static async Task<IResult> GetCurrentUser(
        HttpContext httpContext,
        AppDbContext context)
    {
        try
        {
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await context.AppUsers
                .Include(u => u.ClientProfile)
                .Include(u => u.TrainerProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Results.Unauthorized();
            }

            var response = new
            {
                userId = user.Id.ToString(),
                email = user.Email,
                userType = user.UserType,
                alias = user.ClientProfile?.Alias,
                displayName = user.ClientProfile?.DisplayName,
                fullName = user.TrainerProfile?.FullName,
                practiceName = user.TrainerProfile?.PracticeName,
                isVerified = user.IsVerified
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to get user information.");
        }
    }

    private static async Task<IResult> UpdateProfile(
        UpdateProfileRequest request,
        HttpContext httpContext,
        AppDbContext context)
    {
        try
        {
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await context.AppUsers
                .Include(u => u.ClientProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Results.Unauthorized();
            }

            // Only clients can update their profile
            if (user.UserType != "client" || user.ClientProfile == null)
            {
                return Results.BadRequest("Only clients can update their profile.");
            }

            // Update display name
            if (request.DisplayName != null)
            {
                user.ClientProfile.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                    ? null
                    : request.DisplayName.Trim();
                user.ClientProfile.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync();

            return Results.Ok(new AuthResponse("Profile updated successfully."));
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to update profile. Please try again.");
        }
    }

    private static async Task<IResult> SetUserRole(
        SetUserRoleRequest request,
        HttpContext httpContext,
        AppDbContext context)
    {
        try
        {
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var user = await context.AppUsers
                .Include(u => u.ClientProfile)
                .Include(u => u.TrainerProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return Results.Unauthorized();
            }

            // Validate role
            if (request.Role != "client" && request.Role != "trainer")
            {
                return Results.BadRequest(new AuthResponse("Invalid role. Must be 'client' or 'trainer'."));
            }

            // Only allow setting role if user doesn't have one yet
            if (!string.IsNullOrEmpty(user.UserType))
            {
                return Results.BadRequest(new AuthResponse("User role has already been set."));
            }

            // Set the user role
            user.UserType = request.Role;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            // Create appropriate profile if needed
            if (request.Role == "client" && user.ClientProfile == null)
            {
                var clientProfile = new ClientProfile
                {
                    UserId = user.Id,
                    Alias = GenerateClientAlias()
                };
                context.ClientProfiles.Add(clientProfile);
            }
            else if (request.Role == "trainer" && user.TrainerProfile == null)
            {
                var trainerProfile = new TrainerProfile
                {
                    UserId = user.Id,
                    FullName = user.Email, // Default to email, user can update later
                    PracticeName = "Practice" // Default, user can update later
                };
                context.TrainerProfiles.Add(trainerProfile);
            }

            await context.SaveChangesAsync();

            return Results.Ok(new AuthResponse("Role set successfully."));
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to set user role. Please try again.");
        }
    }

    private static async Task<IResult> RefreshAccessToken(
        HttpContext httpContext,
        IRefreshTokenService refreshTokenService,
        IJwtService jwtService,
        AppDbContext context)
    {
        try
        {
            // Get refresh token from cookie
            if (!httpContext.Request.Cookies.TryGetValue("refresh_token", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            {
                return Results.Unauthorized();
            }

            // Get IP address and user agent
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();

            // Rotate the refresh token (invalidate old, generate new)
            var newRefreshToken = await refreshTokenService.RotateRefreshTokenAsync(refreshToken, ipAddress, userAgent);

            if (newRefreshToken == null)
            {
                // Invalid or expired refresh token
                httpContext.Response.Cookies.Delete("auth_token");
                httpContext.Response.Cookies.Delete("refresh_token");
                return Results.Unauthorized();
            }

            // Get user ID from old token to generate new access token
            var userId = await refreshTokenService.ValidateRefreshTokenAsync(refreshToken);
            if (userId == null)
            {
                // This shouldn't happen as RotateRefreshTokenAsync already validated it
                return Results.Unauthorized();
            }

            // Get user info to generate JWT
            var user = await context.AppUsers
                .Include(u => u.ClientProfile)
                .Include(u => u.TrainerProfile)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
            {
                return Results.Unauthorized();
            }

            // Generate new access token
            var claims = new JwtClaims(
                UserId: user.Id.ToString(),
                Email: user.Email,
                UserType: user.UserType,
                Alias: user.ClientProfile?.Alias
            );

            var newAccessToken = jwtService.GenerateToken(claims);

            // Set new access token cookie (short-lived)
            httpContext.Response.Cookies.Append("auth_token", newAccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            // Set new refresh token cookie (long-lived)
            httpContext.Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });

            return Results.Ok(new AuthResponse(
                "Token refreshed successfully",
                UserType: user.UserType,
                UserId: user.Id.ToString(),
                Alias: user.ClientProfile?.Alias,
                Token: newAccessToken,
                RefreshToken: newRefreshToken
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to refresh token. Please login again.");
        }
    }

    private static IResult Logout(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete("auth_token");
        httpContext.Response.Cookies.Delete("refresh_token");
        return Results.Ok(new AuthResponse("Logged out successfully."));
    }

    private static string GenerateSecureCode()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var code = (BitConverter.ToUInt32(bytes, 0) % 900000) + 100000;
        return code.ToString();
    }

    private static string GenerateClientAlias()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);

        var result = new StringBuilder(6);
        result.Append('C');
        result.Append('-');

        for (int i = 0; i < 4; i++)
        {
            result.Append(chars[bytes[i] % chars.Length]);
        }

        return result.ToString();
    }
}