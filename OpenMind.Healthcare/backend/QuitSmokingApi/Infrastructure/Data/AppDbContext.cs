using DDD.BuildingBlocks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuitSmokingApi.Domain.Aggregates;
using QuitSmokingApi.Domain.Entities;

namespace QuitSmokingApi.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator) : DbContext(options)
{
    public DbSet<QuitJourney> QuitJourneys { get; set; }
    public DbSet<Achievement> Achievements { get; set; }
    public DbSet<HealthMilestone> HealthMilestones { get; set; }
    public DbSet<MotivationalQuote> MotivationalQuotes { get; set; }
    public DbSet<CravingTip> CravingTips { get; set; }

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
        
        // Configure QuitJourney aggregate
        modelBuilder.Entity<QuitJourney>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired();
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.QuitDate).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            // Configure SmokingHabits value object as owned entity
            entity.OwnsOne(e => e.SmokingHabits, habits =>
            {
                habits.Property(h => h.CigarettesPerDay).HasColumnName("CigarettesPerDay");
                habits.Property(h => h.CigarettesPerPack).HasColumnName("CigarettesPerPack");
                
                // Configure Money value object within SmokingHabits
                habits.OwnsOne(h => h.PricePerPack, price =>
                {
                    price.Property(p => p.Amount).HasColumnName("PricePerPack").HasPrecision(18, 2);
                    price.Property(p => p.Currency).HasColumnName("Currency").HasMaxLength(3).HasDefaultValue("USD");
                });
            });

            // Smoked ("failed") days are part of the QuitJourney aggregate: owned so that they are
            // always loaded with the journey and deleted when removed from the collection.
            entity.OwnsMany(e => e.SmokedDays, days =>
            {
                days.ToTable("SmokedDays");
                days.WithOwner().HasForeignKey(d => d.QuitJourneyId);
                days.HasKey(d => d.Id);
                days.Property(d => d.Id).ValueGeneratedNever();
                days.Property(d => d.Date).IsRequired();
                days.Property(d => d.CigarettesSmoked).IsRequired();
                days.Property(d => d.Trigger).HasConversion<string>().HasMaxLength(50);
                days.Property(d => d.Note).HasMaxLength(SmokedDay.MaxNoteLength);
                days.Property(d => d.RecordedAt).IsRequired();

                // One record per calendar day per journey
                days.HasIndex(d => new { d.QuitJourneyId, d.Date }).IsUnique();

                days.Ignore(d => d.DomainEvents);
            });

            entity.Navigation(e => e.SmokedDays).UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.Ignore(e => e.DomainEvents);
        });
        
        // Configure Achievement aggregate root
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Icon).HasMaxLength(10);
            entity.Property(e => e.RequiredDays).IsRequired();
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Ignore(e => e.DomainEvents);
        });
        
        modelBuilder.Entity<HealthMilestone>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.TimeRequiredMinutes).IsRequired();
            entity.Property(e => e.TimeDisplay).HasMaxLength(50);
            entity.Property(e => e.Icon).HasMaxLength(10);
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Ignore(e => e.DomainEvents);
        });
        
        modelBuilder.Entity<MotivationalQuote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quote).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Author).HasMaxLength(100);
            entity.Property(e => e.Category).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Ignore(e => e.DomainEvents);
        });
        
        // Configure CravingTip aggregate root
        modelBuilder.Entity<CravingTip>(entity =>
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
    }
}
