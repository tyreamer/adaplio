using Adaplio.Api.Models;

namespace Adaplio.Api.Services;

public interface IInviteService
{
    Task<InviteResponse> CreateInviteAsync(int trainerId, InviteCreateRequest request);
    Task<InviteValidationResponse> ValidateInviteAsync(string token);
    Task<bool> AcceptInviteAsync(int userId, InviteAcceptRequest request);
    Task<bool> RevokeInviteAsync(int trainerId, string token);
    Task<List<Invite>> GetTrainerInvitesAsync(int trainerId);
}