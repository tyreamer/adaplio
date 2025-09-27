namespace Adaplio.Api.Services;

public interface IInputSanitizer
{
    string SanitizeString(string? input, int maxLength = 1000);
    string SanitizeHtml(string? input);
    string SanitizeEmail(string? input);
    string SanitizePhone(string? input);
    string SanitizeUrl(string? input);
    bool IsValidTimezone(string? timezone);
    bool IsValidTimeFormat(string? time);
}