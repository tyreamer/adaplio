using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("transcript")]
public class Transcript
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("media_asset_id")]
    public int MediaAssetId { get; set; }

    [Column("text_content")]
    public string TextContent { get; set; } = string.Empty;

    [Column("language")]
    [MaxLength(10)]
    public string? Language { get; set; }

    [Column("confidence_score")]
    public decimal? ConfidenceScore { get; set; }

    [Column("processing_time_ms")]
    public int? ProcessingTimeMs { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("segments_json")]
    public string? SegmentsJson { get; set; } // JSON array of timestamped segments

    // Navigation properties
    [ForeignKey(nameof(MediaAssetId))]
    public MediaAsset MediaAsset { get; set; } = null!;
}