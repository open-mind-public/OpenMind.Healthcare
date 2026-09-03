using DDD.BuildingBlocks;
using DietApi.Domain.Services;
using DietApi.Domain.ValueObjects;

namespace DietApi.Tests.Domain;

/// <summary>
/// Resolving what will actually be analysed.
/// </summary>
/// <remarks>
/// This is the only place in the feature that reads "today", so it is the only place a date bug
/// can enter. Every case here is pinned to a fixed clock.
/// </remarks>
public class AnalysisPeriodResolverTests
{
    private readonly AnalysisPeriodResolver _resolver = new();

    private static readonly DateTime Clock = new(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Clock);

    [Theory]
    [InlineData(PeriodPreset.Week, 7)]
    [InlineData(PeriodPreset.Month, 30)]
    [InlineData(PeriodPreset.Quarter, 90)]
    public void A_preset_reaches_back_its_own_number_of_days_including_today(PeriodPreset preset, int days)
    {
        var period = _resolver.Resolve(preset, Today.AddDays(-365), Clock);

        period.To.ShouldBe(Today);
        period.From.ShouldBe(Today.AddDays(-(days - 1)));
        period.TotalDays.ShouldBe(days);
        period.WasNarrowed.ShouldBeFalse();
    }

    [Fact]
    public void The_whole_plan_preset_starts_at_the_plan_start()
    {
        var planStart = Today.AddDays(-200);

        var period = _resolver.Resolve(PeriodPreset.Plan, planStart, Clock);

        period.From.ShouldBe(planStart);
        period.To.ShouldBe(Today);
        period.TotalDays.ShouldBe(201);
    }

    [Fact]
    public void A_window_longer_than_the_plan_is_clamped_and_flagged()
    {
        // A member four days into their plan asking for a quarter gets four days, said plainly -
        // not eighty-six empty days dressed up as a quiet period.
        var planStart = Today.AddDays(-3);

        var period = _resolver.Resolve(PeriodPreset.Quarter, planStart, Clock);

        period.From.ShouldBe(planStart);
        period.TotalDays.ShouldBe(4);
        period.WasNarrowed.ShouldBeTrue();
    }

    [Fact]
    public void A_plan_starting_today_gives_a_single_day()
    {
        var period = _resolver.Resolve(PeriodPreset.Month, Today, Clock);

        period.From.ShouldBe(Today);
        period.To.ShouldBe(Today);
        period.TotalDays.ShouldBe(1);
        period.WasNarrowed.ShouldBeTrue();
    }

    [Fact]
    public void A_plan_dated_in_the_future_is_treated_as_starting_today()
    {
        // Not reachable through the plan rules, but a resolver that threw here would take out the
        // whole page over a date the member cannot fix from this screen.
        var period = _resolver.Resolve(PeriodPreset.Week, Today.AddDays(5), Clock);

        period.From.ShouldBe(Today);
        period.To.ShouldBe(Today);
        period.TotalDays.ShouldBe(1);
    }

    [Fact]
    public void The_comparison_window_is_the_span_of_the_same_length_immediately_before()
    {
        var period = _resolver.Resolve(PeriodPreset.Week, Today.AddDays(-365), Clock);

        period.HasComparison.ShouldBeTrue();
        period.PreviousTo.ShouldBe(period.From.AddDays(-1));
        period.PreviousFrom.ShouldBe(period.From.AddDays(-7));

        var previousLength = period.PreviousTo!.Value.DayNumber - period.PreviousFrom!.Value.DayNumber + 1;
        previousLength.ShouldBe(period.TotalDays);
    }

    [Fact]
    public void There_is_no_comparison_when_the_preceding_window_would_predate_the_plan()
    {
        // A partial window compared against a full one would report a fall that is an artefact of
        // having joined recently. Reporting nothing is more honest than reporting that.
        var period = _resolver.Resolve(PeriodPreset.Week, Today.AddDays(-9), Clock);

        period.HasComparison.ShouldBeFalse();
        period.PreviousFrom.ShouldBeNull();
        period.PreviousTo.ShouldBeNull();
    }

    [Fact]
    public void The_whole_plan_preset_never_has_a_comparison()
    {
        // There is nothing before a plan started.
        var period = _resolver.Resolve(PeriodPreset.Plan, Today.AddDays(-500), Clock);

        period.HasComparison.ShouldBeFalse();
    }

    [Fact]
    public void A_period_starts_with_no_logged_days_until_the_store_is_asked()
    {
        var period = _resolver.Resolve(PeriodPreset.Month, Today.AddDays(-365), Clock);

        period.LoggedDays.ShouldBe(0);

        var completed = period.WithLoggedDays(22);

        completed.LoggedDays.ShouldBe(22);
        completed.From.ShouldBe(period.From);
        completed.To.ShouldBe(period.To);
        completed.HasComparison.ShouldBe(period.HasComparison);
    }

    [Fact]
    public void More_logged_days_than_the_period_holds_is_refused()
    {
        var period = _resolver.Resolve(PeriodPreset.Week, Today.AddDays(-365), Clock);

        Should.Throw<DomainException>(() => period.WithLoggedDays(8));
    }

    [Fact]
    public void Resolving_twice_at_the_same_moment_gives_the_same_period()
    {
        var first = _resolver.Resolve(PeriodPreset.Month, Today.AddDays(-100), Clock);
        var second = _resolver.Resolve(PeriodPreset.Month, Today.AddDays(-100), Clock);

        first.ShouldBe(second);
    }
}
