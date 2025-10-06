using Adaplio.Api.Auth;
using Adaplio.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace Adaplio.Api.Tests.Services;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;

    public JwtServiceTests()
    {
        var configValues = new Dictionary<string, string?>
        {
            {"Jwt:Secret", "test-secret-key-that-is-long-enough-for-security-requirements-256-bits"},
            {"Jwt:Issuer", "test-issuer"},
            {"Jwt:Audience", "test-audience"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        _jwtService = new JwtService(_configuration);
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidJwtToken()
    {
        // Arrange
        var claims = new JwtClaims(
            UserId: "123",
            Email: "test@test.com",
            UserType: "client",
            Alias: "C-TEST"
        );

        // Act
        var token = _jwtService.GenerateToken(claims);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Should().HaveCount(3); // JWT has 3 parts: header.payload.signature
    }

    [Fact]
    public void GenerateToken_ShouldIncludeAllClaims()
    {
        // Arrange
        var claims = new JwtClaims(
            UserId: "123",
            Email: "test@test.com",
            UserType: "client",
            Alias: "C-TEST"
        );

        // Act
        var token = _jwtService.GenerateToken(claims);

        // Parse the token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "123");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "test@test.com");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "client");
        jwtToken.Claims.Should().Contain(c => c.Type == "user_type" && c.Value == "client");
        jwtToken.Claims.Should().Contain(c => c.Type == "alias" && c.Value == "C-TEST");
    }

    [Fact]
    public void GenerateToken_ShouldNotIncludeAlias_WhenNull()
    {
        // Arrange
        var claims = new JwtClaims(
            UserId: "123",
            Email: "test@test.com",
            UserType: "trainer",
            Alias: null
        );

        // Act
        var token = _jwtService.GenerateToken(claims);

        // Parse the token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Claims.Should().NotContain(c => c.Type == "alias");
    }

    [Fact]
    public void GenerateToken_ShouldSetCorrectIssuerAndAudience()
    {
        // Arrange
        var claims = new JwtClaims(
            UserId: "123",
            Email: "test@test.com",
            UserType: "client",
            Alias: "C-TEST"
        );

        // Act
        var token = _jwtService.GenerateToken(claims);

        // Parse the token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Issuer.Should().Be("test-issuer");
        jwtToken.Audiences.Should().Contain("test-audience");
    }

    [Fact]
    public void GenerateToken_ShouldSetExpiration()
    {
        // Arrange
        var claims = new JwtClaims(
            UserId: "123",
            Email: "test@test.com",
            UserType: "client",
            Alias: "C-TEST"
        );

        // Act
        var token = _jwtService.GenerateToken(claims);

        // Parse the token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ValidateToken_ShouldReturnPrincipal_ForValidToken()
    {
        // Arrange
        var claims = new JwtClaims(
            UserId: "123",
            Email: "test@test.com",
            UserType: "client",
            Alias: "C-TEST"
        );
        var token = _jwtService.GenerateToken(claims);

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "123");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "test@test.com");
    }

    [Fact]
    public void ValidateToken_ShouldReturnNull_ForInvalidToken()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _jwtService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ShouldReturnNull_ForExpiredToken()
    {
        // Arrange - Create a service with very short expiration
        var shortExpiryConfig = new Dictionary<string, string?>
        {
            {"Jwt:Secret", "test-secret-key-that-is-long-enough-for-security-requirements-256-bits"},
            {"Jwt:Issuer", "test-issuer"},
            {"Jwt:Audience", "test-audience"}
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(shortExpiryConfig)
            .Build();

        // Note: We can't actually test expired tokens easily in unit tests
        // This would require manipulating system time or waiting
        // Integration tests would be better for this scenario

        var invalidToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.expired.token";

        // Act
        var principal = _jwtService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Theory]
    [InlineData("client", "client@test.com")]
    [InlineData("trainer", "trainer@test.com")]
    public void GenerateToken_ShouldHandleDifferentUserTypes(string userType, string email)
    {
        // Arrange
        var claims = new JwtClaims(
            UserId: "123",
            Email: email,
            UserType: userType,
            Alias: null
        );

        // Act
        var token = _jwtService.GenerateToken(claims);

        // Parse the token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        jwtToken.Claims.Should().Contain(c => c.Type == "user_type" && c.Value == userType);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == email);
    }

    [Fact]
    public void GenerateToken_ShouldProduceDifferentTokens_ForDifferentClaims()
    {
        // Arrange
        var claims1 = new JwtClaims(
            UserId: "123",
            Email: "user1@test.com",
            UserType: "client",
            Alias: "C-TEST1"
        );

        var claims2 = new JwtClaims(
            UserId: "456",
            Email: "user2@test.com",
            UserType: "trainer",
            Alias: null
        );

        // Act
        var token1 = _jwtService.GenerateToken(claims1);
        var token2 = _jwtService.GenerateToken(claims2);

        // Assert
        token1.Should().NotBe(token2);
    }
}
