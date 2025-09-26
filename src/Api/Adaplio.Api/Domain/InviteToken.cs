using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("invite_token")]
public class InviteToken
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("token")]
    [MaxLength(20)]
    public string Token { get; set; } = string.Empty;

    [Column("grant_code_id")]
    public int? GrantCodeId { get; set; }

    [Column("phone_number")]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }

    [Column("used_at")]
    public DateTimeOffset? UsedAt { get; set; }

    [Column("used_by_client_profile_id")]
    public int? UsedByClientProfileId { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    // Navigation properties
    [ForeignKey(nameof(GrantCodeId))]
    public GrantCode? GrantCode { get; set; }

    [ForeignKey(nameof(UsedByClientProfileId))]
    public ClientProfile? UsedByClientProfile { get; set; }
}