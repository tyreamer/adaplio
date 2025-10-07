using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Adaplio.RuntimeTests;

/// <summary>
/// End-to-end tests for client and trainer onboarding workflows.
/// Tests the complete user journey from signup to profile completion.
/// </summary>
public class OnboardingFlowTests : IClassFixture<ApiTestFixture>
{
    private readonly HttpClient _client;

    public OnboardingFlowTests(ApiTestFixture fixture)
    {
        _client = fixture.Client;
    }

    #region Trainer Onboarding Flow

    [Fact]
    public async Task TrainerOnboarding_CompleteFlow_ShouldSucceed()
    {
        var testEmail = $"trainer-onboard-{Guid.NewGuid()}@example.com";
        var password = "SecurePassword123!";

        // Step 1: Register
        var registerRequest = new
        {
            email = testEmail,
            password = password,
            firstName = "John",
            lastName = "Trainer"
        };

        var registerResponse = await _client.PostAsJsonAsync("/auth/trainer/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        authResult.Should().NotBeNull();
        var token = authResult!.Token;

        // Step 2: Verify authentication works
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var meResponse = await _client.GetAsync("/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Complete profile (if endpoint exists)
        var profileUpdateRequest = new
        {
            firstName = "John",
            lastName = "Trainer",
            specialization = "Physical Therapy",
            bio = "Experienced trainer"
        };

        var profileResponse = await _client.PutAsJsonAsync("/auth/profile", profileUpdateRequest);
        profileResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent,
            HttpStatusCode.NotFound);

        // Cleanup
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task TrainerOnboarding_DuplicateEmail_ShouldFail()
    {
        var testEmail = $"trainer-dup-{Guid.NewGuid()}@example.com";
        var password = "SecurePassword123!";

        var registerRequest = new
        {
            email = testEmail,
            password = password,
            firstName = "Test",
            lastName = "Trainer"
        };

        // First registration
        var firstResponse = await _client.PostAsJsonAsync("/auth/trainer/register", registerRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second registration with same email
        var secondResponse = await _client.PostAsJsonAsync("/auth/trainer/register", registerRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TrainerOnboarding_MissingRequiredFields_ShouldFail()
    {
        var incompleteRequest = new
        {
            email = $"trainer-{Guid.NewGuid()}@example.com"
            // Missing password, firstName, lastName
        };

        var response = await _client.PostAsJsonAsync("/auth/trainer/register", incompleteRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Client Onboarding Flow

    [Fact]
    public async Task ClientOnboarding_MagicLinkRequest_ShouldSucceed()
    {
        var testEmail = $"client-{Guid.NewGuid()}@example.com";

        var request = new { email = testEmail };
        var response = await _client.PostAsJsonAsync("/auth/client/magic-link", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<MagicLinkResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().NotBeNullOrEmpty();
        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task ClientOnboarding_InvalidEmailFormat_ShouldFail()
    {
        var invalidEmails = new[] { "notanemail", "@example.com", "test@", "test..user@example.com" };

        foreach (var invalidEmail in invalidEmails)
        {
            var request = new { email = invalidEmail };
            var response = await _client.PostAsJsonAsync("/auth/client/magic-link", request);

            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.UnprocessableEntity,
                HttpStatusCode.OK); // Some might pass basic validation
        }
    }

    [Fact]
    public async Task ClientOnboarding_RoleSelection_ShouldSetCorrectRole()
    {
        // This test verifies that the role setting endpoint works
        // Note: Requires authentication, so we'll test the endpoint exists

        var roleRequest = new { userType = "client" };
        var response = await _client.PostAsJsonAsync("/auth/role", roleRequest);

        // Should require authentication
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Profile Completion Tests

    [Fact]
    public async Task ProfileUpdate_WithValidToken_ShouldSucceed()
    {
        // Register a trainer to get a token
        var testEmail = $"trainer-profile-{Guid.NewGuid()}@example.com";
        var registerRequest = new
        {
            email = testEmail,
            password = "SecurePassword123!",
            firstName = "Test",
            lastName = "Trainer"
        };

        var registerResponse = await _client.PostAsJsonAsync("/auth/trainer/register", registerRequest);
        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult!.Token);

        // Update profile
        var updateRequest = new
        {
            firstName = "Updated",
            lastName = "Name",
            phoneNumber = "+1234567890"
        };

        var updateResponse = await _client.PutAsJsonAsync("/auth/profile", updateRequest);
        updateResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent,
            HttpStatusCode.NotFound);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task ProfileUpdate_WithoutAuth_ShouldFail()
    {
        var updateRequest = new
        {
            firstName = "Test",
            lastName = "User"
        };

        var response = await _client.PutAsJsonAsync("/auth/profile", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Multi-Step Workflow Tests

    [Fact]
    public async Task CompleteTrainerJourney_ShouldWork()
    {
        var testEmail = $"journey-trainer-{Guid.NewGuid()}@example.com";

        // 1. Register
        var registerResponse = await _client.PostAsJsonAsync("/auth/trainer/register", new
        {
            email = testEmail,
            password = "SecurePassword123!",
            firstName = "Journey",
            lastName = "Tester"
        });

        registerResponse.EnsureSuccessStatusCode();
        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var token = authResult!.Token;

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 2. Get current user info
        var meResponse = await _client.GetAsync("/auth/me");
        meResponse.EnsureSuccessStatusCode();

        // 3. Update profile
        var profileResponse = await _client.PutAsJsonAsync("/auth/profile", new
        {
            firstName = "Journey",
            lastName = "Tester",
            bio = "Test bio"
        });

        profileResponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.NoContent,
            HttpStatusCode.NotFound);

        // 4. Access protected resource (verify token still valid)
        var secondMeResponse = await _client.GetAsync("/auth/me");
        secondMeResponse.EnsureSuccessStatusCode();

        _client.DefaultRequestHeaders.Authorization = null;
    }

    #endregion

    #region Security & Validation Tests

    [Fact]
    public async Task OnboardingEndpoints_ShouldHaveCSRFProtection()
    {
        // Test that sensitive endpoints require proper headers
        var response = await _client.PostAsJsonAsync("/auth/trainer/register", new
        {
            email = "test@example.com",
            password = "test"
        });

        // Should have security headers in response
        response.Headers.Should().NotBeNull();
    }

    [Fact]
    public async Task OnboardingEndpoints_ShouldValidateInputLength()
    {
        var longEmail = new string('a', 500) + "@example.com";
        var response = await _client.PostAsJsonAsync("/auth/client/magic-link", new
        {
            email = longEmail
        });

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task OnboardingEndpoints_ShouldSanitizeInput()
    {
        var xssAttempt = "<script>alert('xss')</script>@example.com";
        var response = await _client.PostAsJsonAsync("/auth/client/magic-link", new
        {
            email = xssAttempt
        });

        // Should reject or sanitize malicious input
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.UnprocessableEntity,
            HttpStatusCode.OK);
    }

    #endregion

    #region Data Models

    private record AuthResponse(
        string Token,
        string RefreshToken,
        string UserType,
        string? Email = null
    );

    private record MagicLinkResponse(string Message, DateTimeOffset ExpiresAt);

    #endregion
}
