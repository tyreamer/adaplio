using Adaplio.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Adaplio.Api.Tests.Services;

public class InputSanitizerTests
{
    private readonly Mock<ILogger<InputSanitizer>> _mockLogger;
    private readonly InputSanitizer _inputSanitizer;

    public InputSanitizerTests()
    {
        _mockLogger = new Mock<ILogger<InputSanitizer>>();
        _inputSanitizer = new InputSanitizer(_mockLogger.Object);
    }

    [Theory]
    [InlineData("John Doe", "John Doe")] // Normal name
    [InlineData("Dr. Sarah O'Connor", "Dr. Sarah O'Connor")] // With apostrophe
    [InlineData("Jean-Luc Picard", "Jean-Luc Picard")] // With hyphen
    [InlineData("José María", "José María")] // With accents
    [InlineData("  John Doe  ", "John Doe")] // With whitespace
    [InlineData("John<script>alert('xss')</script>Doe", "JohnDoe")] // XSS attempt
    [InlineData("John & Jane", "John & Jane")] // With ampersand (should be preserved)
    public void SanitizeString_ShouldReturnCleanedText(string input, string expected)
    {
        // Act
        var result = _inputSanitizer.SanitizeString(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SanitizeString_WithInvalidInput_ShouldReturnEmpty(string? input)
    {
        // Act
        var result = _inputSanitizer.SanitizeString(input);

        // Assert
        result.Should().Be("");
    }

    [Theory]
    [InlineData("test@example.com", "test@example.com")] // Valid email
    [InlineData("user.name+tag@domain.co.uk", "user.name+tag@domain.co.uk")] // Complex valid email
    [InlineData("invalid-email", "")] // Invalid format
    [InlineData("@domain.com", "")] // Missing username
    [InlineData("user@", "")] // Missing domain
    [InlineData("", "")] // Empty
    [InlineData(null, "")] // Null
    public void SanitizeEmail_ShouldReturnValidEmailOrEmpty(string? input, string expected)
    {
        // Act
        var result = _inputSanitizer.SanitizeEmail(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("+1234567890", "+1234567890")] // Valid E.164
    [InlineData("+1-555-123-4567", "+15551234567")] // With formatting, should clean
    [InlineData("+1 (555) 123-4567", "+15551234567")] // With spaces and parentheses
    [InlineData("555-123-4567", "")] // Missing country code
    [InlineData("+1234567890123456", "")] // Too long
    [InlineData("+123", "")] // Too short
    [InlineData("invalid-phone", "")] // Invalid format
    public void SanitizePhone_ShouldReturnValidE164OrEmpty(string input, string expected)
    {
        // Act
        var result = _inputSanitizer.SanitizePhone(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("https://example.com", "https://example.com")] // Valid HTTPS
    [InlineData("http://example.com", "http://example.com")] // Valid HTTP
    [InlineData("https://subdomain.example.com/path", "https://subdomain.example.com/path")] // With subdomain and path
    [InlineData("javascript:alert('xss')", "")] // XSS attempt
    [InlineData("ftp://example.com", "")] // Invalid protocol
    [InlineData("example.com", "https://example.com")] // Missing protocol, should add https
    [InlineData("www.example.com", "https://www.example.com")] // Missing protocol with www
    [InlineData("not-a-url", "")] // Invalid URL
    public void SanitizeUrl_ShouldReturnValidHttpUrlOrEmpty(string input, string expected)
    {
        // Act
        var result = _inputSanitizer.SanitizeUrl(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("This is a normal bio.", "This is a normal bio.")] // Normal text
    [InlineData("<script>alert('xss')</script>Bio content", "Bio content")] // HTML tags removed
    [InlineData("Bio with <b>bold</b> text", "Bio with bold text")] // HTML tags removed
    [InlineData("Bio with\n\nnew lines", "Bio with\n\nnew lines")] // Line breaks preserved
    [InlineData("Bio with emoji 😊", "Bio with emoji 😊")] // Emoji preserved
    [InlineData("   Bio with extra spaces   ", "Bio with extra spaces")] // Trimmed
    public void SanitizeHtml_ShouldRemoveHtmlAndTrim(string input, string expected)
    {
        // Act
        var result = _inputSanitizer.SanitizeHtml(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("America/New_York", true)]
    [InlineData("Europe/London", true)]
    [InlineData("Asia/Tokyo", true)]
    [InlineData("UTC", true)]
    [InlineData("Invalid/Timezone", false)]
    [InlineData("EST", false)] // Abbreviations not allowed
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidTimezone_ShouldReturnCorrectResult(string? timezone, bool expected)
    {
        // Act
        var result = _inputSanitizer.IsValidTimezone(timezone);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("12:00", true)]
    [InlineData("23:59", true)]
    [InlineData("00:00", true)]
    [InlineData("24:00", false)] // Invalid hour
    [InlineData("12:60", false)] // Invalid minute
    [InlineData("invalid", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidTimeFormat_ShouldReturnCorrectResult(string? time, bool expected)
    {
        // Act
        var result = _inputSanitizer.IsValidTimeFormat(time);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void InputSanitizer_Constructor_ShouldInitializeCorrectly()
    {
        // Assert
        _inputSanitizer.Should().NotBeNull();
    }
}