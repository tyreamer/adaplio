using Adaplio.Frontend.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Adaplio.Frontend.Services;

public class FormValidationService
{
    private readonly ILogger<FormValidationService> _logger;

    public FormValidationService(ILogger<FormValidationService> logger)
    {
        _logger = logger;
    }

    public async Task<ValidationResult> ValidateClientProfileAsync(ClientProfileModel model)
    {
        var result = new ValidationResult();

        // Basic data annotation validation
        var context = new ValidationContext(model);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (!Validator.TryValidateObject(model, context, validationResults, true))
        {
            foreach (var validationResult in validationResults)
            {
                result.Errors.Add(validationResult.ErrorMessage ?? "Validation error");
            }
        }

        // Custom business rules
        await ValidateDisplayNameAsync(model.DisplayName, result);
        await ValidateEmergencyContactAsync(model, result);
        await ValidateTimezoneAsync(model.Timezone, result);

        return result;
    }

    public async Task<ValidationResult> ValidateTrainerProfileAsync(TrainerProfileModel model)
    {
        var result = new ValidationResult();

        // Basic data annotation validation
        var context = new ValidationContext(model);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (!Validator.TryValidateObject(model, context, validationResults, true))
        {
            foreach (var validationResult in validationResults)
            {
                result.Errors.Add(validationResult.ErrorMessage ?? "Validation error");
            }
        }

        // Custom business rules
        await ValidateDisplayNameAsync(model.DisplayName, result);
        await ValidateWebsiteAsync(model.Website, result);
        await ValidateSpecialtiesAsync(model.Specialties, result);
        await ValidateProfessionalInfoAsync(model, result);

        return result;
    }

    private async Task ValidateDisplayNameAsync(string? displayName, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return;

        // Check for inappropriate content
        if (await ContainsInappropriateContentAsync(displayName))
        {
            result.Errors.Add("Display name contains inappropriate content");
        }

        // Check for valid characters (letters, numbers, spaces, common punctuation)
        if (!Regex.IsMatch(displayName, @"^[a-zA-Z0-9\s\-\.']+$"))
        {
            result.Errors.Add("Display name contains invalid characters");
        }

        // Check for minimum meaningful content
        if (displayName.Trim().Length < 2)
        {
            result.Errors.Add("Display name must be at least 2 characters long");
        }
    }

    private async Task ValidateEmergencyContactAsync(ClientProfileModel model, ValidationResult result)
    {
        var hasName = !string.IsNullOrWhiteSpace(model.EmergencyContactName);
        var hasPhone = !string.IsNullOrWhiteSpace(model.EmergencyContactPhone);

        // If one is provided, both should be provided
        if (hasName && !hasPhone)
        {
            result.Errors.Add("Emergency contact phone number is required when name is provided");
        }
        else if (hasPhone && !hasName)
        {
            result.Errors.Add("Emergency contact name is required when phone number is provided");
        }

        // Validate phone format if provided
        if (hasPhone && !IsValidPhoneNumber(model.EmergencyContactPhone!))
        {
            result.Errors.Add("Emergency contact phone must be in international format (e.g., +1-555-123-4567)");
        }

        await Task.CompletedTask;
    }

    private async Task ValidateTimezoneAsync(string? timezone, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            return;

        var validTimezones = GetValidTimezones();
        if (!validTimezones.Contains(timezone))
        {
            result.Errors.Add("Please select a valid timezone");
        }

        await Task.CompletedTask;
    }

    private async Task ValidateWebsiteAsync(string? website, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(website))
            return;

        if (!Uri.TryCreate(website, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            result.Errors.Add("Website must be a valid HTTP or HTTPS URL");
        }

        await Task.CompletedTask;
    }

    private async Task ValidateSpecialtiesAsync(List<string> specialties, ValidationResult result)
    {
        if (specialties.Count > 10)
        {
            result.Errors.Add("Maximum 10 specialties allowed");
        }

        foreach (var specialty in specialties)
        {
            if (string.IsNullOrWhiteSpace(specialty))
            {
                result.Errors.Add("Specialty cannot be empty");
                continue;
            }

            if (specialty.Length > 50)
            {
                result.Errors.Add("Each specialty must be 50 characters or less");
            }

            if (await ContainsInappropriateContentAsync(specialty))
            {
                result.Errors.Add($"Specialty '{specialty}' contains inappropriate content");
            }
        }

        // Check for duplicates
        var duplicates = specialties.GroupBy(s => s.ToLower().Trim())
                                   .Where(g => g.Count() > 1)
                                   .Select(g => g.Key);

        if (duplicates.Any())
        {
            result.Errors.Add("Duplicate specialties found");
        }
    }

    private async Task ValidateProfessionalInfoAsync(TrainerProfileModel model, ValidationResult result)
    {
        // License number format validation (if provided)
        if (!string.IsNullOrWhiteSpace(model.LicenseNumber))
        {
            if (!Regex.IsMatch(model.LicenseNumber, @"^[A-Z0-9\-]+$"))
            {
                result.Errors.Add("License number should contain only letters, numbers, and hyphens");
            }
        }

        // Bio content validation
        if (!string.IsNullOrWhiteSpace(model.Bio))
        {
            if (await ContainsInappropriateContentAsync(model.Bio))
            {
                result.Errors.Add("Bio contains inappropriate content");
            }

            // Check for HTML content
            if (Regex.IsMatch(model.Bio, @"<[^>]*>"))
            {
                result.Errors.Add("Bio cannot contain HTML tags");
            }
        }

        await Task.CompletedTask;
    }

    private async Task<bool> ContainsInappropriateContentAsync(string content)
    {
        // Simple profanity filter - in production, use a proper service
        var inappropriateWords = new[] { "spam", "test123", "dummy" };
        var lowerContent = content.ToLower();

        await Task.CompletedTask;
        return inappropriateWords.Any(word => lowerContent.Contains(word));
    }

    private bool IsValidPhoneNumber(string phoneNumber)
    {
        // E.164 format: +[country code][number]
        return Regex.IsMatch(phoneNumber, @"^\+[1-9]\d{1,14}$");
    }

    private HashSet<string> GetValidTimezones()
    {
        return new HashSet<string>
        {
            "UTC",
            "America/New_York",
            "America/Chicago",
            "America/Denver",
            "America/Los_Angeles",
            "America/Toronto",
            "America/Vancouver",
            "Europe/London",
            "Europe/Paris",
            "Europe/Berlin",
            "Europe/Rome",
            "Europe/Madrid",
            "Asia/Tokyo",
            "Asia/Shanghai",
            "Asia/Kolkata",
            "Australia/Sydney",
            "Australia/Melbourne",
            "Pacific/Auckland",
            "Africa/Johannesburg",
            "America/Sao_Paulo",
            "America/Mexico_City",
            "Asia/Dubai",
            "Asia/Singapore"
        };
    }
}

public class ValidationResult
{
    public List<string> Errors { get; } = new();
    public bool IsValid => !Errors.Any();
}