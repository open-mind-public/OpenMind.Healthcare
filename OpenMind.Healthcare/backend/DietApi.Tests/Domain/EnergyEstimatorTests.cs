using DietApi.Domain.Services;

namespace DietApi.Tests.Domain;

/// <summary>
/// The estimate members see beside every session. Pinned at its worked example and at its
/// boundaries, because a silent change to this arithmetic would rewrite what every past session
/// appeared to cost.
/// </summary>
public class EnergyEstimatorTests
{
    private readonly EnergyEstimator _estimator = new();

    [Fact]
    public void The_worked_example_from_the_research_holds()
    {
        // 8.3 MET x 70 kg x 0.75 h = 435.75, rounded away from zero.
        _estimator.Estimate(met: 8.3m, durationMinutes: 45, weightKg: 70m).ShouldBe(436);
    }

    [Fact]
    public void An_hour_at_one_met_costs_roughly_body_weight_in_calories()
    {
        // The formula's own sanity check: 1 MET for one hour is about 1 kcal per kilogram.
        _estimator.Estimate(met: 1m, durationMinutes: 60, weightKg: 70m).ShouldBe(70);
    }

    [Fact]
    public void The_estimate_scales_with_body_weight()
    {
        var lighter = _estimator.Estimate(8.3m, 45, 60m);
        var heavier = _estimator.Estimate(8.3m, 45, 90m);

        heavier.ShouldBeGreaterThan(lighter);

        // Same activity, same duration: the ratio is the weight ratio, give or take rounding.
        (heavier / (decimal)lighter).ShouldBe(1.5m, tolerance: 0.01m);
    }

    [Fact]
    public void The_estimate_scales_with_duration()
    {
        var fortyFive = _estimator.Estimate(8.3m, 45, 70m);
        var ninety = _estimator.Estimate(8.3m, 90, 70m);

        // Allowing a kilocalorie either way: doubling the duration doubles the estimate, and
        // rounding each independently can differ by one.
        ninety.ShouldBeInRange(fortyFive * 2 - 1, fortyFive * 2 + 1);
    }

    [Fact]
    public void A_recorded_session_never_reads_as_zero()
    {
        // 2.3 MET x 30 kg x 1/60 h = 1.15 kcal - and a lighter, gentler minute would round to
        // nothing at all. A member who bothered to record it should not be told it cost nothing.
        _estimator.Estimate(met: 2.3m, durationMinutes: 1, weightKg: 20m).ShouldBe(EnergyEstimator.FloorKcal);
        _estimator.Estimate(met: 2.3m, durationMinutes: 1, weightKg: 20m).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void A_session_with_no_duration_is_not_estimated_at_all()
    {
        // The floor applies to sessions that happened. Nothing is not a session, and the rules
        // refuse it before this is ever reached - but the estimator does not invent a calorie
        // for it either.
        _estimator.Estimate(8.3m, 0, 70m).ShouldBe(0);
    }

    [Fact]
    public void The_estimate_is_a_whole_number_of_kilocalories()
    {
        // Not a rounding preference: minutes and kilocalories are int columns because the weekly
        // summary aggregates them in SQL, and SQLite cannot sum a decimal (ADR 0002).
        _estimator.Estimate(7.3m, 37, 81.4m).ShouldBeOfType<int>();
    }
}
