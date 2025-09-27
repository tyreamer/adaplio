using Adaplio.Api.Data;
using Adaplio.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Adaplio.Api.Tests.Services;

public class AuditServiceTests
{
    private readonly Mock<ILogger<AuditService>> _mockLogger;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;

    public AuditServiceTests()
    {
        _mockLogger = new Mock<ILogger<AuditService>>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
    }

    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task LogProfileChangeAsync_ShouldLogCorrectly()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var auditService = new AuditService(context, _mockLogger.Object, _mockHttpContextAccessor.Object);

        var userId = 1;
        var field = "DisplayName";
        var oldValue = "Old Name";
        var newValue = "New Name";

        // Act
        await auditService.LogProfileChangeAsync(userId, field, oldValue, newValue);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Profile update")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogSharingScopeChangeAsync_ShouldLogCorrectly()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var auditService = new AuditService(context, _mockLogger.Object, _mockHttpContextAccessor.Object);

        var clientId = 1;
        var trainerId = 2;
        var scope = "detailed";
        var granted = true;

        // Act
        await auditService.LogSharingScopeChangeAsync(clientId, trainerId, scope, granted);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Sharing scope change")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogUploadAsync_ShouldLogCorrectly()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var auditService = new AuditService(context, _mockLogger.Object, _mockHttpContextAccessor.Object);

        var userId = 1;
        var uploadType = "avatar";
        var fileName = "avatar.jpg";
        var success = true;

        // Act
        await auditService.LogUploadAsync(userId, uploadType, fileName, success);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Upload attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LogTrainerAccessRevokedAsync_ShouldLogCorrectly()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var auditService = new AuditService(context, _mockLogger.Object, _mockHttpContextAccessor.Object);

        var clientUserId = 1;
        var trainerUserId = 2;

        // Act
        await auditService.LogTrainerAccessRevokedAsync(clientUserId, trainerUserId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Trainer access revoked")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AuditServiceConstructor_ShouldInitializeCorrectly()
    {
        // Arrange
        using var context = GetInMemoryContext();

        // Act
        var auditService = new AuditService(context, _mockLogger.Object, _mockHttpContextAccessor.Object);

        // Assert
        auditService.Should().NotBeNull();
    }

    [Fact]
    public async Task LogProfileChangeAsync_WithNullValues_ShouldHandleGracefully()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var auditService = new AuditService(context, _mockLogger.Object, _mockHttpContextAccessor.Object);

        var userId = 1;
        var field = "DisplayName";

        // Act & Assert - should not throw
        await auditService.LogProfileChangeAsync(userId, field, null, null);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Profile update")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}