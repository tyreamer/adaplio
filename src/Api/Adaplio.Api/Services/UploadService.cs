using System.Security.Cryptography;
using System.Text;

namespace Adaplio.Api.Services;

public class UploadService : IUploadService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UploadService> _logger;

    // For demo purposes, we'll use local file storage
    // In production, this would integrate with Azure Blob Storage, AWS S3, or similar
    private const string LocalStoragePath = "uploads";
    private const int UrlExpirationMinutes = 60;

    public UploadService(IConfiguration configuration, ILogger<UploadService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Ensure uploads directory exists
        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), LocalStoragePath);
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }
    }

    public async Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(
        string fileName,
        string contentType,
        string uploadType)
    {
        try
        {
            // Validate file type
            if (!IsValidContentType(contentType))
            {
                throw new ArgumentException($"Invalid content type: {contentType}");
            }

            // Validate upload type
            if (uploadType != "avatar" && uploadType != "logo")
            {
                throw new ArgumentException($"Invalid upload type: {uploadType}");
            }

            // Generate unique file name
            var fileExtension = GetFileExtension(contentType);
            var uniqueFileName = $"{uploadType}_{Guid.NewGuid():N}{fileExtension}";
            var uploadKey = $"{uploadType}s/{DateTime.UtcNow:yyyy/MM/dd}/{uniqueFileName}";

            // Generate signed upload URL (for demo, we'll use a simple token-based approach)
            var uploadToken = GenerateUploadToken(uploadKey, DateTime.UtcNow.AddMinutes(UrlExpirationMinutes));
            var baseUrl = GetBaseUrl();

            var uploadUrl = $"{baseUrl}/api/uploads/upload?key={Uri.EscapeDataString(uploadKey)}&token={uploadToken}";
            var publicUrl = $"{baseUrl}/api/uploads/files/{Uri.EscapeDataString(uploadKey)}";

            _logger.LogInformation("Generated presigned upload URL for {UploadType}: {UploadKey}", uploadType, uploadKey);

            return new PresignedUploadResult(
                UploadUrl: uploadUrl,
                PublicUrl: publicUrl,
                Fields: new Dictionary<string, string>
                {
                    { "Content-Type", contentType },
                    { "upload_type", uploadType },
                    { "expires_at", DateTime.UtcNow.AddMinutes(UrlExpirationMinutes).ToString("O") }
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned upload URL for {UploadType}", uploadType);
            throw;
        }
    }

    public async Task<bool> ValidateUploadedFileAsync(string publicUrl)
    {
        try
        {
            // Extract the file key from the public URL
            var uri = new Uri(publicUrl);
            var segments = uri.Segments;
            if (segments.Length < 4 || segments[^3] != "files/")
                return false;

            var uploadKey = Uri.UnescapeDataString(segments[^1]);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), LocalStoragePath, uploadKey);

            var exists = File.Exists(filePath);

            if (exists)
            {
                // Additional validation: check file size and type
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > 2 * 1024 * 1024) // 2MB limit
                {
                    _logger.LogWarning("Uploaded file {UploadKey} exceeds size limit: {Size} bytes", uploadKey, fileInfo.Length);
                    return false;
                }
            }

            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate uploaded file: {PublicUrl}", publicUrl);
            return false;
        }
    }

    public async Task<bool> DeleteFileAsync(string publicUrl)
    {
        try
        {
            // Extract the file key from the public URL
            var uri = new Uri(publicUrl);
            var segments = uri.Segments;
            if (segments.Length < 4 || segments[^3] != "files/")
                return false;

            var uploadKey = Uri.UnescapeDataString(segments[^1]);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), LocalStoragePath, uploadKey);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted file: {UploadKey}", uploadKey);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {PublicUrl}", publicUrl);
            return false;
        }
    }

    private bool IsValidContentType(string contentType)
    {
        var allowedTypes = new[]
        {
            "image/png",
            "image/jpeg",
            "image/jpg",
            "image/webp"
        };

        return allowedTypes.Contains(contentType.ToLower());
    }

    private string GetFileExtension(string contentType)
    {
        return contentType.ToLower() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
    }

    private string GenerateUploadToken(string uploadKey, DateTime expiresAt)
    {
        var secret = _configuration["Upload:Secret"] ?? "default-upload-secret-key";
        var payload = $"{uploadKey}:{expiresAt:O}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var token = Convert.ToBase64String(hash);

        return $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))}.{token}";
    }

    private bool ValidateUploadToken(string token, string uploadKey)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2)
                return false;

            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            var expectedToken = parts[1];

            var payloadParts = payload.Split(':');
            if (payloadParts.Length != 2)
                return false;

            var tokenUploadKey = payloadParts[0];
            var expiresAt = DateTime.Parse(payloadParts[1]);

            if (tokenUploadKey != uploadKey || DateTime.UtcNow > expiresAt)
                return false;

            // Verify signature
            var secret = _configuration["Upload:Secret"] ?? "default-upload-secret-key";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedToken = Convert.ToBase64String(hash);

            return computedToken == expectedToken;
        }
        catch
        {
            return false;
        }
    }

    private string GetBaseUrl()
    {
        // In production, this would come from configuration
        var scheme = _configuration["Upload:Scheme"] ?? "https";
        var host = _configuration["Upload:Host"] ?? "localhost:5000";
        return $"{scheme}://{host}";
    }

    // Method to handle the actual file upload (called by the upload endpoint)
    public async Task<bool> HandleFileUploadAsync(string uploadKey, string token, Stream fileStream, string contentType)
    {
        try
        {
            // Validate token
            if (!ValidateUploadToken(token, uploadKey))
            {
                _logger.LogWarning("Invalid upload token for key: {UploadKey}", uploadKey);
                return false;
            }

            // Validate file size
            if (fileStream.Length > 2 * 1024 * 1024) // 2MB limit
            {
                _logger.LogWarning("File too large: {Size} bytes for key: {UploadKey}", fileStream.Length, uploadKey);
                return false;
            }

            // Create directory structure
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), LocalStoragePath, uploadKey);
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            // Save file
            using var fileStreamWriter = new FileStream(filePath, FileMode.Create);
            await fileStream.CopyToAsync(fileStreamWriter);

            _logger.LogInformation("Successfully uploaded file: {UploadKey}", uploadKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle file upload for key: {UploadKey}", uploadKey);
            return false;
        }
    }
}