using Adaplio.Frontend.Models;
using System.Text.RegularExpressions;

namespace Adaplio.Frontend.Validators;

public class ClientProfileValidator
{
    public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
    {
        var clientModel = (ClientProfileModel)model;
        var errors = new List<string>();

        switch (propertyName)
        {
            case nameof(ClientProfileModel.DisplayName):
                if (string.IsNullOrEmpty(clientModel.DisplayName))
                    errors.Add("Display name is required");
                else if (clientModel.DisplayName.Length < 2 || clientModel.DisplayName.Length > 60)
                    errors.Add("Display name must be between 2 and 60 characters");
                else if (!BeValidDisplayName(clientModel.DisplayName))
                    errors.Add("Display name contains invalid characters");
                break;

            case nameof(ClientProfileModel.Timezone):
                if (string.IsNullOrEmpty(clientModel.Timezone))
                    errors.Add("Timezone is required");
                else if (!BeValidTimezone(clientModel.Timezone))
                    errors.Add("Please select a valid timezone");
                break;

            case nameof(ClientProfileModel.Injury):
                if (!string.IsNullOrEmpty(clientModel.Injury))
                {
                    if (clientModel.Injury.Length > 200)
                        errors.Add("Injury description cannot exceed 200 characters");
                    if (!NotContainHtml(clientModel.Injury))
                        errors.Add("HTML is not allowed in injury description");
                }
                break;

            case nameof(ClientProfileModel.AffectedSide):
                if (!string.IsNullOrEmpty(clientModel.AffectedSide) && !BeValidAffectedSide(clientModel.AffectedSide))
                    errors.Add("Please select a valid affected side");
                break;

            case nameof(ClientProfileModel.EmergencyContactName):
                if (!string.IsNullOrEmpty(clientModel.EmergencyContactName))
                {
                    if (clientModel.EmergencyContactName.Length > 100)
                        errors.Add("Emergency contact name cannot exceed 100 characters");
                    if (!BeValidContactName(clientModel.EmergencyContactName))
                        errors.Add("Emergency contact name contains invalid characters");
                }
                // Cross-validation: if name is provided, phone should be too
                if (!string.IsNullOrEmpty(clientModel.EmergencyContactName) && string.IsNullOrEmpty(clientModel.EmergencyContactPhone))
                    errors.Add("Emergency contact phone is required when name is provided");
                break;

            case nameof(ClientProfileModel.EmergencyContactPhone):
                if (!string.IsNullOrEmpty(clientModel.EmergencyContactPhone))
                {
                    if (!BeValidPhoneNumber(clientModel.EmergencyContactPhone))
                        errors.Add("Phone number must be in E.164 format (e.g., +1234567890)");
                }
                // Cross-validation: if phone is provided, name should be too
                if (!string.IsNullOrEmpty(clientModel.EmergencyContactPhone) && string.IsNullOrEmpty(clientModel.EmergencyContactName))
                    errors.Add("Emergency contact name is required when phone is provided");
                break;
        }

        return errors;
    };

    private bool BeValidDisplayName(string? displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return true;

        // Allow letters, numbers, spaces, hyphens, apostrophes, and dots
        var regex = new Regex(@"^[a-zA-Z0-9\s\-'\.]+$");
        return regex.IsMatch(displayName);
    }

    private bool BeValidTimezone(string? timezone)
    {
        if (string.IsNullOrEmpty(timezone)) return false;

        // List of common valid timezones
        var validTimezones = new HashSet<string>
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

        return validTimezones.Contains(timezone);
    }

    private bool BeValidAffectedSide(string? affectedSide)
    {
        if (string.IsNullOrEmpty(affectedSide)) return true;

        var validSides = new[] { "Left", "Right", "Both" };
        return validSides.Contains(affectedSide);
    }

    private bool BeValidContactName(string? contactName)
    {
        if (string.IsNullOrEmpty(contactName)) return true;

        // Allow letters, spaces, hyphens, and apostrophes
        var regex = new Regex(@"^[a-zA-Z\s\-']+$");
        return regex.IsMatch(contactName);
    }

    private bool BeValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber)) return true;

        // E.164 format: +[country code][number]
        var regex = new Regex(@"^\+[1-9]\d{1,14}$");
        return regex.IsMatch(phoneNumber);
    }

    private bool NotContainHtml(string? text)
    {
        if (string.IsNullOrEmpty(text)) return true;

        // Check for HTML tags
        var htmlRegex = new Regex(@"<[^>]*>");
        return !htmlRegex.IsMatch(text);
    }

    public Func<object, string, Task<IEnumerable<string>>> Validation(System.Linq.Expressions.Expression<Func<ClientProfileModel, object>> expression)
    {
        var propertyName = GetPropertyName(expression);
        return ValidateValue;
    }

    private static string GetPropertyName(System.Linq.Expressions.Expression<Func<ClientProfileModel, object>> expression)
    {
        if (expression.Body is System.Linq.Expressions.MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        if (expression.Body is System.Linq.Expressions.UnaryExpression unaryExpression &&
            unaryExpression.Operand is System.Linq.Expressions.MemberExpression memberExp)
        {
            return memberExp.Member.Name;
        }

        throw new ArgumentException("Invalid expression");
    }
}