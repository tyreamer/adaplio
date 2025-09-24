using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Adaplio.Api.Domain;

[Table("exercise")]
public class Exercise
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("category")]
    [MaxLength(100)]
    public string? Category { get; set; } // e.g. "strength", "mobility", "balance"

    [Column("default_sets")]
    public int? DefaultSets { get; set; }

    [Column("default_reps")]
    public int? DefaultReps { get; set; }

    [Column("default_hold_seconds")]
    public int? DefaultHoldSeconds { get; set; }

    [Column("instructions")]
    public string? Instructions { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public ICollection<PlanTemplateItem> PlanTemplateItems { get; set; } = [];
    public ICollection<ExerciseInstance> ExerciseInstances { get; set; } = [];
}