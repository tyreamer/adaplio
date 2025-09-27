using System.ComponentModel.DataAnnotations;

namespace Adaplio.Api.Models;

public class Invite
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Token { get; set; } = string.Empty;

    [Required]
    public int TrainerId { get; set; }

    [Required]
    [MaxLength(200)]
    public string TrainerName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ClinicName { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? RedeemedAt { get; set; }

    public int? RedeemedByUserId { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    public bool IsRedeemed => RedeemedAt.HasValue;

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsValid => !IsExpired && !IsRedeemed && !IsRevoked;

    // Navigation properties
    public User? Trainer { get; set; }
    public User? RedeemedByUser { get; set; }
}

public class InviteCreateRequest
{
    public string? ClinicName { get; set; }
}

public class InviteResponse
{
    public string Token { get; set; } = string.Empty;
    public string DeepLink { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string QrPngUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class InviteValidationResponse
{
    public bool IsValid { get; set; }
    public string? TrainerName { get; set; }
    public string? ClinicName { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class InviteAcceptRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    public bool ShareSummary { get; set; } = true;

    public bool RemindersEnabled { get; set; } = true;

    public TimeSpan? ReminderTime { get; set; }

    public string? InjuryType { get; set; }

    public string? AffectedSide { get; set; }
}