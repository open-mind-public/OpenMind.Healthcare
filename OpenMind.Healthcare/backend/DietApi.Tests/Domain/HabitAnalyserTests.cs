using DietApi.Domain.Repositories;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.Domain;

/// <summary>
/// The pure service behind the analytics "Habits" section: beer and exercise frequency, and how
/// eating on beer days compares with every other day.
/// </summary>
public class HabitAnalyserTests
{
    private static readonly DateTime Clock = DateTime.UtcNow;
    private static readonly DateOnly Today = DateOnly.FromDateTime(Clock);

    private readonly HabitAnalyser _analyser = new();
    private readonly AnalysisPeriodResolver _resolver = new();

    private DateOnly Ago(int days) => Today.AddDays(-days);

    private AnalysisPeriod Week(DateOnly planStart) => _resolver.Resolve(PeriodPreset.Week, planStart, Clock);

    private static DayIntakeRow Logged(DateOnly date, int calories, int target = 2100) =>
        new(date, calories, 100m, 200m, 70m, target, 157.5m, 210m, 70m);

    [Fact]
    public void Counts_the_beer_and_exercise_days_in_the_period()
    {
        var planStart = Ago(60);
        var period = Week(planStart); // 7 in-plan days: Ago(6)..Today

        var logged = new List<DayIntakeRow>
        {
            Logged(Ago(1), 2500), // over target, a beer day
            Logged(Ago(3), 1800), // on target, a beer day
            Logged(Ago(2), 1900), // on target
            Logged(Ago(5), 2600), // over target
        };

        var beer = new HashSet<DateOnly> { Ago(1), Ago(3) };
        var exercise = new HashSet<DateOnly> { Ago(2) };

        var result = _analyser.Analyse(period, planStart, Today, logged, beer, exercise);

        result.InPlanDays.ShouldBe(7);
        result.BeerDays.ShouldBe(2);
        result.BeerDaysPerWeek.ShouldBe(2.0m);
        result.ExerciseDays.ShouldBe(1);
        result.ExerciseDaysPerWeek.ShouldBe(1.0m);
    }

    [Fact]
    public void Splits_eating_outcomes_between_beer_days_and_every_other_day()
    {
        var planStart = Ago(60);
        var period = Week(planStart);

        var logged = new List<DayIntakeRow>
        {
            Logged(Ago(1), 2500), // beer, over
            Logged(Ago(3), 1800), // beer, on target
            Logged(Ago(2), 1900), // non-beer, on target
            Logged(Ago(5), 2600), // non-beer, over
            // Today, Ago(4), Ago(6): non-beer, not logged
        };

        var beer = new HashSet<DateOnly> { Ago(1), Ago(3) };

        var result = _analyser.Analyse(period, planStart, Today, logged, beer, new HashSet<DateOnly>());

        result.OnBeerDays.Days.ShouldBe(2);
        result.OnBeerDays.OnTargetDays.ShouldBe(1);
        result.OnBeerDays.OverTargetDays.ShouldBe(1);
        result.OnBeerDays.NotLoggedDays.ShouldBe(0);
        result.OnBeerDays.OverTargetShare.ShouldBe(0.5m);

        result.OnNonBeerDays.Days.ShouldBe(5);
        result.OnNonBeerDays.OnTargetDays.ShouldBe(1);
        result.OnNonBeerDays.OverTargetDays.ShouldBe(1);
        result.OnNonBeerDays.NotLoggedDays.ShouldBe(3);

        // The two groups partition the in-plan days.
        (result.OnBeerDays.Days + result.OnNonBeerDays.Days).ShouldBe(result.InPlanDays);
    }

    [Fact]
    public void A_period_with_no_beer_days_reports_zero_rather_than_nothing()
    {
        var planStart = Ago(60);
        var period = Week(planStart);

        var result = _analyser.Analyse(
            period, planStart, Today,
            new List<DayIntakeRow> { Logged(Ago(2), 1900) },
            new HashSet<DateOnly>(),
            new HashSet<DateOnly>());

        result.BeerDays.ShouldBe(0);
        result.BeerDaysPerWeek.ShouldBe(0m);
        result.OnBeerDays.Days.ShouldBe(0);
        result.OnBeerDays.OverTargetShare.ShouldBe(0m);
        result.OnNonBeerDays.Days.ShouldBe(7);
    }

    [Fact]
    public void A_beer_date_before_the_plan_started_is_not_counted()
    {
        var planStart = Ago(3); // Week clamps to 4 in-plan days: Ago(3)..Today
        var period = Week(planStart);

        var beer = new HashSet<DateOnly> { Ago(5), Ago(1) }; // Ago(5) predates the plan

        var result = _analyser.Analyse(
            period, planStart, Today, new List<DayIntakeRow>(), beer, new HashSet<DateOnly>());

        result.InPlanDays.ShouldBe(4);
        result.BeerDays.ShouldBe(1);
    }

    [Fact]
    public void The_per_week_rate_scales_a_sub_week_period_up()
    {
        var planStart = Ago(2); // 3 in-plan days
        var period = Week(planStart);

        var result = _analyser.Analyse(
            period, planStart, Today, new List<DayIntakeRow>(),
            new HashSet<DateOnly> { Ago(1) }, new HashSet<DateOnly>());

        result.InPlanDays.ShouldBe(3);
        result.BeerDaysPerWeek.ShouldBe(2.3m); // 1 / (3/7) = 2.33 -> 2.3
    }
}
