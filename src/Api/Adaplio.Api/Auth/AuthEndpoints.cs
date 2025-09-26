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
        HttpContext httpContext)
    {
        try
        {
            // Generate 6-digit code
            var code = GenerateSecureCode();
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

            // Save magic link to database
            var magicLink = new MagicLink
            {
                Email = request.Email.ToLowerInvariant(),
                Code = code,
                ExpiresAt = expiresAt,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString()
            };

            // Clean up old expired links for this email
            var expiredLinks = await context.MagicLinks
                .Where(ml => ml.Email == request.Email.ToLowerInvariant() && ml.ExpiresAt < DateTimeOffset.UtcNow)
                .ToListAsync();

            context.MagicLinks.RemoveRange(expiredLinks);
            context.MagicLinks.Add(magicLink);
            await context.SaveChangesAsync();

            // Send email
            await emailService.SendMagicLinkAsync(request.Email, code);

            return Results.Ok(new ClientMagicLinkResponse(
                "Magic link sent successfully. Please check your email.",
                expiresAt));
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to send magic link. Please try again later.");
        }
    }

    private static async Task<IResult> VerifyMagicLink(
        ClientVerifyRequest request,
        AppDbContext context,
        IJwtService jwtService,
        HttpContext httpContext)
    {
        try
        {
            // Find valid magic link
            var magicLink = await context.MagicLinks
                .FirstOrDefaultAsync(ml =>
                    ml.Code == request.Code &&
                    ml.ExpiresAt > DateTimeOffset.UtcNow &&
                    ml.UsedAt == null);

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
                // Create new user without role (for first-time role selection)
                user = new AppUser
                {
                    Email = magicLink.Email,
                    UserType = "", // Empty string to trigger role selection
                    IsVerified = true
                };

                context.AppUsers.Add(user);
                await context.SaveChangesAsync();
            }
            else
            {
                // Update existing user
                user.IsVerified = true;
                user.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await context.SaveChangesAsync();

            // Generate JWT
            var claims = new JwtClaims(
                UserId: user.Id.ToString(),
                Email: user.Email,
                UserType: user.UserType,
                Alias: user.ClientProfile?.Alias
            );

            var token = jwtService.GenerateToken(claims);

            // Set HttpOnly cookie
            httpContext.Response.Cookies.Append("auth_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(24)
            });

            return Results.Ok(new AuthResponse(
                "Login successful",
                UserType: user.UserType,
                UserId: user.Id.ToString(),
                Alias: user.ClientProfile?.Alias,
                Token: token
            ));
        }
        catch (Exception ex)
        {
            return Results.Problem("Verification failed. Please try again.");
        }
    }

    private static async Task<IResult> RegisterTrainer(
        TrainerRegisterRequest request,
        AppDbContext context)
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
                return Results.BadRequest(new AuthResponse("Email already registered."));
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

            return Results.Ok(new AuthResponse("Trainer registered successfully."));
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

            // Set HttpOnly cookie
            httpContext.Response.Cookies.Append("auth_token", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(24)
            });

            return Results.Ok(new AuthResponse(
                "Login successful",
                UserType: user.UserType,
                UserId: user.Id.ToString(),
                Token: token
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
            var userIdClaim = httpContext.User.FindFirst("UserId")?.Value;
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
            var userIdClaim = httpContext.User.FindFirst("UserId")?.Value;
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
            var userIdClaim = httpContext.User.FindFirst("UserId")?.Value;
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

    private static IResult Logout(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete("auth_token");
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