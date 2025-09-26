using System.Text;
using System.Text.Json;

namespace Adaplio.Api.Services;

public class SMSService : ISMSService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SMSService> _logger;

    public SMSService(HttpClient httpClient, IConfiguration configuration, ILogger<SMSService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> SendInviteLinkAsync(string phoneNumber, string inviteToken, string? trainerName = null)
    {
        var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";
        var inviteLink = $"{baseUrl}/?invite={inviteToken}";

        var trainerText = !string.IsNullOrEmpty(trainerName) ? $" from {trainerName}" : "";
        var message = $"You've been invited{trainerText} to start your PT plan on Adaplio! Click here to get started: {inviteLink}";

        return await SendMessageAsync(phoneNumber, message);
    }

    public async Task<bool> SendMessageAsync(string phoneNumber, string message)
    {
        try
        {
            // Check if we're in development mode
            if (_configuration["Environment"] == "Development")
            {
                _logger.LogInformation("SMS Service (Development Mode): Would send to {PhoneNumber}: {Message}", phoneNumber, message);
                return true;
            }

            // For production, integrate with actual SMS service (Twilio, AWS SNS, etc.)
            var smsProvider = _configuration["SMS:Provider"];

            switch (smsProvider?.ToLower())
            {
                case "twilio":
                    return await SendViaTwilio(phoneNumber, message);
                case "aws":
                    return await SendViaAWS(phoneNumber, message);
                default:
                    _logger.LogWarning("No SMS provider configured. Message not sent to {PhoneNumber}", phoneNumber);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    private async Task<bool> SendViaTwilio(string phoneNumber, string message)
    {
        // Placeholder for Twilio integration
        _logger.LogInformation("Would send via Twilio to {PhoneNumber}: {Message}", phoneNumber, message);
        return true;
    }

    private async Task<bool> SendViaAWS(string phoneNumber, string message)
    {
        // Placeholder for AWS SNS integration
        _logger.LogInformation("Would send via AWS SNS to {PhoneNumber}: {Message}", phoneNumber, message);
        return true;
    }
}