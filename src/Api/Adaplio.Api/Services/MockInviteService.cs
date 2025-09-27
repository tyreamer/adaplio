using Adaplio.Api.Models;

namespace Adaplio.Api.Services;

public class MockInviteService : IInviteService
{
    private readonly ILogger<MockInviteService> _logger;

    public MockInviteService(ILogger<MockInviteService> logger)
    {
        _logger = logger;
    }

    public async Task<InviteResponse> CreateInviteAsync(int trainerId, InviteCreateRequest request)
    {
        // Mock implementation - in a real system this would create database records
        _logger.LogInformation("Mock: Creating invite for trainer {TrainerId}", trainerId);

        var token = "MOCK-TOKEN-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
        return new InviteResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            QrPngUrl = $"/api/qr/{token}",
            DeepLink = $"/join?invite={token}",
            ShortCode = token[..6]
        };
    }

    public async Task<InviteValidationResponse> ValidateInviteAsync(string token)
    {
        // Mock implementation
        _logger.LogInformation("Mock: Validating invite token {Token}", token);

        if (token.StartsWith("MOCK-TOKEN") || token == "valid-test-token")
        {
            return new InviteValidationResponse
            {
                IsValid = true,
                TrainerName = "Dr. Smith",
                ClinicName = "HealthCare Clinic",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
        }

        return new InviteValidationResponse
        {
            IsValid = false,
            ErrorMessage = "Invalid or expired invitation token"
        };
    }

    public async Task<bool> AcceptInviteAsync(int userId, InviteAcceptRequest request)
    {
        // Mock implementation
        _logger.LogInformation("Mock: User {UserId} accepting invite {Token}", userId, request.Token);
        return true;
    }

    public async Task<bool> RevokeInviteAsync(int trainerId, string token)
    {
        // Mock implementation
        _logger.LogInformation("Mock: Trainer {TrainerId} revoking invite {Token}", trainerId, token);
        return true;
    }

    public async Task<List<Invite>> GetTrainerInvitesAsync(int trainerId)
    {
        // Mock implementation
        _logger.LogInformation("Mock: Getting invites for trainer {TrainerId}", trainerId);
        return new List<Invite>();
    }
}