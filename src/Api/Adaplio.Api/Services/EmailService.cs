using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Adaplio.Api.Services;

public interface IEmailService
{
    Task SendMagicLinkAsync(string email, string code);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendMagicLinkAsync(string email, string code)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Adaplio",
                _configuration["Email:FromEmail"] ?? "noreply@adaplio.local"));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Your Adaplio Login Code";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $"""
                    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                        <h2 style="color: #2563eb;">Welcome to Adaplio</h2>
                        <p>Your login code is:</p>
                        <div style="background: #f3f4f6; padding: 20px; text-align: center; border-radius: 8px; margin: 20px 0;">
                            <h1 style="font-size: 32px; letter-spacing: 4px; color: #1f2937; margin: 0;">{code}</h1>
                        </div>
                        <p>This code will expire in 15 minutes.</p>
                        <p>If you didn't request this login, please ignore this email.</p>
                        <hr style="margin: 30px 0; border: none; border-top: 1px solid #e5e7eb;">
                        <p style="color: #6b7280; font-size: 12px;">Adaplio - Your Physical Therapy Companion</p>
                    </div>
                    """,
                TextBody = $"""
                    Welcome to Adaplio

                    Your login code is: {code}

                    This code will expire in 15 minutes.

                    If you didn't request this login, please ignore this email.

                    Adaplio - Your Physical Therapy Companion
                    """
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "1025");
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];

            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.None);

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                await client.AuthenticateAsync(username, password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Magic link sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send magic link to {Email}", email);
            throw;
        }
    }
}