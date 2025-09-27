using System.Text;
using System.Text.Json;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

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
            var smsProvider = _configuration["SMS:Provider"] ?? _configuration["SMS_PROVIDER"];

            // Default to Twilio if environment variables are present
            if (string.IsNullOrEmpty(smsProvider))
            {
                var twilioSid = _configuration["TWILIO_ACCOUNT_SID"];
                if (!string.IsNullOrEmpty(twilioSid))
                {
                    smsProvider = "twilio";
                }
            }

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
        try
        {
            var accountSid = _configuration["Twilio:AccountSid"] ?? _configuration["TWILIO_ACCOUNT_SID"];
            var authToken = _configuration["Twilio:AuthToken"] ?? _configuration["TWILIO_AUTH_TOKEN"];
            var fromNumber = _configuration["Twilio:PhoneNumber"] ?? _configuration["TWILIO_PHONE_NUMBER"];

            if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
            {
                _logger.LogError("Twilio configuration missing. Check TWILIO_ACCOUNT_SID, TWILIO_AUTH_TOKEN, and TWILIO_PHONE_NUMBER");
                return false;
            }

            TwilioClient.Init(accountSid, authToken);

            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(fromNumber),
                to: new PhoneNumber(phoneNumber)
            );

            _logger.LogInformation("SMS sent successfully via Twilio. SID: {MessageSid}, Status: {Status}",
                messageResource.Sid, messageResource.Status);

            return messageResource.Status != MessageResource.StatusEnum.Failed
                && messageResource.Status != MessageResource.StatusEnum.Undelivered;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS via Twilio to {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    private async Task<bool> SendViaAWS(string phoneNumber, string message)
    {
        // Placeholder for AWS SNS integration
        _logger.LogInformation("Would send via AWS SNS to {PhoneNumber}: {Message}", phoneNumber, message);
        return true;
    }
}