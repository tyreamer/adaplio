using System.ComponentModel.DataAnnotations;

namespace Adaplio.Api.Auth;

// Client magic link DTOs
public record ClientMagicLinkRequest(
    [Required, EmailAddress, MaxLength(255)] string Email
);

public record ClientMagicLinkResponse(
    string Message,
    DateTimeOffset ExpiresAt
);

public record ClientVerifyRequest(
    [Required, MaxLength(20)] string Code
);

// Trainer auth DTOs
public record TrainerRegisterRequest(
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MinLength(8), MaxLength(100)] string Password,
    [MaxLength(200)] string? FullName = null,
    [MaxLength(200)] string? PracticeName = null
);

public record TrainerLoginRequest(
    [Required, EmailAddress, MaxLength(255)] string Email,
    [Required, MaxLength(100)] string Password
);

public record AuthResponse(
    string Message,
    string? UserType = null,
    string? UserId = null,
    string? Alias = null
);

// JWT Claims
public record JwtClaims(
    string UserId,
    string Email,
    string UserType,
    string? Alias = null
);