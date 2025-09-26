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
    public DbSet<Domain.Gamification> Gamifications { get; set; }
    public DbSet<XpAward> XpAwards { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PostgreSQL type conversions
        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            // Configure auto-increment for all primary keys
            modelBuilder.Entity<AppUser>()
                .Property(u => u.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<MagicLink>()
                .Property(ml => ml.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<GrantCode>()
                .Property(gc => gc.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<ClientProfile>()
                .Property(cp => cp.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<TrainerProfile>()
                .Property(tp => tp.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<ConsentGrant>()
                .Property(cg => cg.Id)
                .UseIdentityColumn();

            // Add all other entities with Id fields
            modelBuilder.Entity<MediaAsset>()
                .Property(ma => ma.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<Transcript>()
                .Property(t => t.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<ExtractionResult>()
                .Property(er => er.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<Exercise>()
                .Property(e => e.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<PlanTemplate>()
                .Property(pt => pt.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<PlanTemplateItem>()
                .Property(pti => pti.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<PlanProposal>()
                .Property(pp => pp.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<PlanInstance>()
                .Property(pi => pi.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<ExerciseInstance>()
                .Property(ei => ei.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<PlanItemAcceptance>()
                .Property(pia => pia.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<ProgressEvent>()
                .Property(pe => pe.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<AdherenceWeek>()
                .Property(aw => aw.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<Domain.Gamification>()
                .Property(g => g.Id)
                .UseIdentityColumn();

            modelBuilder.Entity<XpAward>()
                .Property(xa => xa.Id)
                .UseIdentityColumn();

            // Boolean to integer conversion for is_verified column
            modelBuilder.Entity<AppUser>()
                .Property(u => u.IsVerified)
                .HasConversion<int>();

            // Configure all DateTimeOffset columns to use proper PostgreSQL timestamp type
            // AppUser
            modelBuilder.Entity<AppUser>()
                .Property(u => u.CreatedAt)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<AppUser>()
                .Property(u => u.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            // MagicLink
            modelBuilder.Entity<MagicLink>()
                .Property(ml => ml.ExpiresAt)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<MagicLink>()
                .Property(ml => ml.UsedAt)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<MagicLink>()
                .Property(ml => ml.CreatedAt)
                .HasColumnType("timestamp with time zone");

            // GrantCode
            modelBuilder.Entity<GrantCode>()
                .Property(gc => gc.ExpiresAt)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<GrantCode>()
                .Property(gc => gc.UsedAt)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<GrantCode>()
                .Property(gc => gc.CreatedAt)
                .HasColumnType("timestamp with time zone");

            // ClientProfile
            modelBuilder.Entity<ClientProfile>()
                .Property(cp => cp.CreatedAt)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<ClientProfile>()
                .Property(cp => cp.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            // TrainerProfile
            modelBuilder.Entity<TrainerProfile>()
                .Property(tp => tp.CreatedAt)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<TrainerProfile>()
                .Property(tp => tp.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            // ConsentGrant
            modelBuilder.Entity<ConsentGrant>()
                .Property(cg => cg.CreatedAt)
                .HasColumnType("timestamp with time zone");
            modelBuilder.Entity<ConsentGrant>()
                .Property(cg => cg.RevokedAt)
                .HasColumnType("timestamp with time zone");
        }

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

        modelBuilder.Entity<XpAward>()
            .HasIndex(xa => xa.ProgressEventId)
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

            modelBuilder.Entity<Domain.Gamification>()
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
            try
            {
                // Check if the entity has an UpdatedAt property
                var updatedAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProperty != null)
                {
                    updatedAtProperty.CurrentValue = DateTimeOffset.UtcNow;
                }
            }
            catch
            {
                // Skip if property doesn't exist - some entities might not have UpdatedAt
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