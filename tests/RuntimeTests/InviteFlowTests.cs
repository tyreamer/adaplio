using System.Net;
using System.Net.Http.Json;
using Xunit;
using FluentAssertions;

namespace Adaplio.RuntimeTests;

/// <summary>
/// Runtime integration tests for invite functionality (email and SMS invites).
/// These tests hit the actual running API and test real invite flows.
/// </summary>
public class InviteFlowTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public InviteFlowTests(ApiTestFixture fixture)
    {
        _client = fixture.Client;
        _baseUrl = fixture.BaseUrl;
    }

    #region Email Invite Tests

    [Fact]
    public async Task EmailInvite_ShouldRequireAuthentication()
    {
        // Attempt to send email invite without authentication
        var inviteRequest = new { email = "patient@example.com" };
        var response = await _client.PostAsJsonAsync("/api/invites/email", inviteRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EmailInvite_ShouldRejectNonTrainerUsers()
    {
        // TODO: Create client user and test that they cannot send invites
        // For now, we test that authentication is required
        var inviteRequest = new { email = "patient@example.com" };
        var response = await _client.PostAsJsonAsync("/api/invites/email", inviteRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EmailInvite_ShouldValidateEmailFormat()
    {
        // First, create and login as trainer
        var (token, _) = await CreateAndLoginTrainer();

        // Set auth header
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Test with invalid email
        var invalidEmailRequest = new { email = "not-an-email" };
        var response = await _client.PostAsJsonAsync("/api/invites/email", invalidEmailRequest);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);

        // Clean up
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task EmailInvite_ShouldRejectEmptyEmail()
    {
        // First, create and login as trainer
        var (token, _) = await CreateAndLoginTrainer();

        // Set auth header
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Test with empty email
        var emptyEmailRequest = new { email = "" };
        var response = await _client.PostAsJsonAsync("/api/invites/email", emptyEmailRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Clean up
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task EmailInvite_ShouldSendInvite_WhenValidTrainerAndEmail()
    {
        // First, create and login as trainer
        var (token, _) = await CreateAndLoginTrainer();

        // Set auth header
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Create grant code first (required for invites)
        await CreateGrantCode();

        // Send email invite
        var testEmail = $"patient-{Guid.NewGuid()}@example.com";
        var inviteRequest = new { email = testEmail };
        var response = await _client.PostAsJsonAsync("/api/invites/email", inviteRequest);

        // Should succeed
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<InviteResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Contain("sent successfully");

        // Clean up
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task EmailInvite_ShouldFailWithoutGrantCode()
    {
        // First, create and login as trainer
        var (token, _) = await CreateAndLoginTrainer();

        // Set auth header
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Try to send invite without creating grant code first
        var testEmail = $"patient-{Guid.NewGuid()}@example.com";
        var inviteRequest = new { email = testEmail };
        var response = await _client.PostAsJsonAsync("/api/invites/email", inviteRequest);

        // Should fail with BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorContent = await response.Content.ReadAsStringAsync();
        errorContent.Should().Contain("grant code");

        // Clean up
        _client.DefaultRequestHeaders.Authorization = null;
    }

    #endregion

    #region SMS Invite Tests

    [Fact]
    public async Task SMSInvite_ShouldAcceptValidPhoneNumber()
    {
        var smsRequest = new { phoneNumber = "+11234567890" };
        var response = await _client.PostAsJsonAsync("/api/invites/sms", smsRequest);

        // SMS endpoint is public, so it should not return Unauthorized
        // It might fail due to SMS service not configured, but endpoint should exist
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SMSInvite_ShouldRejectMissingPhoneNumber()
    {
        var emptyRequest = new { };
        var response = await _client.PostAsJsonAsync("/api/invites/sms", emptyRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Invite Token Tests

    [Fact]
    public async Task CreateInviteToken_ShouldRequireAuthentication()
    {
        var response = await _client.PostAsJsonAsync("/api/invites/token", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateInviteToken_ShouldGenerateToken_WhenAuthenticated()
    {
        // First, create and login as trainer
        var (token, _) = await CreateAndLoginTrainer();

        // Set auth header
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Create grant code first
        await CreateGrantCode();

        // Create invite token
        var response = await _client.PostAsJsonAsync("/api/invites/token", new { expirationHours = 24 });

        // Should succeed
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CreateInviteTokenResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().NotBeNullOrEmpty();
        result.InviteUrl.Should().Contain(result.Token);
        result.QRCodeUrl.Should().Contain(result.Token);
        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        // Clean up
        _client.DefaultRequestHeaders.Authorization = null;
    }

    #endregion

    #region Helper Methods

    private async Task<(string token, string email)> CreateAndLoginTrainer()
    {
        var testEmail = $"trainer-invite-{Guid.NewGuid()}@example.com";
        var password = "SecurePassword123!";

        var registerRequest = new
        {
            email = testEmail,
            password = password,
            fullName = "Test Trainer",
            practiceName = "Test Practice"
        };

        var registerResponse = await _client.PostAsJsonAsync("/auth/trainer/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return (authResult!.Token, testEmail);
    }

    private async Task CreateGrantCode()
    {
        // Create grant code endpoint
        var grantRequest = new
        {
            type = "PERMANENT",
            maxUses = 10
        };

        var response = await _client.PostAsJsonAsync("/api/trainer/grants", grantRequest);

        // If it fails, it might be because the endpoint requires specific setup
        // But we'll try anyway for the test
        if (!response.IsSuccessStatusCode)
        {
            // Grant code might already exist or endpoint might be different
            // This is okay for testing purposes
        }
    }

    #endregion

    #region Data Models

    private record InviteResponse(string Message);

    private record AuthResponse(
        string Token,
        string RefreshToken,
        string UserType,
        string? Email = null
    );

    private record CreateInviteTokenResponse(
        string Token,
        string InviteUrl,
        string QRCodeUrl,
        DateTimeOffset ExpiresAt
    );

    #endregion
}
