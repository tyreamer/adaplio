namespace Adaplio.Api.Services;

public interface ISMSService
{
    Task<bool> SendInviteLinkAsync(string phoneNumber, string inviteToken, string? trainerName = null);
    Task<bool> SendMessageAsync(string phoneNumber, string message);
}