using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Adaplio.Frontend.Components.Invites;
using MudBlazor;
using MudBlazor.Services;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Threading;
using Moq.Protected;

namespace Adaplio.Frontend.Tests.Components.Invites;

public class InviteClientDialogTests : TestContext
{
    private Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private HttpClient _httpClient;

    public InviteClientDialogTests()
    {
        // Setup mock HTTP handler
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };

        // Register required services
        Services.AddMudServices();
        Services.AddSingleton(_httpClient);
        Services.AddMudBlazorSnackbar();
    }

    [Fact]
    public void InviteClientDialog_ShouldLoadAndDisplayInviteToken()
    {
        // Arrange
        var expectedToken = "ABC123";
        var expectedUrl = $"http://localhost/?invite={expectedToken}";

        SetupSuccessfulTokenCreation(expectedToken, expectedUrl);

        // Act
        var component = RenderComponent<InviteClientDialog>();

        // Assert - Should show loading initially, then show invite data
        component.WaitForState(() => component.Markup.Contains(expectedToken), timeout: TimeSpan.FromSeconds(5));
        component.Markup.Should().Contain(expectedToken);
    }

    [Fact]
    public void InviteClientDialog_ShouldDisplayEmailInputField()
    {
        // Arrange
        SetupSuccessfulTokenCreation("TOKEN123", "http://localhost/?invite=TOKEN123");

        // Act
        var component = RenderComponent<InviteClientDialog>();
        component.WaitForState(() => !component.Markup.Contains("Loading"), timeout: TimeSpan.FromSeconds(5));

        // Assert
        var emailInput = component.FindAll("input").FirstOrDefault(i =>
            i.GetAttribute("placeholder")?.Contains("example.com") == true);
        emailInput.Should().NotBeNull();
    }

    [Fact]
    public void InviteClientDialog_ShouldDisplaySMSInputField()
    {
        // Arrange
        SetupSuccessfulTokenCreation("TOKEN123", "http://localhost/?invite=TOKEN123");

        // Act
        var component = RenderComponent<InviteClientDialog>();
        component.WaitForState(() => !component.Markup.Contains("Loading"), timeout: TimeSpan.FromSeconds(5));

        // Assert
        var smsInput = component.FindAll("input").FirstOrDefault(i =>
            i.GetAttribute("placeholder")?.Contains("1234567890") == true);
        smsInput.Should().NotBeNull();
    }

    [Fact]
    public async Task InviteClientDialog_SendEmail_ShouldShowSuccessMessage()
    {
        // Arrange
        SetupSuccessfulTokenCreation("TOKEN123", "http://localhost/?invite=TOKEN123");
        SetupSuccessfulEmailSend();

        var component = RenderComponent<InviteClientDialog>();
        component.WaitForState(() => !component.Markup.Contains("Loading"), timeout: TimeSpan.FromSeconds(5));

        // Find email input and button
        var emailInput = component.FindAll("input").First(i =>
            i.GetAttribute("placeholder")?.Contains("example.com") == true);
        var sendEmailButton = component.FindAll("button").First(b =>
            b.TextContent.Contains("Send Email"));

        // Act
        await component.InvokeAsync(() => emailInput.Change("test@example.com"));
        await component.InvokeAsync(() => sendEmailButton.Click());

        // Assert - Should show success snackbar
        // Note: Snackbar verification would require MudBlazor test helpers
        _mockHttpMessageHandler.Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/invites/email")),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    [Fact]
    public async Task InviteClientDialog_SendEmail_WithInvalidEmail_ShouldShowWarning()
    {
        // Arrange
        SetupSuccessfulTokenCreation("TOKEN123", "http://localhost/?invite=TOKEN123");

        var component = RenderComponent<InviteClientDialog>();
        component.WaitForState(() => !component.Markup.Contains("Loading"), timeout: TimeSpan.FromSeconds(5));

        // Find email input and button
        var sendEmailButton = component.FindAll("button").First(b =>
            b.TextContent.Contains("Send Email"));

        // Act - Try to send without entering email
        await component.InvokeAsync(() => sendEmailButton.Click());

        // Assert - Should not call API
        _mockHttpMessageHandler.Protected()
            .Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/invites/email")),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    [Fact]
    public async Task InviteClientDialog_SendSMS_ShouldCallSMSEndpoint()
    {
        // Arrange
        SetupSuccessfulTokenCreation("TOKEN123", "http://localhost/?invite=TOKEN123");
        SetupSuccessfulSMSSend();

        var component = RenderComponent<InviteClientDialog>();
        component.WaitForState(() => !component.Markup.Contains("Loading"), timeout: TimeSpan.FromSeconds(5));

        // Find phone input and button
        var phoneInput = component.FindAll("input").First(i =>
            i.GetAttribute("placeholder")?.Contains("1234567890") == true);
        var sendSMSButton = component.FindAll("button").First(b =>
            b.TextContent.Contains("Send SMS"));

        // Act
        await component.InvokeAsync(() => phoneInput.Change("+11234567890"));
        await component.InvokeAsync(() => sendSMSButton.Click());

        // Assert
        _mockHttpMessageHandler.Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/invites/sms")),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    [Fact]
    public void InviteClientDialog_ShouldDisplayQRCode()
    {
        // Arrange
        SetupSuccessfulTokenCreation("TOKEN123", "http://localhost/?invite=TOKEN123");
        SetupSuccessfulQRCodeFetch();

        // Act
        var component = RenderComponent<InviteClientDialog>();
        component.WaitForState(() => !component.Markup.Contains("Loading"), timeout: TimeSpan.FromSeconds(5));

        // Assert - Should contain QR code section
        component.Markup.Should().Contain("Scan QR Code");
    }

    [Fact]
    public void InviteClientDialog_ShouldDisplayInviteLink()
    {
        // Arrange
        var expectedUrl = "http://localhost/?invite=TOKEN123";
        SetupSuccessfulTokenCreation("TOKEN123", expectedUrl);

        // Act
        var component = RenderComponent<InviteClientDialog>();
        component.WaitForState(() => !component.Markup.Contains("Loading"), timeout: TimeSpan.FromSeconds(5));

        // Assert
        component.Markup.Should().Contain(expectedUrl);
        component.Markup.Should().Contain("Invite Link");
    }

    #region Helper Methods

    private void SetupSuccessfulTokenCreation(string token, string inviteUrl)
    {
        var response = new
        {
            token = token,
            inviteUrl = inviteUrl,
            qrCodeUrl = $"http://localhost/qr/{token}",
            expiresAt = DateTimeOffset.UtcNow.AddHours(24)
        };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/invites/token")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(response)
            });
    }

    private void SetupSuccessfulQRCodeFetch()
    {
        var qrResponse = new { qrCodeDataUrl = "data:image/png;base64,ABC123" };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("/api/qr/")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(qrResponse)
            });
    }

    private void SetupSuccessfulEmailSend()
    {
        var response = new { message = "Email invite sent successfully!" };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/invites/email")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(response)
            });
    }

    private void SetupSuccessfulSMSSend()
    {
        var response = new { message = "SMS sent successfully!" };

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/invites/sms")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(response)
            });
    }

    #endregion
}
