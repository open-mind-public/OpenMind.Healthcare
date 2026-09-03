using System.Diagnostics;
using DietApi.Infrastructure.Data;
using DietApi.Infrastructure.Data.Repositories;
using DietApi.Infrastructure.Data.Seeds;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Tests.Infrastructure;

/// <summary>
/// SC-003, measured rather than assumed: does the seeded catalogue answer the words people
/// actually type?
/// </summary>
/// <remarks>
/// Runs the checked-in corpus against the real seed through the real repository, because search
/// quality is a property of those two together. If it fails, the seed is what to widen - the
/// criterion is what a member needs, not what the catalogue happens to manage.
/// </remarks>
public class ActivitySearchQualityTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"diet-search-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task The_catalogue_answers_at_least_the_required_share_of_everyday_terms()
    {
        using var context = SeededContext();
        var repository = new ActivityTypeRepository(context);

        var terms = ActivitySearchCorpus.Terms();
        var misses = new List<string>();

        foreach (var term in terms)
        {
            var matches = await repository.SearchAsync(term, ActivitySearchCorpus.ResultsExamined);

            if (matches.Count == 0)
            {
                misses.Add(term);
            }
        }

        var hitRate = (decimal)(terms.Count - misses.Count) / terms.Count;

        hitRate.ShouldBeGreaterThanOrEqualTo(
            ActivitySearchCorpus.RequiredHitRate,
            $"{misses.Count} of {terms.Count} terms returned nothing in the first "
            + $"{ActivitySearchCorpus.ResultsExamined} results: {string.Join(", ", misses)}. "
            + "Widen the seed rather than relaxing this bar.");
    }

    [Fact]
    public async Task Prefix_matches_come_first_so_the_obvious_answer_is_the_first_one()
    {
        using var context = SeededContext();
        var repository = new ActivityTypeRepository(context);

        // "run" also matches nothing else here, but the ordering rule is what is being asserted:
        // someone typing a word wants entries that start with it before entries that merely
        // contain it.
        var matches = await repository.SearchAsync("running", ActivitySearchCorpus.ResultsExamined);

        matches.ShouldNotBeEmpty();
        matches[0].Name.ShouldStartWith("Running");
    }

    [Fact]
    public async Task Search_is_well_within_the_one_second_criterion()
    {
        using var context = SeededContext();
        var repository = new ActivityTypeRepository(context);

        // Warm the connection so this measures the query, not the first-use cost.
        await repository.SearchAsync("walking");

        var watch = Stopwatch.StartNew();
        foreach (var term in ActivitySearchCorpus.Terms())
        {
            await repository.SearchAsync(term);
        }
        watch.Stop();

        var perSearch = watch.ElapsedMilliseconds / (double)ActivitySearchCorpus.Terms().Count;

        perSearch.ShouldBeLessThan(1000, $"each search averaged {perSearch:F1}ms");
    }

    [Fact]
    public async Task An_activity_we_do_not_have_returns_nothing_rather_than_a_near_miss()
    {
        using var context = SeededContext();
        var repository = new ActivityTypeRepository(context);

        (await repository.SearchAsync("quidditch")).ShouldBeEmpty();
    }

    private DietDbContext SeededContext()
    {
        var context = NewContext();
        context.Database.Migrate();
        DbInitializer.Initialize(context);
        return context;
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
