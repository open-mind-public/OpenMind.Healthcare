using DDD.BuildingBlocks;
using DietApi.Domain.Aggregates;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Infrastructure.Data;

/// <summary>
/// The diet bounded context's own store. It owns its database file, its migration history, and
/// its volume, and reads no other context's tables - the only identifier crossing the boundary
/// is the member's <c>UserId</c>, taken from the authenticated token.
/// </summary>
public class DietDbContext(DbContextOptions<DietDbContext> options, IMediator mediator) : DbContext(options)
{
    public DbSet<DietPlan> DietPlans { get; set; }
    public DbSet<LoggedDay> LoggedDays { get; set; }
    public DbSet<ExerciseDay> ExerciseDays { get; set; }
    public DbSet<FoodLibraryItem> FoodLibraryItems { get; set; }
    public DbSet<ActivityType> ActivityTypes { get; set; }
    public DbSet<DietAchievement> DietAchievements { get; set; }
    public DbSet<EatingTip> EatingTips { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entities = ChangeTracker.Entries<Entity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        foreach (var entity in entities)
        {
            entity.ClearDomainEvents();
        }

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DietPlan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();

            // One active plan per member
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.Property(e => e.Goal).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ActivityLevel).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.TargetSource).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.TargetWeightKg).HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.OwnsOne(e => e.BodyMetrics, metrics =>
            {
                metrics.Property(m => m.HeightCm).HasColumnName("HeightCm").HasPrecision(5, 1);
                metrics.Property(m => m.Age).HasColumnName("Age");
                metrics.Property(m => m.Sex).HasColumnName("Sex").HasConversion<string>().HasMaxLength(20);
            });

            entity.OwnsOne(e => e.Targets, targets =>
            {
                // Calories are int everywhere: EF Core maps decimal to SQLite TEXT, which cannot
                // be averaged numerically, and the average-intake statistic needs exactly that.
                targets.Property(t => t.Calories).HasColumnName("TargetCalories");
                targets.Property(t => t.ProteinG).HasColumnName("TargetProteinG").HasPrecision(6, 1);
                targets.Property(t => t.CarbsG).HasColumnName("TargetCarbsG").HasPrecision(6, 1);
                targets.Property(t => t.FatG).HasColumnName("TargetFatG").HasPrecision(6, 1);
            });

            entity.OwnsMany(e => e.WeightReadings, readings =>
            {
                readings.ToTable("WeightReadings");
                readings.WithOwner().HasForeignKey(r => r.DietPlanId);
                readings.HasKey(r => r.Id);
                readings.Property(r => r.Id).ValueGeneratedNever();
                readings.Property(r => r.Date).IsRequired();
                readings.Property(r => r.WeightKg).IsRequired().HasPrecision(5, 2);
                readings.Property(r => r.RecordedAt).IsRequired();

                // One reading per calendar day per plan
                readings.HasIndex(r => new { r.DietPlanId, r.Date }).IsUnique();

                readings.Ignore(r => r.DomainEvents);
            });

            entity.OwnsMany(e => e.UnlockedAchievements, unlocked =>
            {
                unlocked.ToTable("UnlockedAchievements");
                unlocked.WithOwner().HasForeignKey(u => u.DietPlanId);
                unlocked.HasKey(u => u.Id);
                unlocked.Property(u => u.Id).ValueGeneratedNever();
                unlocked.Property(u => u.DietAchievementId).IsRequired();
                unlocked.Property(u => u.EarnedOn).IsRequired();

                // An achievement is earned at most once per member
                unlocked.HasIndex(u => new { u.DietPlanId, u.DietAchievementId }).IsUnique();

                unlocked.Ignore(u => u.DomainEvents);
            });

            entity.OwnsMany(e => e.ExerciseShortcuts, shortcuts =>
            {
                shortcuts.ToTable("ExerciseShortcuts");
                shortcuts.WithOwner().HasForeignKey(s => s.DietPlanId);
                shortcuts.HasKey(s => s.Id);
                shortcuts.Property(s => s.Id).ValueGeneratedNever();
                shortcuts.Property(s => s.ActivityTypeId).IsRequired();
                shortcuts.Property(s => s.Name).IsRequired().HasMaxLength(80);
                shortcuts.Property(s => s.DurationMinutes).IsRequired();
                shortcuts.Property(s => s.Position).IsRequired();
                shortcuts.Property(s => s.CreatedAt).IsRequired();

                // The no-duplicates rule, made true at the storage layer as well as in the domain,
                // so a race that somehow slipped past the aggregate still cannot land.
                shortcuts.HasIndex(s => new { s.DietPlanId, s.ActivityTypeId, s.DurationMinutes }).IsUnique();

                // The ordered read.
                shortcuts.HasIndex(s => new { s.DietPlanId, s.Position });

                shortcuts.Ignore(s => s.DomainEvents);
            });

            entity.Navigation(e => e.ExerciseShortcuts).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Navigation(e => e.UnlockedAchievements).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Navigation(e => e.WeightReadings).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<LoggedDay>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DietPlanId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Conflicting writes are refused, not merged. SQLite has no native row version, so
            // the aggregate reassigns this on every mutation and EF Core puts the previous value
            // in the UPDATE's WHERE clause - a zero-row result is the conflict signal.
            entity.Property(e => e.Version).IsRequired().IsConcurrencyToken();

            // One day per date per plan
            entity.HasIndex(e => new { e.DietPlanId, e.Date }).IsUnique();

            // Range reads for the calendar and statistics
            entity.HasIndex(e => new { e.UserId, e.Date });

            entity.OwnsOne(e => e.TargetSnapshot, targets =>
            {
                targets.Property(t => t.Calories).HasColumnName("TargetCalories");
                targets.Property(t => t.ProteinG).HasColumnName("TargetProteinG").HasPrecision(6, 1);
                targets.Property(t => t.CarbsG).HasColumnName("TargetCarbsG").HasPrecision(6, 1);
                targets.Property(t => t.FatG).HasColumnName("TargetFatG").HasPrecision(6, 1);
            });

            entity.OwnsOne(e => e.Totals, totals =>
            {
                totals.Property(t => t.Calories).HasColumnName("TotalCalories");
                totals.Property(t => t.ProteinG).HasColumnName("TotalProteinG").HasPrecision(7, 1);
                totals.Property(t => t.CarbsG).HasColumnName("TotalCarbsG").HasPrecision(7, 1);
                totals.Property(t => t.FatG).HasColumnName("TotalFatG").HasPrecision(7, 1);
            });

            entity.OwnsMany(e => e.Entries, entries =>
            {
                entries.ToTable("FoodEntries");
                entries.WithOwner().HasForeignKey(f => f.LoggedDayId);
                entries.HasKey(f => f.Id);
                entries.Property(f => f.Id).ValueGeneratedNever();
                entries.Property(f => f.FoodLibraryItemId).IsRequired();
                entries.Property(f => f.ServingSizeId).IsRequired();
                entries.Property(f => f.FoodName).IsRequired().HasMaxLength(200);
                entries.Property(f => f.ServingLabel).IsRequired().HasMaxLength(50);
                entries.Property(f => f.Quantity).IsRequired().HasPrecision(6, 2);
                entries.Property(f => f.MealType).HasConversion<string>().HasMaxLength(50);
                entries.Property(f => f.LoggedAt).IsRequired();

                entries.OwnsOne(f => f.Nutrition, nutrition =>
                {
                    nutrition.Property(v => v.Calories).HasColumnName("Calories");
                    nutrition.Property(v => v.ProteinG).HasColumnName("ProteinG").HasPrecision(6, 1);
                    nutrition.Property(v => v.CarbsG).HasColumnName("CarbsG").HasPrecision(6, 1);
                    nutrition.Property(v => v.FatG).HasColumnName("FatG").HasPrecision(6, 1);
                });

                entries.Ignore(f => f.DomainEvents);
            });

            entity.Navigation(e => e.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
        });

        // A sibling of LoggedDay, not a part of it. Their lifecycles differ - a logged day is
        // deleted when its last meal goes - so exercise owned there would be destroyed with the
        // dinner it had nothing to do with (research.md R-002, FR-013).
        modelBuilder.Entity<ExerciseDay>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DietPlanId).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            // Conflicting writes are refused, not merged. SQLite has no native row version, so
            // the aggregate reassigns this on every mutation and EF Core puts the previous value
            // in the UPDATE's WHERE clause - a zero-row result is the conflict signal.
            entity.Property(e => e.Version).IsRequired().IsConcurrencyToken();

            // One exercise day per date per plan
            entity.HasIndex(e => new { e.DietPlanId, e.Date }).IsUnique();

            // Range reads for the calendar and the weekly summary
            entity.HasIndex(e => new { e.UserId, e.Date });

            entity.OwnsOne(e => e.Totals, totals =>
            {
                // Both int: the weekly summary aggregates them in SQL, and EF Core maps decimal
                // to SQLite TEXT, which cannot be summed numerically (ADR 0002).
                totals.Property(t => t.Minutes).HasColumnName("TotalMinutes");
                totals.Property(t => t.Kilocalories).HasColumnName("TotalKilocalories");
            });

            entity.OwnsMany(e => e.Entries, entries =>
            {
                entries.ToTable("ExerciseEntries");
                entries.WithOwner().HasForeignKey(x => x.ExerciseDayId);
                entries.HasKey(x => x.Id);
                entries.Property(x => x.Id).ValueGeneratedNever();
                entries.Property(x => x.ActivityTypeId).IsRequired();
                entries.Property(x => x.ActivityName).IsRequired().HasMaxLength(120);

                // Snapshotted and displayed, never SQL-aggregated - so decimal is safe here.
                entries.Property(x => x.Met).IsRequired().HasPrecision(4, 1);

                entries.Property(x => x.DurationMinutes).IsRequired();
                entries.Property(x => x.EstimatedKcal).IsRequired();
                entries.Property(x => x.RecordedAt).IsRequired();

                entries.Ignore(x => x.DomainEvents);
            });

            entity.Navigation(e => e.Entries).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<ActivityType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
            entity.Property(e => e.SearchName).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Met).IsRequired().HasPrecision(4, 1);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.SearchName);

            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<DietAchievement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(10);
            entity.Property(e => e.Criterion).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Threshold).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<EatingTip>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(10);
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Ignore(e => e.DomainEvents);
        });

        modelBuilder.Entity<FoodLibraryItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SearchName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.SearchName);

            entity.OwnsMany(e => e.ServingSizes, servings =>
            {
                servings.ToTable("ServingSizes");
                servings.WithOwner().HasForeignKey(s => s.FoodLibraryItemId);
                servings.HasKey(s => s.Id);
                servings.Property(s => s.Id).ValueGeneratedNever();
                servings.Property(s => s.Label).IsRequired().HasMaxLength(50);
                servings.Property(s => s.GramWeight).IsRequired().HasPrecision(7, 2);

                servings.OwnsOne(s => s.Nutrition, nutrition =>
                {
                    nutrition.Property(v => v.Calories).HasColumnName("Calories");
                    nutrition.Property(v => v.ProteinG).HasColumnName("ProteinG").HasPrecision(6, 1);
                    nutrition.Property(v => v.CarbsG).HasColumnName("CarbsG").HasPrecision(6, 1);
                    nutrition.Property(v => v.FatG).HasColumnName("FatG").HasPrecision(6, 1);
                });

                servings.Ignore(s => s.DomainEvents);
            });

            entity.Navigation(e => e.ServingSizes).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
        });
    }
}
