using System.Security.Cryptography;
using System.Text;
using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Adaplio.Api.Services;

public interface IRefreshTokenService
{
    /// <summary>
    /// Generates a new refresh token for a user and stores its hash in the database
    /// </summary>
    Task<string> GenerateRefreshTokenAsync(int userId, string? ipAddress, string? userAgent);

    /// <summary>
    /// Validates a refresh token and returns the associated user ID if valid
    /// </summary>
    Task<int?> ValidateRefreshTokenAsync(string token);

    /// <summary>
    /// Rotates a refresh token: invalidates the old one and generates a new one
    /// </summary>
    Task<string?> RotateRefreshTokenAsync(string oldToken, string? ipAddress, string? userAgent);

    /// <summary>
    /// Revokes all refresh tokens for a user (e.g., on logout or password change)
    /// </summary>
    Task RevokeAllUserTokensAsync(int userId);

    /// <summary>
    /// Clean up expired refresh tokens from the database
    /// </summary>
    Task CleanupExpiredTokensAsync();
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RefreshTokenService> _logger;
    private const int TokenExpiryDays = 30; // Long-lived refresh tokens

    public RefreshTokenService(AppDbContext context, ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateRefreshTokenAsync(int userId, string? ipAddress, string? userAgent)
    {
        // Generate a cryptographically secure random token
        var tokenBytes = new byte[32]; // 256 bits
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes);

        // Hash the token before storing in DB (never store raw tokens)
        var tokenHash = ComputeTokenHash(token);

        // Create refresh token entity
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(TokenExpiryDays),
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated refresh token for user {UserId}", userId);

        return token; // Return raw token to send to client
    }

    public async Task<int?> ValidateRefreshTokenAsync(string token)
    {
        var tokenHash = ComputeTokenHash(token);

        // Find the refresh token
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (refreshToken == null)
        {
            _logger.LogWarning("Refresh token not found in database");
            return null;
        }

        // Check if token is valid (not expired and not revoked)
        if (!refreshToken.IsValid)
        {
            _logger.LogWarning("Refresh token is expired or revoked for user {UserId}", refreshToken.UserId);
            return null;
        }

        return refreshToken.UserId;
    }

    public async Task<string?> RotateRefreshTokenAsync(string oldToken, string? ipAddress, string? userAgent)
    {
        var tokenHash = ComputeTokenHash(oldToken);

        // Find the old refresh token
        var oldRefreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (oldRefreshToken == null || !oldRefreshToken.IsValid)
        {
            _logger.LogWarning("Cannot rotate invalid or missing refresh token");
            return null;
        }

        // Revoke the old token
        oldRefreshToken.RevokedAt = DateTimeOffset.UtcNow;

        // Generate new token
        var newToken = await GenerateRefreshTokenAsync(oldRefreshToken.UserId, ipAddress, userAgent);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Rotated refresh token for user {UserId}", oldRefreshToken.UserId);

        return newToken;
    }

    public async Task RevokeAllUserTokensAsync(int userId)
    {
        var userTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in userTokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked all refresh tokens for user {UserId}", userId);
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-60); // Keep tokens for 60 days after expiry for audit purposes

        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < cutoffDate)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cleaned up {Count} expired refresh tokens", expiredTokens.Count);
        }
    }

    /// <summary>
    /// Computes SHA256 hash of a token for secure storage
    /// </summary>
    private static string ComputeTokenHash(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
