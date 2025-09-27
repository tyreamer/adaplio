using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Adaplio.Api.Profile;

public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this WebApplication app)
    {
        var profileGroup = app.MapGroup("/api/me/profile").WithTags("Profile").RequireAuthorization();

        // Profile management endpoints
        profileGroup.MapGet("", GetProfile);
        profileGroup.MapPatch("", UpdateProfile);

        // Client-specific endpoints for trainer management
        var clientGroup = app.MapGroup("/api/client").WithTags("Client Profile").RequireAuthorization();
        clientGroup.MapPatch("/trainers/{trainerId:int}/scope", UpdateSharingScope);
        clientGroup.MapDelete("/trainers/{trainerId:int}", RevokeTrainerAccess);

        // Upload endpoints
        var uploadGroup = app.MapGroup("/api/uploads").WithTags("Uploads").RequireAuthorization();
        uploadGroup.MapPost("/presign", GetPresignedUploadUrl);
        uploadGroup.MapPost("/upload", HandleFileUpload).AllowAnonymous(); // Allow anonymous for pre-signed uploads
        uploadGroup.MapGet("/files/{*filePath}", ServeFile).AllowAnonymous(); // Allow anonymous for serving files
    }

    private static async Task<IResult> GetProfile(
        HttpContext context,
        AppDbContext dbContext)
    {
        var userId = GetUserId(context);
        var userType = GetUserType(context);

        if (userId == null || userType == null)
            return Results.Unauthorized();

        var user = await dbContext.AppUsers
            .Include(u => u.ClientProfile)
                .ThenInclude(cp => cp!.ConsentGrants)
                    .ThenInclude(cg => cg.TrainerProfile)
                        .ThenInclude(tp => tp.User)
            .Include(u => u.TrainerProfile)
            .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

        if (user == null)
            return Results.NotFound();

        var profile = await BuildProfileResponse(user, userType, dbContext);
        return Results.Ok(profile);
    }

    private static async Task<IResult> UpdateProfile(
        UpdateProfileRequest request,
        HttpContext context,
        AppDbContext dbContext,
        IAuditService auditService,
        IInputSanitizer sanitizer)
    {
        var userId = GetUserId(context);
        var userType = GetUserType(context);

        if (userId == null || userType == null)
            return Results.Unauthorized();

        var user = await dbContext.AppUsers
            .Include(u => u.ClientProfile)
            .Include(u => u.TrainerProfile)
            .FirstOrDefaultAsync(u => u.Id == int.Parse(userId));

        if (user == null)
            return Results.NotFound();

        // Update common fields with sanitization and audit logging
        if (!string.IsNullOrEmpty(request.DisplayName))
        {
            var sanitizedDisplayName = sanitizer.SanitizeString(request.DisplayName, 100);
            if (!string.IsNullOrEmpty(sanitizedDisplayName))
            {
                if (userType == "client" && user.ClientProfile != null)
                {
                    var oldValue = user.ClientProfile.DisplayName;
                    user.ClientProfile.DisplayName = sanitizedDisplayName;
                    await auditService.LogProfileChangeAsync(int.Parse(userId), "display_name", oldValue, sanitizedDisplayName, userType);
                }
                else if (userType == "trainer" && user.TrainerProfile != null)
                {
                    var oldValue = user.TrainerProfile.FullName;
                    user.TrainerProfile.FullName = sanitizedDisplayName;
                    await auditService.LogProfileChangeAsync(int.Parse(userId), "full_name", oldValue, sanitizedDisplayName, userType);
                }
            }
        }

        // Update timezone for clients with validation
        if (userType == "client" && user.ClientProfile != null && !string.IsNullOrEmpty(request.Timezone))
        {
            if (sanitizer.IsValidTimezone(request.Timezone))
            {
                var oldValue = user.ClientProfile.Timezone;
                user.ClientProfile.Timezone = request.Timezone;
                await auditService.LogProfileChangeAsync(int.Parse(userId), "timezone", oldValue, request.Timezone, userType);
            }
            else
            {
                return Results.BadRequest($"Invalid timezone: {request.Timezone}");
            }
        }

        // Update client-specific fields
        if (userType == "client" && user.ClientProfile != null && request.Client != null)
        {
            await UpdateClientProfile(user.ClientProfile, request.Client);
        }

        // Update trainer-specific fields
        if (userType == "trainer" && user.TrainerProfile != null && request.Trainer != null)
        {
            await UpdateTrainerProfile(user.TrainerProfile, request.Trainer);
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        var profile = await BuildProfileResponse(user, userType, dbContext);
        return Results.Ok(profile);
    }

    private static async Task<IResult> UpdateSharingScope(
        int trainerId,
        UpdateSharingScopeRequest request,
        HttpContext context,
        AppDbContext dbContext,
        IAuditService auditService)
    {
        var userId = GetUserId(context);
        var userType = GetUserType(context);

        if (userId == null || userType != "client")
            return Results.Forbid();

        var clientProfile = await dbContext.ClientProfiles
            .FirstOrDefaultAsync(cp => cp.UserId == int.Parse(userId));

        if (clientProfile == null)
            return Results.NotFound();

        // Ensure trainer exists
        var trainerProfile = await dbContext.TrainerProfiles
            .FirstOrDefaultAsync(tp => tp.Id == trainerId);

        if (trainerProfile == null)
            return Results.NotFound("Trainer not found");

        // Find existing consent grant
        var consentGrant = await dbContext.ConsentGrants
            .FirstOrDefaultAsync(cg =>
                cg.ClientProfileId == clientProfile.Id &&
                cg.TrainerProfileId == trainerId &&
                cg.RevokedAt == null);

        if (consentGrant == null)
            return Results.NotFound("No active consent grant found");

        // If view_summary is turned off, also turn off view_details
        if (!request.ViewSummary && request.ViewDetails)
        {
            return Results.BadRequest("Cannot enable view_details without view_summary");
        }

        // Update or create consent grants for each scope
        await UpdateConsentGrantScope(dbContext, clientProfile.Id, trainerId, "view_summary", request.ViewSummary);
        await UpdateConsentGrantScope(dbContext, clientProfile.Id, trainerId, "view_details", request.ViewDetails);

        // Log the scope changes
        await auditService.LogSharingScopeChangeAsync(int.Parse(userId), trainerId, "view_summary", request.ViewSummary);
        await auditService.LogSharingScopeChangeAsync(int.Parse(userId), trainerId, "view_details", request.ViewDetails);

        await dbContext.SaveChangesAsync();

        return Results.Ok(new { message = "Sharing scope updated successfully" });
    }

    private static async Task<IResult> RevokeTrainerAccess(
        int trainerId,
        HttpContext context,
        AppDbContext dbContext,
        IAuditService auditService)
    {
        var userId = GetUserId(context);
        var userType = GetUserType(context);

        if (userId == null || userType != "client")
            return Results.Forbid();

        var clientProfile = await dbContext.ClientProfiles
            .FirstOrDefaultAsync(cp => cp.UserId == int.Parse(userId));

        if (clientProfile == null)
            return Results.NotFound();

        // Revoke all active consent grants for this trainer
        var consentGrants = await dbContext.ConsentGrants
            .Where(cg =>
                cg.ClientProfileId == clientProfile.Id &&
                cg.TrainerProfileId == trainerId &&
                cg.RevokedAt == null)
            .ToListAsync();

        foreach (var grant in consentGrants)
        {
            grant.RevokedAt = DateTimeOffset.UtcNow;
        }

        // Log the revocation
        await auditService.LogTrainerAccessRevokedAsync(int.Parse(userId), trainerId);

        await dbContext.SaveChangesAsync();

        return Results.Ok(new { message = "Trainer access revoked successfully" });
    }

    private static async Task<IResult> GetPresignedUploadUrl(
        PresignUploadRequest request,
        HttpContext context,
        IUploadService uploadService,
        IAuditService auditService)
    {
        try
        {
            if (request.Kind != "avatar" && request.Kind != "logo")
                return Results.BadRequest("Kind must be 'avatar' or 'logo'");

            if (!IsValidImageContentType(request.ContentType))
                return Results.BadRequest("Invalid content type. Only PNG, JPEG, and WebP are allowed");

            var userId = GetUserId(context);
            if (userId == null)
                return Results.Unauthorized();

            // Generate unique filename with user context
            var fileName = $"{request.Kind}_{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}";

            var result = await uploadService.GeneratePresignedUploadUrlAsync(
                fileName,
                request.ContentType,
                request.Kind
            );

            var response = new PresignUploadResponse(
                UploadUrl: result.UploadUrl,
                PublicUrl: result.PublicUrl
            );

            // Log the upload URL generation
            await auditService.LogUploadAsync(int.Parse(userId), request.Kind, fileName, true);

            return Results.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return Results.Problem("Failed to generate upload URL");
        }
    }

    private static async Task<IResult> HandleFileUpload(
        HttpContext context,
        IUploadService uploadService)
    {
        try
        {
            var uploadKey = context.Request.Query["key"].ToString();
            var token = context.Request.Query["token"].ToString();

            if (string.IsNullOrEmpty(uploadKey) || string.IsNullOrEmpty(token))
                return Results.BadRequest("Missing upload key or token");

            if (!context.Request.HasFormContentType)
                return Results.BadRequest("Request must be multipart/form-data");

            var form = await context.Request.ReadFormAsync();
            var file = form.Files.FirstOrDefault();

            if (file == null || file.Length == 0)
                return Results.BadRequest("No file provided");

            // Cast to UploadService to access the HandleFileUploadAsync method
            if (uploadService is UploadService concreteUploadService)
            {
                var success = await concreteUploadService.HandleFileUploadAsync(
                    uploadKey,
                    token,
                    file.OpenReadStream(),
                    file.ContentType
                );

                if (success)
                    return Results.Ok(new { message = "File uploaded successfully" });
                else
                    return Results.BadRequest("Upload failed");
            }

            return Results.Problem("Upload service not available");
        }
        catch (Exception ex)
        {
            return Results.Problem("Upload failed");
        }
    }

    private static async Task<IResult> ServeFile(
        string filePath,
        HttpContext context)
    {
        try
        {
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            var fullPath = Path.Combine(uploadsPath, filePath);

            // Security check: ensure the path is within the uploads directory
            var uploadsFullPath = Path.GetFullPath(uploadsPath);
            var requestedFullPath = Path.GetFullPath(fullPath);

            if (!requestedFullPath.StartsWith(uploadsFullPath))
                return Results.NotFound();

            if (!File.Exists(fullPath))
                return Results.NotFound();

            var contentType = GetContentTypeFromExtension(Path.GetExtension(fullPath));
            var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);

            return Results.File(fileStream, contentType);
        }
        catch
        {
            return Results.NotFound();
        }
    }

    // Helper methods
    private static string? GetUserId(HttpContext context)
    {
        return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static string? GetUserType(HttpContext context)
    {
        return context.User.FindFirst("user_type")?.Value;
    }

    private static async Task<ProfileResponse> BuildProfileResponse(AppUser user, string userType, AppDbContext dbContext)
    {
        ClientProfileData? clientData = null;
        TrainerProfileData? trainerData = null;

        if (userType == "client" && user.ClientProfile != null)
        {
            clientData = await BuildClientProfileData(user.ClientProfile, dbContext);
        }
        else if (userType == "trainer" && user.TrainerProfile != null)
        {
            trainerData = BuildTrainerProfileData(user.TrainerProfile);
        }

        return new ProfileResponse(
            Role: userType,
            DisplayName: userType == "client" ? user.ClientProfile?.DisplayName : user.TrainerProfile?.FullName,
            Timezone: user.ClientProfile?.Timezone,
            AvatarUrl: null, // TODO: Add avatar URL field to profiles
            Client: clientData,
            Trainer: trainerData
        );
    }

    private static async Task<ClientProfileData> BuildClientProfileData(ClientProfile profile, AppDbContext dbContext)
    {
        // Parse preferences JSON
        var preferences = ParseClientPreferences(profile.PreferencesJson);

        // Get connected trainers
        var consentGrants = await dbContext.ConsentGrants
            .Where(cg => cg.ClientProfileId == profile.Id && cg.RevokedAt == null)
            .Include(cg => cg.TrainerProfile)
            .ThenInclude(tp => tp.User)
            .ToListAsync();

        var connectedTrainers = consentGrants
            .GroupBy(cg => cg.TrainerProfileId)
            .Select(g => new ConnectedTrainerData(
                TrainerId: g.Key.ToString(),
                DisplayName: g.First().TrainerProfile.FullName ?? g.First().TrainerProfile.User.Email,
                Scope: new SharingScope(
                    ViewSummary: g.Any(x => x.Scope == "view_summary"),
                    ViewDetails: g.Any(x => x.Scope == "view_details")
                )
            ))
            .ToList();

        return new ClientProfileData(
            Injury: preferences.Injury,
            AffectedSide: preferences.AffectedSide,
            EmergencyContactName: preferences.EmergencyContactName,
            EmergencyContactPhone: preferences.EmergencyContactPhone,
            LargeTextMode: preferences.LargeTextMode,
            Reminders: new ReminderSettings(
                Enabled: preferences.Reminders.Enabled,
                Time: preferences.Reminders.Time,
                Channels: new NotificationChannels(
                    Push: preferences.Reminders.Channels.Push,
                    Email: preferences.Reminders.Channels.Email,
                    Sms: preferences.Reminders.Channels.Sms
                ),
                QuietHours: preferences.Reminders.QuietHours
            ),
            Trainers: connectedTrainers
        );
    }

    private static TrainerProfileData BuildTrainerProfileData(TrainerProfile profile)
    {
        return new TrainerProfileData(
            Credentials: null, // TODO: Add credentials field
            ClinicName: profile.PracticeName,
            Location: null, // TODO: Add location field
            Website: null, // TODO: Add website field
            Phone: profile.Phone,
            Bio: profile.Bio,
            Specialties: new List<string>(), // TODO: Add specialties field
            Availability: new List<AvailabilityWindow>(), // TODO: Add availability field
            DefaultReminderTime: null, // TODO: Add default reminder time field
            LogoUrl: null, // TODO: Add logo URL field
            LicenseNumber: profile.LicenseNumber
        );
    }

    private static async Task UpdateClientProfile(ClientProfile profile, ClientProfileUpdateData update)
    {
        var preferences = ParseClientPreferences(profile.PreferencesJson);

        // Update fields
        if (update.Injury != null) preferences.Injury = update.Injury;
        if (update.AffectedSide != null) preferences.AffectedSide = update.AffectedSide;
        if (update.EmergencyContactName != null) preferences.EmergencyContactName = update.EmergencyContactName;
        if (update.EmergencyContactPhone != null) preferences.EmergencyContactPhone = update.EmergencyContactPhone;
        if (update.LargeTextMode.HasValue) preferences.LargeTextMode = update.LargeTextMode.Value;

        if (update.Reminders != null)
        {
            if (update.Reminders.Enabled.HasValue)
                preferences.Reminders.Enabled = update.Reminders.Enabled.Value;
            if (update.Reminders.Time != null)
                preferences.Reminders.Time = update.Reminders.Time;
            if (update.Reminders.Channels != null)
            {
                if (update.Reminders.Channels.Push.HasValue)
                    preferences.Reminders.Channels.Push = update.Reminders.Channels.Push.Value;
                if (update.Reminders.Channels.Email.HasValue)
                    preferences.Reminders.Channels.Email = update.Reminders.Channels.Email.Value;
                if (update.Reminders.Channels.Sms.HasValue)
                    preferences.Reminders.Channels.Sms = update.Reminders.Channels.Sms.Value;
            }
            if (update.Reminders.QuietHours != null)
                preferences.Reminders.QuietHours = update.Reminders.QuietHours;
        }

        profile.PreferencesJson = JsonSerializer.Serialize(preferences);
    }

    private static async Task UpdateTrainerProfile(TrainerProfile profile, TrainerProfileUpdateData update)
    {
        if (update.ClinicName != null) profile.PracticeName = update.ClinicName;
        if (update.Phone != null) profile.Phone = update.Phone;
        if (update.Bio != null) profile.Bio = update.Bio;
        if (update.LicenseNumber != null) profile.LicenseNumber = update.LicenseNumber;

        // TODO: Add fields for credentials, location, website, specialties, availability, etc.
    }

    private static async Task UpdateConsentGrantScope(AppDbContext dbContext, int clientProfileId, int trainerProfileId, string scope, bool enabled)
    {
        var existingGrant = await dbContext.ConsentGrants
            .FirstOrDefaultAsync(cg =>
                cg.ClientProfileId == clientProfileId &&
                cg.TrainerProfileId == trainerProfileId &&
                cg.Scope == scope &&
                cg.RevokedAt == null);

        if (enabled && existingGrant == null)
        {
            // Create new grant
            var newGrant = new ConsentGrant
            {
                ClientProfileId = clientProfileId,
                TrainerProfileId = trainerProfileId,
                Scope = scope,
                GrantedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };
            dbContext.ConsentGrants.Add(newGrant);
        }
        else if (!enabled && existingGrant != null)
        {
            // Revoke existing grant
            existingGrant.RevokedAt = DateTimeOffset.UtcNow;
        }
    }

    private static ClientPreferences ParseClientPreferences(string? preferencesJson)
    {
        if (string.IsNullOrEmpty(preferencesJson))
        {
            return new ClientPreferences();
        }

        try
        {
            return JsonSerializer.Deserialize<ClientPreferences>(preferencesJson) ?? new ClientPreferences();
        }
        catch
        {
            return new ClientPreferences();
        }
    }

    private static bool IsValidImageContentType(string contentType)
    {
        return contentType.ToLower() switch
        {
            "image/png" => true,
            "image/jpeg" => true,
            "image/jpg" => true,
            "image/webp" => true,
            _ => false
        };
    }

    private static string GetFileExtension(string contentType)
    {
        return contentType.ToLower() switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "image/jpg" => "jpg",
            "image/webp" => "webp",
            _ => "jpg"
        };
    }

    private static string GetContentTypeFromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    // Internal model for client preferences JSON
    private class ClientPreferences
    {
        public string? Injury { get; set; }
        public string? AffectedSide { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public bool LargeTextMode { get; set; } = false;
        public MutableReminderSettings Reminders { get; set; } = new()
        {
            Enabled = true,
            Time = "19:00",
            Channels = new MutableNotificationChannels
            {
                Push = true,
                Email = true,
                Sms = false
            },
            QuietHours = new QuietHours("22:00", "07:00")
        };
    }

    private class MutableReminderSettings
    {
        public bool Enabled { get; set; }
        public string? Time { get; set; }
        public MutableNotificationChannels Channels { get; set; } = new();
        public QuietHours? QuietHours { get; set; }
    }

    private class MutableNotificationChannels
    {
        public bool Push { get; set; }
        public bool Email { get; set; }
        public bool Sms { get; set; }
    }
}