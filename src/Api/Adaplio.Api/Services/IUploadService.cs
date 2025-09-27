namespace Adaplio.Api.Services;

public interface IUploadService
{
    Task<PresignedUploadResult> GeneratePresignedUploadUrlAsync(string fileName, string contentType, string uploadType);
    Task<bool> ValidateUploadedFileAsync(string publicUrl);
    Task<bool> DeleteFileAsync(string publicUrl);
}

public record PresignedUploadResult(
    string UploadUrl,
    string PublicUrl,
    Dictionary<string, string> Fields
);