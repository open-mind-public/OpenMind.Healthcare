using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.TestSupport;

/// <summary>
/// Builds the composite an observation rule is evaluated against, around a pinned clock.
/// </summary>
/// <remarks>
/// Every figure defaults to something unremarkable, so a test that wants one pattern sets one
/// thing and knows nothing else fired by accident.
/// </remarks>
public sealed class AnalyticsFiguresBuilder
{
    private static readonly DateTime Clock = new(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Clock);

    private readonly IntakeAnalyser _intake = new();
    private readonly MacronutrientAnalyser _macros = new();
    private readonly PatternAnalyser _patterns = new();

    private int _loggedDays = 20;
    private int _previousLoggedDays = 20;
    private PeriodPreset _preset = PeriodPreset.Month;

    private readonly List<DayIntakeRow> _days = [];
    private readonly List<MealIntakeRow> _meals = [];
    private readonly List<CategoryIntakeRow> _categories = [];
    private readonly List<FoodContributionRow> _foods = [];
    private readonly List<QuarterHourRow> _quarters = [];

    private int _utcOffsetMinutes;

    public static AnalyticsFiguresBuilder Figures() => new();

    public AnalyticsFiguresBuilder LoggedDays(int days)
    {
        _loggedDays = days;
        return this;
    }

    public AnalyticsFiguresBuilder PreviouslyLoggedDays(int days)
    {
        _previousLoggedDays = days;
        return this;
    }

    public AnalyticsFiguresBuilder ForPreset(PeriodPreset preset)
    {
        _preset = preset;
        return this;
    }

    /// <summary>A day, dated so its weekday is what the caller wants.</summary>
    public AnalyticsFiguresBuilder Day(
        DateOnly date, int calories, decimal protein = 100m, decimal? targetProtein = 157.5m)
    {
        _days.Add(new DayIntakeRow(date, calories, protein, 200m, 70m, 2100, targetProtein, 210m, 70m));
        return this;
    }

    public AnalyticsFiguresBuilder Meal(MealType meal, int kilocalories, int entries = 10)
    {
        _meals.Add(new MealIntakeRow(meal, kilocalories, entries));
        return this;
    }

    public AnalyticsFiguresBuilder Category(FoodCategory category, int kilocalories)
    {
        _categories.Add(new CategoryIntakeRow(category, kilocalories));
        return this;
    }

    public AnalyticsFiguresBuilder Food(string name, int kilocalories, int times = 10)
    {
        _foods.Add(new FoodContributionRow(Guid.NewGuid(), name, kilocalories, times));
        return this;
    }

    public AnalyticsFiguresBuilder LoggedAt(int hour, int quarter, int kilocalories)
    {
        _quarters.Add(new QuarterHourRow(hour, quarter, kilocalories));
        return this;
    }

    public AnalyticsFiguresBuilder InTimeZone(int utcOffsetMinutes)
    {
        _utcOffsetMinutes = utcOffsetMinutes;
        return this;
    }

    /// <summary>
    /// A period of genuinely unremarkable days, so a rule under test is the only thing that fires.
    /// </summary>
    /// <remarks>
    /// Protein defaults to sitting on its target here rather than at the builder's usual 100 g,
    /// which is a real shortfall against a 157.5 g target and would trip the protein rule.
    /// </remarks>
    public AnalyticsFiguresBuilder WithOrdinaryDays(
        int count, int caloriesEach = 2000, decimal proteinEach = 157.5m)
    {
        for (var i = 0; i < count; i++)
        {
            Day(Today.AddDays(-i), caloriesEach, protein: proteinEach);
        }

        return LoggedDays(count);
    }

    public AnalyticsFigures Build()
    {
        var resolver = new AnalysisPeriodResolver();
        var period = resolver
            .Resolve(_preset, Today.AddDays(-365), Clock)
            .WithLoggedDays(_loggedDays);

        var totalEnergy = _meals.Sum(m => m.Kilocalories);

        return new AnalyticsFigures(
            period,
            _intake.Summarise(_days, period.TotalDays),
            _intake.BreakDownByMeal(_meals),
            _intake.BreakDownByCategory(_categories),
            _intake.TopFoods(
                [.. _foods.OrderByDescending(f => f.Kilocalories)],
                totalEnergy > 0 ? totalEnergy : _days.Sum(d => d.Calories)),
            _macros.Analyse(_days),
            _patterns.ByWeekday(_days),
            _patterns.ByHour(_quarters, _utcOffsetMinutes),
            _previousLoggedDays);
    }

    /// <summary>The pinned clock's today, so tests can date days onto known weekdays.</summary>
    public static DateOnly Anchor => Today;
}
