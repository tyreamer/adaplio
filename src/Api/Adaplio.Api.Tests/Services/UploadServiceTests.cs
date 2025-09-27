using Adaplio.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Adaplio.Api.Tests.Services;

public class UploadServiceTests
{
    private readonly Mock<ILogger<UploadService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly UploadService _uploadService;

    public UploadServiceTests()
    {
        _mockLogger = new Mock<ILogger<UploadService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _uploadService = new UploadService(_mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GeneratePresignedUploadUrlAsync_ShouldReturnValidUrls()
    {
        // Arrange
        var fileName = "test-upload.jpg";
        var contentType = "image/jpeg";
        var uploadType = "avatar";

        // Act
        var result = await _uploadService.GeneratePresignedUploadUrlAsync(fileName, contentType, uploadType);

        // Assert
        result.Should().NotBeNull();
        result.UploadUrl.Should().NotBeNullOrEmpty();
        result.PublicUrl.Should().NotBeNullOrEmpty();

        // URLs should be different
        result.UploadUrl.Should().NotBe(result.PublicUrl);

        // Both should be valid URIs
        Uri.IsWellFormedUriString(result.UploadUrl, UriKind.Absolute).Should().BeTrue();
        Uri.IsWellFormedUriString(result.PublicUrl, UriKind.Absolute).Should().BeTrue();
    }

    [Fact]
    public async Task GeneratePresignedUploadUrlAsync_ShouldIncludeMetadata()
    {
        // Arrange
        var fileName = "test.jpg";
        var contentType = "image/jpeg";
        var uploadType = "avatar";

        // Act
        var result = await _uploadService.GeneratePresignedUploadUrlAsync(fileName, contentType, uploadType);

        // Assert
        result.Fields.Should().NotBeNull();
        result.Fields.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateUploadedFileAsync_WithValidUrl_ShouldReturnTrue()
    {
        // Arrange
        var publicUrl = "https://cdn.example.com/uploads/test.jpg";

        // Act
        var isValid = await _uploadService.ValidateUploadedFileAsync(publicUrl);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFileAsync_WithValidUrl_ShouldReturnTrue()
    {
        // Arrange
        var publicUrl = "https://cdn.example.com/uploads/test.jpg";

        // Act
        var result = await _uploadService.DeleteFileAsync(publicUrl);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateUploadedFileAsync_WithInvalidUrl_ShouldReturnFalse()
    {
        // Arrange
        var invalidUrl = "https://malicious-site.com/fake.jpg";

        // Act
        var isValid = await _uploadService.ValidateUploadedFileAsync(invalidUrl);

        // Assert
        isValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ValidateUploadedFileAsync_WithNullOrEmptyUrl_ShouldReturnFalse(string? url)
    {
        // Act
        var isValid = await _uploadService.ValidateUploadedFileAsync(url!);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void UploadService_Constructor_ShouldInitializeCorrectly()
    {
        // Assert
        _uploadService.Should().NotBeNull();
    }
}