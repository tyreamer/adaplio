using System.ComponentModel.DataAnnotations;

namespace Adaplio.Api.Auth;

// Trainer grant creation
public record CreateGrantRequest();

public record CreateGrantResponse(
    string GrantCode,
    string Url,
    DateTimeOffset ExpiresAt
);

// Grant validation (public)
public record GrantValidationResponse(
    string TrainerName,
    string ClinicName,
    string? LogoUrl,
    DateTimeOffset ExpiresAt
);

// Client grant acceptance
public record AcceptGrantRequest(
    [Required, MaxLength(20)] string GrantCode
);

public record AcceptGrantResponse(
    string Message,
    string TrainerName,
    string[] Scopes
);

// Trainer client list
public record TrainerClientDto(
    string ClientAlias,
    string[] Scopes,
    decimal AdherencePct,
    DateTimeOffset? LastActivity,
    DateTimeOffset GrantedAt
);

public record TrainerClientsResponse(
    TrainerClientDto[] Clients
);

// Development seeding
public record SeedGrantResponse(
    string TrainerEmail,
    string ClientEmail,
    string ClientAlias,
    string GrantCode,
    DateTimeOffset ExpiresAt
);