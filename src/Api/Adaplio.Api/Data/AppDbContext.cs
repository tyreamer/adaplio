using Adaplio.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Adaplio.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<ClientProfile> ClientProfiles { get; set; }
    public DbSet<TrainerProfile> TrainerProfiles { get; set; }
    public DbSet<ConsentGrant> ConsentGrants { get; set; }
    public DbSet<MagicLink> MagicLinks { get; set; }
    public DbSet<GrantCode> GrantCodes { get; set; }
    public DbSet<MediaAsset> MediaAssets { get; set; }
    public DbSet<Transcript> Transcripts { get; set; }
    public DbSet<ExtractionResult> ExtractionResults { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<PlanTemplate> PlanTemplates { get; set; }
    public DbSet<PlanTemplateItem> PlanTemplateItems { get; set; }
    public DbSet<PlanProposal> PlanProposals { get; set; }
    public DbSet<PlanInstance> PlanInstances { get; set; }
    public DbSet<ExerciseInstance> ExerciseInstances { get; set; }
    public DbSet<PlanItemAcceptance> PlanItemAcceptances { get; set; }
    public DbSet<ProgressEvent> ProgressEvents { get; set; }
    public DbSet<AdherenceWeek> AdherenceWeeks { get; set; }
    public DbSet<Gamification> Gamifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure unique constraints
        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<ClientProfile>()
            .HasIndex(c => c.Alias)
            .IsUnique()
            .HasFilter("[alias] IS NOT NULL");

        modelBuilder.Entity<ClientProfile>()
            .HasIndex(c => c.UserId)
            .IsUnique();

        modelBuilder.Entity<TrainerProfile>()
            .HasIndex(t => t.UserId)
            .IsUnique();

        modelBuilder.Entity<Gamification>()
            .HasIndex(g => g.ClientProfileId)
            .IsUnique();

        // Configure composite unique constraints
        modelBuilder.Entity<ConsentGrant>()
            .HasIndex(cg => new { cg.ClientProfileId, cg.TrainerProfileId, cg.Scope })
            .IsUnique()
            .HasFilter("[revoked_at] IS NULL");

        modelBuilder.Entity<AdherenceWeek>()
            .HasIndex(aw => new { aw.ClientProfileId, aw.Year, aw.WeekNumber })
            .IsUnique();

        modelBuilder.Entity<MagicLink>()
            .HasIndex(ml => ml.Code)
            .IsUnique();

        modelBuilder.Entity<MagicLink>()
            .HasIndex(ml => new { ml.Email, ml.CreatedAt });

        modelBuilder.Entity<GrantCode>()
            .HasIndex(gc => gc.Code)
            .IsUnique();

        modelBuilder.Entity<GrantCode>()
            .HasIndex(gc => new { gc.TrainerProfileId, gc.CreatedAt });

        // Configure decimal precision
        modelBuilder.Entity<Transcript>()
            .Property(t => t.ConfidenceScore)
            .HasPrecision(5, 4);

        modelBuilder.Entity<ExtractionResult>()
            .Property(er => er.ConfidenceScore)
            .HasPrecision(5, 4);

        modelBuilder.Entity<AdherenceWeek>()
            .Property(aw => aw.AdherencePercentage)
            .HasPrecision(5, 2);

        modelBuilder.Entity<AdherenceWeek>()
            .Property(aw => aw.AverageDifficultyRating)
            .HasPrecision(3, 1);

        modelBuilder.Entity<AdherenceWeek>()
            .Property(aw => aw.AveragePainLevel)
            .HasPrecision(3, 1);

        // Configure cascade delete behaviors to prevent cycles
        modelBuilder.Entity<ClientProfile>()
            .HasMany(c => c.ConsentGrants)
            .WithOne(cg => cg.ClientProfile)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TrainerProfile>()
            .HasMany(t => t.ConsentGrants)
            .WithOne(cg => cg.TrainerProfile)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlanTemplate>()
            .HasMany(pt => pt.PlanProposals)
            .WithOne(pp => pp.PlanTemplate)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MediaAsset>()
            .HasOne(ma => ma.Transcript)
            .WithOne(t => t.MediaAsset)
            .HasForeignKey<Transcript>(t => t.MediaAssetId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure DateOnly conversion for SQLite (PostgreSQL handles DateOnly natively)
        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            modelBuilder.Entity<PlanInstance>()
                .Property(pi => pi.StartDate)
                .HasConversion<DateOnlyConverter>();

            modelBuilder.Entity<PlanInstance>()
                .Property(pi => pi.PlannedEndDate)
                .HasConversion<DateOnlyConverter>();

            modelBuilder.Entity<PlanInstance>()
                .Property(pi => pi.ActualEndDate)
                .HasConversion<DateOnlyConverter>();

            modelBuilder.Entity<AdherenceWeek>()
                .Property(aw => aw.WeekStartDate)
                .HasConversion<DateOnlyConverter>();

            modelBuilder.Entity<Gamification>()
                .Property(g => g.LastActivityDate)
                .HasConversion<DateOnlyConverter>();
        }
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.Property("UpdatedAt").CurrentValue != null)
            {
                entry.Property("UpdatedAt").CurrentValue = DateTimeOffset.UtcNow;
            }
        }
    }
}

// Custom converter for DateOnly to work with SQLite
public class DateOnlyConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateOnly, DateTime>
{
    public DateOnlyConverter() : base(
        dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
        dateTime => DateOnly.FromDateTime(dateTime))
    {
    }
}