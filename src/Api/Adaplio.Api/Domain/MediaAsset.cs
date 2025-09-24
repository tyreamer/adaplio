using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("media_asset")]
public class MediaAsset
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("client_profile_id")]
    public int? ClientProfileId { get; set; }

    [Column("filename")]
    [MaxLength(255)]
    public string Filename { get; set; } = string.Empty;

    [Column("content_type")]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Column("file_size")]
    public long FileSize { get; set; }

    [Column("storage_path")]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "uploaded"; // uploaded, processing, processed, failed

    [Column("uploaded_at")]
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("processed_at")]
    public DateTimeOffset? ProcessedAt { get; set; }

    [Column("metadata_json")]
    public string? MetadataJson { get; set; } // JSON blob for file metadata

    // Navigation properties
    [ForeignKey(nameof(ClientProfileId))]
    public ClientProfile? ClientProfile { get; set; }

    public Transcript? Transcript { get; set; }
    public ICollection<ExtractionResult> ExtractionResults { get; set; } = [];
}