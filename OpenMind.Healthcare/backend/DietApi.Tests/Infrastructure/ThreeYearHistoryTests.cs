using System.Diagnostics;
using DietApi.Domain.Aggregates;
using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;
using DietApi.Infrastructure.Data;
using DietApi.Infrastructure.Data.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DietApi.Tests.Infrastructure;

/// <summary>
/// The scale scenario the aggregate split and the stored per-day totals exist to satisfy.
/// </summary>
/// <remarks>
/// Three years of daily logging is roughly 1,100 days and 4,400 food entries. If a calendar year
/// view or a statistics read over that history is slow, the two decisions this design rests on -
/// splitting <c>LoggedDay</c> out of <c>DietPlan</c>, and storing each day's totals rather than
/// deriving them - are the things to revisit, not the target.
/// <para>
/// The budget here is deliberately loose compared with the one-second product criterion, because a
/// developer machine running a test suite is not a fair proxy for a warm server. It is set to catch
/// an order-of-magnitude regression - the kind that means someone started loading entries to draw a
/// calendar - not to police milliseconds.
/// </para>
/// </remarks>
public class ThreeYearHistoryTests : IDisposable
{
    private const int Days = 1095;
    private const int BudgetMs = 2000;

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"diet-scale-{Guid.NewGuid():N}.db");
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task A_year_view_and_statistics_stay_fast_over_three_years_of_daily_logging()
    {
        await SeedThreeYearsAsync();

        using var context = NewContext();
        var dayRepository = new LoggedDayRepository(context);
        var planRepository = new DietPlanRepository(context);

        var plan = await planRepository.GetByUserIdAsync(_userId);
        plan.ShouldNotBeNull();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // A calendar year view.
        var yearWatch = Stopwatch.StartNew();
        var year = await dayRepository.GetRangeAsync(_userId, today.AddDays(-364), today);
        yearWatch.Stop();

        year.Count.ShouldBeGreaterThan(360);

        // Statistics across the whole plan.
        var statsWatch = Stopwatch.StartNew();
        var all = await dayRepository.GetRangeAsync(_userId, plan.StartDate, today);
        var stats = new StreakCalculator().Calculate(all, plan.StartDate);
        statsWatch.Stop();

        stats.TotalDaysLogged.ShouldBe(Days);
        stats.AverageDailyCalories.ShouldBeGreaterThan(0);

        yearWatch.ElapsedMilliseconds.ShouldBeLessThan(BudgetMs,
            $"a year view over {Days} days took {yearWatch.ElapsedMilliseconds}ms");
        statsWatch.ElapsedMilliseconds.ShouldBeLessThan(BudgetMs,
            $"statistics over {Days} days took {statsWatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Adding_one_entry_does_not_get_slower_as_the_history_grows()
    {
        // The point of splitting LoggedDay out of DietPlan: a write touches one day, not the lot.
        await SeedThreeYearsAsync();

        using var context = NewContext();
        var dayRepository = new LoggedDayRepository(context);
        var libraryRepository = new FoodLibraryRepository(context);

        var food = (await libraryRepository.SearchAsync("banana")).First();
        var serving = food.ServingSizes.First();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var day = await dayRepository.GetByDateAsync(_userId, today);
        day.ShouldNotBeNull();

        var watch = Stopwatch.StartNew();
        day.AddEntry(food.Id, serving.Id, food.Name, serving.Label, 1m, MealType.Snack, serving.Nutrition);
        await dayRepository.UpdateAsync(day);
        watch.Stop();

        watch.ElapsedMilliseconds.ShouldBeLessThan(BudgetMs,
            $"adding one entry against {Days} days of history took {watch.ElapsedMilliseconds}ms");
    }

    private async Task SeedThreeYearsAsync()
    {
        using var context = NewContext();
        await context.Database.MigrateAsync();
        DbInitializer.Initialize(context);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-(Days - 1));

        var plan = DietPlan.Create(
            _userId, GoalType.Maintain, startDate,
            BodyMetrics.Create(178m, 34, BiologicalSex.Male),
            ActivityLevel.ModeratelyActive,
            NutritionTargets.Create(2100), TargetSource.Suggested,
            84.6m, 78m);

        context.DietPlans.Add(plan);
        await context.SaveChangesAsync();

        var food = context.FoodLibraryItems.First(f => f.Name == "Banana");
        var serving = food.ServingSizes.First();

        // Four entries a day, the realistic shape: roughly 4,400 rows.
        for (var i = 0; i < Days; i++)
        {
            var date = startDate.AddDays(i);
            var day = LoggedDay.StartDay(plan.Id, _userId, date, plan.Targets, startDate);

            foreach (var meal in new[] { MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snack })
            {
                day.AddEntry(food.Id, serving.Id, food.Name, serving.Label, 1m, meal, serving.Nutrition);
            }

            context.LoggedDays.Add(day);
        }

        await context.SaveChangesAsync();
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
