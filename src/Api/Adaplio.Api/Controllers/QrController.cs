using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace Adaplio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QrController : ControllerBase
{
    /// <summary>
    /// Generate QR code image for invite token
    /// </summary>
    [HttpGet("{token}")]
    public IActionResult GenerateQrCode(string token)
    {
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var inviteUrl = $"{baseUrl}/join?invite={token}";

            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(inviteUrl, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            return File(qrCodeBytes, "image/png");
        }
        catch (Exception)
        {
            return BadRequest("Failed to generate QR code");
        }
    }

    /// <summary>
    /// Generate QR code as base64 data URL
    /// </summary>
    [HttpGet("{token}/data")]
    public IActionResult GenerateQrCodeData(string token)
    {
        try
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var inviteUrl = $"{baseUrl}/join?invite={token}";

            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(inviteUrl, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            var base64 = Convert.ToBase64String(qrCodeBytes);
            var dataUrl = $"data:image/png;base64,{base64}";

            return Ok(new { qrCodeDataUrl = dataUrl });
        }
        catch (Exception)
        {
            return BadRequest("Failed to generate QR code");
        }
    }
}