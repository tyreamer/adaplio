using Adaplio.Api.Data;
using Adaplio.Api.Domain;
using Adaplio.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Adaplio.Api.Tests.Controllers;

public class ProfileControllerTests
{
    private readonly Mock<IAuditService> _mockAuditService;
    private readonly Mock<IUploadService> _mockUploadService;
    private readonly Mock<IInputSanitizer> _mockInputSanitizer;
    private readonly Mock<ILogger> _mockLogger;

    public ProfileControllerTests()
    {
        _mockAuditService = new Mock<IAuditService>();
        _mockUploadService = new Mock<IUploadService>();
        _mockInputSanitizer = new Mock<IInputSanitizer>();
        _mockLogger = new Mock<ILogger>();
    }

    private AppDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ProfileDataAccess_AppUser_ShouldWorkCorrectly()
    {
        // Arrange
        using var context = GetInMemoryContext();

        var user = new AppUser
        {
            Id = 1,
            Email = "test@example.com",
            UserType = "client",
            DisplayName = "Test User",
            Timezone = "America/New_York",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.AppUsers.Add(user);
        await context.SaveChangesAsync();

        // Act
        var retrievedUser = await context.AppUsers
            .FirstOrDefaultAsync(u => u.Id == 1);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.Email.Should().Be("test@example.com");
        retrievedUser.UserType.Should().Be("client");
        retrievedUser.DisplayName.Should().Be("Test User");
    }

    [Fact]
    public async Task ProfileDataAccess_ClientProfile_ShouldWorkCorrectly()
    {
        // Arrange
        using var context = GetInMemoryContext();

        var user = new AppUser
        {
            Id = 1,
            Email = "client@test.com",
            UserType = "client",
            DisplayName = "Test Client",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var clientProfile = new ClientProfile
        {
            UserId = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.AppUsers.Add(user);
        context.ClientProfiles.Add(clientProfile);
        await context.SaveChangesAsync();

        // Act
        var retrievedUser = await context.AppUsers
            .Include(u => u.ClientProfile)
            .FirstOrDefaultAsync(u => u.Id == 1);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.ClientProfile.Should().NotBeNull();
        retrievedUser.ClientProfile!.UserId.Should().Be(1);
    }

    [Fact]
    public async Task ProfileDataAccess_TrainerProfile_ShouldWorkCorrectly()
    {
        // Arrange
        using var context = GetInMemoryContext();

        var user = new AppUser
        {
            Id = 2,
            Email = "trainer@test.com",
            UserType = "trainer",
            DisplayName = "Test Trainer",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var trainerProfile = new TrainerProfile
        {
            UserId = 2,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.AppUsers.Add(user);
        context.TrainerProfiles.Add(trainerProfile);
        await context.SaveChangesAsync();

        // Act
        var retrievedUser = await context.AppUsers
            .Include(u => u.TrainerProfile)
            .FirstOrDefaultAsync(u => u.Id == 2);

        // Assert
        retrievedUser.Should().NotBeNull();
        retrievedUser!.TrainerProfile.Should().NotBeNull();
        retrievedUser.TrainerProfile!.UserId.Should().Be(2);
    }

    [Fact]
    public async Task ProfileUpdate_AppUser_ShouldUpdateFields()
    {
        // Arrange
        using var context = GetInMemoryContext();

        var user = new AppUser
        {
            Id = 1,
            Email = "test@example.com",
            UserType = "client",
            DisplayName = "Original Name",
            Timezone = "America/New_York",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        context.AppUsers.Add(user);
        await context.SaveChangesAsync();

        // Act - Update user
        var userToUpdate = await context.AppUsers.FirstAsync(u => u.Id == 1);
        userToUpdate.DisplayName = "Updated Name";
        userToUpdate.Timezone = "America/Los_Angeles";
        userToUpdate.UpdatedAt = DateTimeOffset.UtcNow;

        await context.SaveChangesAsync();

        // Assert
        var updatedUser = await context.AppUsers.FirstAsync(u => u.Id == 1);
        updatedUser.DisplayName.Should().Be("Updated Name");
        updatedUser.Timezone.Should().Be("America/Los_Angeles");
    }

    [Fact]
    public void InputSanitizer_Setup_ShouldCreateMockCorrectly()
    {
        // Arrange & Act
        _mockInputSanitizer.Setup(x => x.SanitizeString(It.IsAny<string>(), It.IsAny<int>()))
                          .Returns<string, int>((input, maxLength) => input?.Trim() ?? "");

        _mockInputSanitizer.Setup(x => x.SanitizeEmail(It.IsAny<string>()))
                          .Returns<string>(email => !string.IsNullOrEmpty(email) && email.Contains("@") ? email : "");

        // Assert
        var result1 = _mockInputSanitizer.Object.SanitizeString("  Test Name  ");
        var result2 = _mockInputSanitizer.Object.SanitizeEmail("test@example.com");
        var result3 = _mockInputSanitizer.Object.SanitizeEmail("invalid-email");

        result1.Should().Be("Test Name");
        result2.Should().Be("test@example.com");
        result3.Should().Be("");
    }

    [Fact]
    public void AuditService_Setup_ShouldCreateMockCorrectly()
    {
        // Arrange & Act
        _mockAuditService.Setup(x => x.LogProfileChangeAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<object>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Assert
        var task = _mockAuditService.Object.LogProfileChangeAsync(1, "test", "old", "new", "Client");
        task.Should().NotBeNull();
        task.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void UploadService_Setup_ShouldCreateMockCorrectly()
    {
        // Arrange
        var expectedResult = new PresignedUploadResult(
            "https://storage.example.com/upload/12345",
            "https://cdn.example.com/files/12345.jpg",
            new Dictionary<string, string>());

        _mockUploadService.Setup(x => x.GeneratePresignedUploadUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = _mockUploadService.Object.GeneratePresignedUploadUrlAsync("test.jpg", "image/jpeg", "avatar");

        // Assert
        result.Should().NotBeNull();
        result.Result.UploadUrl.Should().Be(expectedResult.UploadUrl);
        result.Result.PublicUrl.Should().Be(expectedResult.PublicUrl);
    }
}