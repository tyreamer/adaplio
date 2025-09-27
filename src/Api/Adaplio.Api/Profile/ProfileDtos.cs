using System.ComponentModel.DataAnnotations;

namespace Adaplio.Api.Profile;

// Profile response DTOs
public record ProfileResponse(
    string Role,
    string? DisplayName,
    string? Timezone,
    string? AvatarUrl,
    ClientProfileData? Client,
    TrainerProfileData? Trainer
);

public record ClientProfileData(
    string? Injury,
    string? AffectedSide,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    bool LargeTextMode,
    ReminderSettings Reminders,
    List<ConnectedTrainerData> Trainers
);

public record TrainerProfileData(
    string? Credentials,
    string? ClinicName,
    string? Location,
    string? Website,
    string? Phone,
    string? Bio,
    List<string> Specialties,
    List<AvailabilityWindow> Availability,
    string? DefaultReminderTime,
    string? LogoUrl,
    string? LicenseNumber
);

public record ReminderSettings(
    bool Enabled,
    string? Time,
    NotificationChannels Channels,
    QuietHours? QuietHours
);

public record NotificationChannels(
    bool Push,
    bool Email,
    bool Sms
);

public record QuietHours(
    string Start,
    string End
);

public record ConnectedTrainerData(
    string TrainerId,
    string DisplayName,
    SharingScope Scope
);

public record SharingScope(
    bool ViewSummary,
    bool ViewDetails
);

public record AvailabilityWindow(
    string Day,
    string Start,
    string End
);

// Profile update request DTOs
public record UpdateProfileRequest(
    [MaxLength(60)] string? DisplayName = null,
    [MaxLength(50)] string? Timezone = null,
    string? AvatarUrl = null,
    ClientProfileUpdateData? Client = null,
    TrainerProfileUpdateData? Trainer = null
);

public record ClientProfileUpdateData(
    [MaxLength(200)] string? Injury = null,
    string? AffectedSide = null, // "Left", "Right", "Both"
    [MaxLength(100)] string? EmergencyContactName = null,
    [Phone] string? EmergencyContactPhone = null,
    bool? LargeTextMode = null,
    ReminderSettingsUpdate? Reminders = null
);

public record TrainerProfileUpdateData(
    [MaxLength(100)] string? Credentials = null,
    [MaxLength(200)] string? ClinicName = null,
    [MaxLength(200)] string? Location = null,
    [Url] string? Website = null,
    [Phone] string? Phone = null,
    [MaxLength(1000)] string? Bio = null,
    List<string>? Specialties = null,
    List<AvailabilityWindow>? Availability = null,
    string? DefaultReminderTime = null,
    string? LogoUrl = null,
    [MaxLength(100)] string? LicenseNumber = null
);

public record ReminderSettingsUpdate(
    bool? Enabled = null,
    string? Time = null, // "HH:mm" format
    NotificationChannelsUpdate? Channels = null,
    QuietHours? QuietHours = null
);

public record NotificationChannelsUpdate(
    bool? Push = null,
    bool? Email = null,
    bool? Sms = null
);

// Sharing scope update DTOs
public record UpdateSharingScopeRequest(
    bool ViewSummary,
    bool ViewDetails
);

// Upload DTOs
public record PresignUploadRequest(
    [Required] string Kind, // "avatar" or "logo"
    [Required] string ContentType
);

public record PresignUploadResponse(
    string UploadUrl,
    string PublicUrl
);

// Validation attributes
public class PhoneAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null) return true;
        var phone = value.ToString();
        return string.IsNullOrEmpty(phone) ||
               (phone.StartsWith("+") && phone.Length >= 10 && phone.Length <= 20);
    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must be in E.164 format (e.g., +1234567890)";
    }
}