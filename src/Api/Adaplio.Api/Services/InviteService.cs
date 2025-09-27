using Adaplio.Api.Data;
using Adaplio.Api.Models;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Security.Cryptography;
using System.Text;

namespace Adaplio.Api.Services;

public interface IInviteService
{
    Task<InviteResponse> CreateInviteAsync(int trainerId, InviteCreateRequest request);
    Task<InviteValidationResponse> ValidateInviteAsync(string token);
    Task<bool> AcceptInviteAsync(int userId, InviteAcceptRequest request);
    Task<bool> RevokeInviteAsync(int trainerId, string token);
    Task<List<Invite>> GetTrainerInvitesAsync(int trainerId);
}

public class InviteService : IInviteService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InviteService> _logger;

    public InviteService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<InviteService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<InviteResponse> CreateInviteAsync(int trainerId, InviteCreateRequest request)
    {
        // Get trainer info
        var trainer = await _context.Users.FindAsync(trainerId);
        if (trainer == null)
            throw new ArgumentException("Trainer not found");

        // Generate secure token (Base32, no confusing characters)
        var token = GenerateSecureToken();

        var invite = new Invite
        {
            Token = token,
            TrainerId = trainerId,
            TrainerName = trainer.DisplayName ?? trainer.Email,
            ClinicName = request.ClinicName,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7) // 7-day TTL
        };

        _context.Invites.Add(invite);
        await _context.SaveChangesAsync();

        // Generate URLs and QR code
        var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost:5001";
        var deepLink = $"{baseUrl}/join?invite={token}";
        var qrPngUrl = await GenerateQrCodeAsync(deepLink);

        return new InviteResponse
        {
            Token = token,
            DeepLink = deepLink,
            ShortCode = FormatShortCode(token),
            QrPngUrl = qrPngUrl,
            ExpiresAt = invite.ExpiresAt
        };
    }

    public async Task<InviteValidationResponse> ValidateInviteAsync(string token)
    {
        var invite = await _context.Invites
            .Include(i => i.Trainer)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invite == null)
        {
            return new InviteValidationResponse
            {
                IsValid = false,
                ErrorMessage = "Invalid invitation code"
            };
        }

        if (!invite.IsValid)
        {
            var errorMessage = invite.IsExpired ? "This invitation has expired" :
                             invite.IsRedeemed ? "This invitation has already been used" :
                             invite.IsRevoked ? "This invitation has been revoked" :
                             "This invitation is no longer valid";

            return new InviteValidationResponse
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }

        return new InviteValidationResponse
        {
            IsValid = true,
            TrainerName = invite.TrainerName,
            ClinicName = invite.ClinicName,
            ExpiresAt = invite.ExpiresAt
        };
    }

    public async Task<bool> AcceptInviteAsync(int userId, InviteAcceptRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Validate and redeem invite
            var invite = await _context.Invites
                .FirstOrDefaultAsync(i => i.Token == request.Token);

            if (invite == null || !invite.IsValid)
                return false;

            // Mark invite as redeemed
            invite.RedeemedAt = DateTime.UtcNow;
            invite.RedeemedByUserId = userId;

            // Create client-trainer relationship
            var relationship = new ClientTrainerRelationship
            {
                ClientId = userId,
                TrainerId = invite.TrainerId,
                ShareSummary = request.ShareSummary,
                RemindersEnabled = request.RemindersEnabled,
                ReminderTime = request.ReminderTime,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ClientTrainerRelationships.Add(relationship);

            // Update client profile if injury info provided
            if (!string.IsNullOrEmpty(request.InjuryType))
            {
                var profile = await _context.ClientProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile != null)
                {
                    profile.InjuryType = request.InjuryType;
                    profile.AffectedSide = request.AffectedSide;
                    profile.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Invite {Token} accepted by user {UserId}", request.Token, userId);
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to accept invite {Token} for user {UserId}", request.Token, userId);
            return false;
        }
    }

    public async Task<bool> RevokeInviteAsync(int trainerId, string token)
    {
        var invite = await _context.Invites
            .FirstOrDefaultAsync(i => i.Token == token && i.TrainerId == trainerId);

        if (invite == null || invite.IsRedeemed)
            return false;

        invite.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Invite {Token} revoked by trainer {TrainerId}", token, trainerId);
        return true;
    }

    public async Task<List<Invite>> GetTrainerInvitesAsync(int trainerId)
    {
        return await _context.Invites
            .Where(i => i.TrainerId == trainerId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    private static string GenerateSecureToken()
    {
        // Generate 8-10 character Base32 token without confusing characters
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No 0, O, 1, I
        var random = RandomNumberGenerator.Create();
        var tokenLength = 8;
        var result = new char[tokenLength];

        var bytes = new byte[tokenLength];
        random.GetBytes(bytes);

        for (int i = 0; i < tokenLength; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }

        return new string(result);
    }

    private static string FormatShortCode(string token)
    {
        // Format as XXX-XXXXX for easier reading
        if (token.Length >= 8)
        {
            return $"{token.Substring(0, 3)}-{token.Substring(3)}";
        }
        return token;
    }

    private async Task<string> GenerateQrCodeAsync(string content)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            // In a production app, you'd save this to blob storage
            // For now, return a data URL
            var base64 = Convert.ToBase64String(qrCodeBytes);
            return $"data:image/png;base64,{base64}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate QR code for content: {Content}", content);
            return string.Empty;
        }
    }
}

public class ClientTrainerRelationship
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public int TrainerId { get; set; }
    public bool ShareSummary { get; set; }
    public bool RemindersEnabled { get; set; }
    public TimeSpan? ReminderTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    // Navigation properties
    public User? Client { get; set; }
    public User? Trainer { get; set; }
}