using System.Text.RegularExpressions;
using System.Web;

namespace Adaplio.Api.Services;

public class InputSanitizer : IInputSanitizer
{
    private readonly ILogger<InputSanitizer> _logger;

    // Common regex patterns
    private static readonly Regex HtmlTagRegex = new(@"<[^>]*>", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled); // E.164 format
    private static readonly Regex UrlRegex = new(@"^https?://[^\s/$.?#].[^\s]*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TimeFormatRegex = new(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", RegexOptions.Compiled);

    // Known timezone identifiers (partial list for validation)
    private static readonly HashSet<string> ValidTimezones = new()
    {
        "UTC", "America/New_York", "America/Chicago", "America/Denver", "America/Los_Angeles",
        "America/Toronto", "America/Vancouver", "Europe/London", "Europe/Paris", "Europe/Berlin",
        "Europe/Rome", "Europe/Madrid", "Asia/Tokyo", "Asia/Shanghai", "Asia/Kolkata",
        "Australia/Sydney", "Australia/Melbourne", "Pacific/Auckland", "Africa/Johannesburg",
        "America/Sao_Paulo", "America/Mexico_City", "Asia/Dubai", "Asia/Singapore"
    };

    public InputSanitizer(ILogger<InputSanitizer> logger)
    {
        _logger = logger;
    }

    public string SanitizeString(string? input, int maxLength = 1000)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        try
        {
            // Remove any HTML tags
            var sanitized = HtmlTagRegex.Replace(input, string.Empty);

            // HTML decode to handle encoded characters
            sanitized = HttpUtility.HtmlDecode(sanitized);

            // Trim whitespace
            sanitized = sanitized.Trim();

            // Limit length
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength);
                _logger.LogWarning("Input truncated to {MaxLength} characters", maxLength);
            }

            // Remove control characters except common whitespace
            sanitized = Regex.Replace(sanitized, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing string input");
            return string.Empty;
        }
    }

    public string SanitizeHtml(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        try
        {
            // For profile data, we completely strip HTML
            // In a full implementation, you might use a library like HtmlSanitizer
            var sanitized = HtmlTagRegex.Replace(input, string.Empty);
            sanitized = HttpUtility.HtmlDecode(sanitized);
            return sanitized.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing HTML input");
            return string.Empty;
        }
    }

    public string SanitizeEmail(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        try
        {
            var sanitized = SanitizeString(input, 255).ToLowerInvariant();

            // Validate email format
            if (!EmailRegex.IsMatch(sanitized))
            {
                _logger.LogWarning("Invalid email format: {Email}", sanitized);
                return string.Empty;
            }

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing email input");
            return string.Empty;
        }
    }

    public string SanitizePhone(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        try
        {
            // Remove all non-digit characters except +
            var sanitized = Regex.Replace(input, @"[^\d+]", string.Empty);

            // Ensure it starts with + for E.164 format
            if (!sanitized.StartsWith("+"))
            {
                // If it starts with 1 (US/Canada), add +
                if (sanitized.StartsWith("1") && sanitized.Length == 11)
                {
                    sanitized = "+" + sanitized;
                }
                else
                {
                    _logger.LogWarning("Phone number does not start with country code: {Phone}", input);
                    return string.Empty;
                }
            }

            // Validate E.164 format
            if (!PhoneRegex.IsMatch(sanitized))
            {
                _logger.LogWarning("Invalid phone format: {Phone}", sanitized);
                return string.Empty;
            }

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing phone input");
            return string.Empty;
        }
    }

    public string SanitizeUrl(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        try
        {
            var sanitized = SanitizeString(input, 500).ToLowerInvariant();

            // Ensure it starts with http:// or https://
            if (!sanitized.StartsWith("http://") && !sanitized.StartsWith("https://"))
            {
                sanitized = "https://" + sanitized;
            }

            // Validate URL format
            if (!UrlRegex.IsMatch(sanitized) || !Uri.TryCreate(sanitized, UriKind.Absolute, out _))
            {
                _logger.LogWarning("Invalid URL format: {Url}", sanitized);
                return string.Empty;
            }

            return sanitized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sanitizing URL input");
            return string.Empty;
        }
    }

    public bool IsValidTimezone(string? timezone)
    {
        if (string.IsNullOrEmpty(timezone))
            return false;

        try
        {
            // Check against our known timezone list
            if (ValidTimezones.Contains(timezone))
                return true;

            // Try to parse with .NET's TimeZoneInfo for broader validation
            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timezone);
                return true;
            }
            catch (TimeZoneNotFoundException)
            {
                // Try IANA timezone IDs on non-Windows systems
                if (!OperatingSystem.IsWindows())
                {
                    try
                    {
                        var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                        return true;
                    }
                    catch
                    {
                        // Fall through to false
                    }
                }
            }

            _logger.LogWarning("Invalid timezone: {Timezone}", timezone);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating timezone");
            return false;
        }
    }

    public bool IsValidTimeFormat(string? time)
    {
        if (string.IsNullOrEmpty(time))
            return false;

        try
        {
            // Validate HH:mm format
            if (!TimeFormatRegex.IsMatch(time))
            {
                _logger.LogWarning("Invalid time format: {Time}", time);
                return false;
            }

            // Additional validation with TimeSpan parsing
            if (!TimeSpan.TryParseExact(time, @"hh\:mm", null, out _))
            {
                _logger.LogWarning("Time parsing failed: {Time}", time);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating time format");
            return false;
        }
    }
}