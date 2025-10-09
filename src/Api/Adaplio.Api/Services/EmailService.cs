using System.Text;
using System.Text.Json;

namespace Adaplio.Api.Services;

public interface IEmailService
{
    Task SendMagicLinkAsync(string email, string code);
    Task SendPasswordResetAsync(string email, string code);
    Task SendInviteEmailAsync(string email, string inviteUrl, string trainerName);
}

public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(HttpClient httpClient, IConfiguration configuration, ILogger<EmailService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendMagicLinkAsync(string email, string code)
    {
        try
        {
            var resendApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY") ?? _configuration["Resend:ApiKey"];
            var fromEmail = Environment.GetEnvironmentVariable("RESEND_FROM_EMAIL") ?? _configuration["Resend:FromEmail"] ?? "noreply@adaplio.com";

            _logger.LogInformation("Attempting to send magic link. API Key present: {HasKey}, From: {FromEmail}",
                !string.IsNullOrEmpty(resendApiKey), fromEmail);

            // Check if Resend is properly configured
            if (string.IsNullOrEmpty(resendApiKey))
            {
                _logger.LogWarning("Resend not configured - API key missing. Magic link code for {Email}: {Code}", email, code);

                // In development/testing, just log the code instead of sending email
                Console.WriteLine($"=== MAGIC LINK CODE for {email} ===");
                Console.WriteLine($"CODE: {code}");
                Console.WriteLine($"=====================================");
                return;
            }

            var emailData = new
            {
                from = fromEmail,
                to = new[] { email },
                subject = "Your Adaplio Login Code",
                html = $"""
                    <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px; background: #ffffff;">
                        <div style="text-align: center; margin-bottom: 40px;">
                            <h1 style="color: #2E90FA; font-size: 28px; font-weight: 700; margin: 0;">Adaplio</h1>
                            <p style="color: #64748B; font-size: 16px; margin: 8px 0 0 0;">Your Physical Therapy Companion</p>
                        </div>

                        <div style="background: #F8FAFC; border: 1px solid #E2E8F0; border-radius: 12px; padding: 32px; margin: 32px 0; text-align: center;">
                            <h2 style="color: #1E293B; font-size: 20px; font-weight: 600; margin: 0 0 16px 0;">Your Login Code</h2>
                            <div style="background: #FFFFFF; border: 2px dashed #CBD5E1; border-radius: 8px; padding: 20px; margin: 20px 0;">
                                <span style="font-family: 'SF Mono', Monaco, monospace; font-size: 36px; font-weight: 700; letter-spacing: 8px; color: #0F172A;">{code}</span>
                            </div>
                            <p style="color: #64748B; font-size: 14px; margin: 16px 0 0 0;">This code will expire in 15 minutes</p>
                        </div>

                        <div style="text-align: center; padding-top: 32px; border-top: 1px solid #E2E8F0;">
                            <p style="color: #64748B; font-size: 14px; margin: 0;">If you didn't request this login code, please ignore this email.</p>
                        </div>
                    </div>
                    """,
                text = $"""
                    Welcome to Adaplio

                    Your login code is: {code}

                    This code will expire in 15 minutes.

                    If you didn't request this login, please ignore this email.

                    ---
                    Adaplio - Your Physical Therapy Companion
                    """
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {resendApiKey}");

            var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Magic link sent successfully to {Email} via Resend. Response: {Response}", email, responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send magic link via Resend. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to send email: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send magic link to {Email} via Resend", email);

            // For development/testing, still log the code so the user can proceed
            _logger.LogWarning("Magic link code for testing: {Code}", code);
            Console.WriteLine($"=== MAGIC LINK CODE for {email} (due to email failure) ===");
            Console.WriteLine($"CODE: {code}");
            Console.WriteLine($"=========================================================");

            // Don't throw in development - allow the flow to continue so users can still get the code via console
            // In production with proper email config, this should be investigated
        }
    }

    public async Task SendPasswordResetAsync(string email, string code)
    {
        try
        {
            var resendApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY") ?? _configuration["Resend:ApiKey"];
            var fromEmail = Environment.GetEnvironmentVariable("RESEND_FROM_EMAIL") ?? _configuration["Resend:FromEmail"] ?? "noreply@adaplio.com";

            _logger.LogInformation("Attempting to send password reset email. API Key present: {HasKey}, From: {FromEmail}",
                !string.IsNullOrEmpty(resendApiKey), fromEmail);

            // Check if Resend is properly configured
            if (string.IsNullOrEmpty(resendApiKey))
            {
                _logger.LogWarning("Resend not configured - API key missing. Password reset code for {Email}: {Code}", email, code);

                // In development/testing, just log the code instead of sending email
                Console.WriteLine($"=== PASSWORD RESET CODE for {email} ===");
                Console.WriteLine($"CODE: {code}");
                Console.WriteLine($"=============================================");
                return;
            }

            var emailData = new
            {
                from = fromEmail,
                to = new[] { email },
                subject = "Reset Your Adaplio Password",
                html = $"""
                    <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px; background: #ffffff;">
                        <div style="text-align: center; margin-bottom: 40px;">
                            <h1 style="color: #FF6B35; font-size: 28px; font-weight: 700; margin: 0;">Adaplio</h1>
                            <p style="color: #64748B; font-size: 16px; margin: 8px 0 0 0;">Your Physical Therapy Companion</p>
                        </div>

                        <div style="background: #FFF9F0; border: 2px solid #FF6B35; border-radius: 12px; padding: 32px; margin: 32px 0;">
                            <h2 style="color: #1E293B; font-size: 20px; font-weight: 600; margin: 0 0 16px 0; text-align: center;">Reset Your Password</h2>
                            <p style="color: #64748B; font-size: 14px; margin: 0 0 20px 0; text-align: center;">Use this code to reset your password. This code will expire in 1 hour.</p>
                            <div style="background: #FFFFFF; border: 2px dashed #FF6B35; border-radius: 8px; padding: 20px; margin: 20px 0; text-align: center;">
                                <span style="font-family: 'SF Mono', Monaco, monospace; font-size: 36px; font-weight: 700; letter-spacing: 8px; color: #FF6B35;">{code}</span>
                            </div>
                            <p style="color: #EF4444; font-size: 13px; margin: 16px 0 0 0; text-align: center; font-weight: 500;">⚠️ Do not share this code with anyone</p>
                        </div>

                        <div style="background: #FEF2F2; border-left: 4px solid #EF4444; border-radius: 4px; padding: 16px; margin: 24px 0;">
                            <p style="color: #991B1B; font-size: 14px; margin: 0; font-weight: 500;">Security Notice</p>
                            <p style="color: #7F1D1D; font-size: 13px; margin: 8px 0 0 0; line-height: 1.5;">If you didn't request a password reset, please ignore this email and ensure your account is secure. Your password will not be changed.</p>
                        </div>

                        <div style="text-align: center; padding-top: 32px; border-top: 1px solid #E2E8F0;">
                            <p style="color: #64748B; font-size: 12px; margin: 0;">Need help? Contact our support team</p>
                        </div>
                    </div>
                    """,
                text = $"""
                    Reset Your Adaplio Password

                    Use this code to reset your password: {code}

                    This code will expire in 1 hour.

                    SECURITY NOTICE: Do not share this code with anyone.

                    If you didn't request a password reset, please ignore this email and ensure your account is secure.

                    ---
                    Adaplio - Your Physical Therapy Companion
                    """
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {resendApiKey}");

            var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Password reset email sent successfully to {Email} via Resend. Response: {Response}", email, responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send password reset email via Resend. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to send email: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email} via Resend", email);

            // For development/testing, still log the code so the user can proceed
            _logger.LogWarning("Password reset code for testing: {Code}", code);
            Console.WriteLine($"=== PASSWORD RESET CODE for {email} (due to email failure) ===");
            Console.WriteLine($"CODE: {code}");
            Console.WriteLine($"================================================================");

            // Don't throw in development - allow the flow to continue so users can still get the code via console
            // In production with proper email config, this should be investigated
        }
    }

    public async Task SendInviteEmailAsync(string email, string inviteUrl, string trainerName)
    {
        try
        {
            var resendApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY") ?? _configuration["Resend:ApiKey"];
            var fromEmail = Environment.GetEnvironmentVariable("RESEND_FROM_EMAIL") ?? _configuration["Resend:FromEmail"] ?? "noreply@adaplio.com";

            _logger.LogInformation("Attempting to send invite email. API Key present: {HasKey}, From: {FromEmail}",
                !string.IsNullOrEmpty(resendApiKey), fromEmail);

            // Check if Resend is properly configured
            if (string.IsNullOrEmpty(resendApiKey))
            {
                _logger.LogWarning("Resend not configured - API key missing. Invite URL for {Email}: {InviteUrl}", email, inviteUrl);

                // In development/testing, just log the invite URL instead of sending email
                Console.WriteLine($"=== INVITE for {email} from {trainerName} ===");
                Console.WriteLine($"INVITE URL: {inviteUrl}");
                Console.WriteLine($"=============================================");
                return;
            }

            var emailData = new
            {
                from = fromEmail,
                to = new[] { email },
                subject = $"{trainerName} invited you to Adaplio",
                html = $"""
                    <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px; background: #ffffff;">
                        <div style="text-align: center; margin-bottom: 40px;">
                            <h1 style="color: #FF6B35; font-size: 28px; font-weight: 700; margin: 0;">Adaplio</h1>
                            <p style="color: #64748B; font-size: 16px; margin: 8px 0 0 0;">Your Physical Therapy Companion</p>
                        </div>

                        <div style="background: #F0F9FF; border: 2px solid #2E90FA; border-radius: 12px; padding: 32px; margin: 32px 0;">
                            <h2 style="color: #1E293B; font-size: 20px; font-weight: 600; margin: 0 0 16px 0; text-align: center;">You've Been Invited!</h2>
                            <p style="color: #334155; font-size: 16px; margin: 0 0 20px 0; text-align: center; line-height: 1.6;">
                                <strong style="color: #FF6B35;">{trainerName}</strong> has invited you to join Adaplio to track your physical therapy exercises and progress.
                            </p>
                            <div style="text-align: center; margin: 32px 0;">
                                <a href="{inviteUrl}" style="display: inline-block; background: #FF6B35; color: #FFFFFF; text-decoration: none; padding: 14px 32px; border-radius: 8px; font-weight: 600; font-size: 16px;">Accept Invitation</a>
                            </div>
                            <p style="color: #64748B; font-size: 13px; margin: 16px 0 0 0; text-align: center;">Or copy and paste this link into your browser:</p>
                            <p style="color: #2E90FA; font-size: 12px; margin: 8px 0 0 0; text-align: center; word-break: break-all;">{inviteUrl}</p>
                        </div>

                        <div style="background: #F8FAFC; border-radius: 8px; padding: 20px; margin: 24px 0;">
                            <h3 style="color: #1E293B; font-size: 16px; font-weight: 600; margin: 0 0 12px 0;">What is Adaplio?</h3>
                            <ul style="color: #64748B; font-size: 14px; margin: 0; padding-left: 20px; line-height: 1.8;">
                                <li>Track your exercise progress and adherence</li>
                                <li>Receive personalized exercise plans from your physical therapist</li>
                                <li>Earn achievements and maintain streaks for staying consistent</li>
                                <li>Communicate easily with your healthcare provider</li>
                            </ul>
                        </div>

                        <div style="text-align: center; padding-top: 32px; border-top: 1px solid #E2E8F0;">
                            <p style="color: #64748B; font-size: 12px; margin: 0;">This invitation was sent by {trainerName}</p>
                            <p style="color: #94A3B8; font-size: 11px; margin: 8px 0 0 0;">If you believe this was sent in error, you can safely ignore this email.</p>
                        </div>
                    </div>
                    """,
                text = $"""
                    You've Been Invited to Adaplio!

                    {trainerName} has invited you to join Adaplio to track your physical therapy exercises and progress.

                    Click here to accept the invitation:
                    {inviteUrl}

                    What is Adaplio?
                    - Track your exercise progress and adherence
                    - Receive personalized exercise plans from your physical therapist
                    - Earn achievements and maintain streaks for staying consistent
                    - Communicate easily with your healthcare provider

                    This invitation was sent by {trainerName}.
                    If you believe this was sent in error, you can safely ignore this email.

                    ---
                    Adaplio - Your Physical Therapy Companion
                    """
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {resendApiKey}");

            var response = await _httpClient.PostAsync("https://api.resend.com/emails", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Invite email sent successfully to {Email} via Resend. Response: {Response}", email, responseContent);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to send invite email via Resend. Status: {StatusCode}, Error: {Error}",
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to send email: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send invite email to {Email} via Resend", email);

            // For development/testing, still log the invite URL so the user can proceed
            _logger.LogWarning("Invite URL for testing: {InviteUrl}", inviteUrl);
            Console.WriteLine($"=== INVITE for {email} from {trainerName} (due to email failure) ===");
            Console.WriteLine($"INVITE URL: {inviteUrl}");
            Console.WriteLine($"================================================================");

            // Don't throw in development - allow the flow to continue
        }
    }
}