using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

/// <summary>
/// Refresh token entity for maintaining long-lived user sessions.
/// Tokens are rotated on each use for security (old token invalidated, new one issued).
/// </summary>
[Table("refresh_token")]
public class RefreshToken
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    /// <summary>
    /// SHA256 hash of the refresh token (never store raw tokens in DB)
    /// </summary>
    [Required]
    [Column("token_hash")]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("revoked_at")]
    public DateTimeOffset? RevokedAt { get; set; }

    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; } = null!;

    /// <summary>
    /// Check if this refresh token is still valid (not expired and not revoked)
    /// </summary>
    public bool IsValid => ExpiresAt > DateTimeOffset.UtcNow && RevokedAt == null;
}
