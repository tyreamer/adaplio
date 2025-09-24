using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("extraction_result")]
public class ExtractionResult
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("media_asset_id")]
    public int MediaAssetId { get; set; }

    [Column("extraction_type")]
    [MaxLength(50)]
    public string ExtractionType { get; set; } = string.Empty; // "regex", "ner", "ml_model"

    [Column("extracted_data_json")]
    public string ExtractedDataJson { get; set; } = string.Empty; // JSON blob of extracted exercises

    [Column("confidence_score")]
    public decimal? ConfidenceScore { get; set; }

    [Column("is_confirmed")]
    public bool IsConfirmed { get; set; } = false;

    [Column("confirmed_at")]
    public DateTimeOffset? ConfirmedAt { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(MediaAssetId))]
    public MediaAsset MediaAsset { get; set; } = null!;
}