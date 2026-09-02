using DietApi.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Tests.Infrastructure;

/// <summary>
/// Restarting the service must never duplicate curated reference data.
/// </summary>
/// <remarks>
/// This is the one place a real database earns its keep. Containers restart, and a seed that is
/// not idempotent corrupts the catalogue on the second boot - which is invisible until a member
/// searches for "banana" and gets it twice. Handler tests still use in-memory fakes; the guard
/// being proven here is a persistence concern, so it needs persistence.
/// </remarks>
public class SeedIdempotencyTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"diet-seed-{Guid.NewGuid():N}.db");

    [Fact]
    public void Seeding_twice_leaves_exactly_one_copy_of_every_curated_item()
    {
        int foods, achievements, tips;

        // First boot: creates the schema and seeds it.
        using (var context = NewContext())
        {
            context.Database.Migrate();
            DbInitializer.Initialize(context);

            foods = context.FoodLibraryItems.Count();
            achievements = context.DietAchievements.Count();
            tips = context.EatingTips.Count();
        }

        foods.ShouldBeGreaterThan(150);
        achievements.ShouldBe(8);
        tips.ShouldBeGreaterThan(0);

        // Second boot against the same file: migrations re-apply, seeds must not.
        using (var context = NewContext())
        {
            context.Database.Migrate();
            DbInitializer.Initialize(context);

            context.FoodLibraryItems.Count().ShouldBe(foods);
            context.DietAchievements.Count().ShouldBe(achievements);
            context.EatingTips.Count().ShouldBe(tips);
        }

        // And a third, in case the guard only holds once.
        using (var context = NewContext())
        {
            DbInitializer.Initialize(context);
            context.FoodLibraryItems.Count().ShouldBe(foods);
        }
    }

    [Fact]
    public void An_empty_database_produces_a_working_schema_with_searchable_food()
    {
        using var context = NewContext();
        context.Database.Migrate();
        DbInitializer.Initialize(context);

        var oats = context.FoodLibraryItems.FirstOrDefault(f => f.SearchName.Contains("porridge"));

        oats.ShouldNotBeNull();
        oats.ServingSizes.ShouldNotBeEmpty();
        oats.ServingSizes.First().Nutrition.Calories.ShouldBeGreaterThan(0);
    }

    private DietDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<DietDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        return new DietDbContext(options, new NoOpMediator());
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    /// <summary>Seeding raises no domain events, so publishing is a no-op here.</summary>
    private sealed class NoOpMediator : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            Task.FromResult<TResponse>(default!);

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            Task.FromResult<object?>(null);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
