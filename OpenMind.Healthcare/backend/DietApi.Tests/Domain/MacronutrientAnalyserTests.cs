using DietApi.Domain.Repositories;
using DietApi.Domain.Services;

namespace DietApi.Tests.Domain;

/// <summary>
/// Macronutrients against the targets that were in force.
/// </summary>
/// <remarks>
/// The test that matters here is <see cref="A_period_spanning_a_target_change_averages_the_targets_that_were_in_force"/>.
/// Comparing a month against today's target is the obvious implementation and it is wrong: it
/// re-judges days the member already saw assessed, against a number that did not exist when they
/// ate. Per-day target snapshots exist precisely so that cannot happen (FR-011, SC-006).
/// </remarks>
public class MacronutrientAnalyserTests
{
    private readonly MacronutrientAnalyser _analyser = new();

    private static readonly DateOnly Start = new(2026, 3, 1);

    [Fact]
    public void Amounts_are_daily_averages_over_the_logged_days()
    {
        var comparison = _analyser.Analyse(
        [
            Row(Start, protein: 100m, carbs: 200m, fat: 60m),
            Row(Start.AddDays(1), protein: 140m, carbs: 240m, fat: 80m)
        ]);

        comparison.ProteinG.ShouldBe(120m);
        comparison.CarbsG.ShouldBe(220m);
        comparison.FatG.ShouldBe(70m);
        comparison.AveragedOverDays.ShouldBe(2);
    }

    [Fact]
    public void A_period_spanning_a_target_change_averages_the_targets_that_were_in_force()
    {
        // Two days at a 160 g protein target, two at 120 g. The honest answer is 140 - the average
        // of what the member was actually aiming for - not whichever target happens to be current.
        var comparison = _analyser.Analyse(
        [
            Row(Start, protein: 100m, targetProtein: 160m),
            Row(Start.AddDays(1), protein: 100m, targetProtein: 160m),
            Row(Start.AddDays(2), protein: 100m, targetProtein: 120m),
            Row(Start.AddDays(3), protein: 100m, targetProtein: 120m)
        ]);

        comparison.TargetProteinG.ShouldBe(140m);
        comparison.HasTargets.ShouldBeTrue();
    }

    [Fact]
    public void A_plan_with_no_macronutrient_targets_still_reports_the_split()
    {
        var comparison = _analyser.Analyse(
        [
            Row(Start, protein: 120m, carbs: 200m, fat: 70m,
                targetProtein: null, targetCarbs: null, targetFat: null)
        ]);

        comparison.HasTargets.ShouldBeFalse();
        comparison.TargetProteinG.ShouldBeNull();
        comparison.ProteinG.ShouldBe(120m);
        comparison.ProteinShare.ShouldBeGreaterThan(0m);
    }

    [Fact]
    public void Days_without_a_target_are_excluded_from_its_average_rather_than_counted_as_zero()
    {
        // Counting the untargeted day as zero would give 80 g and make the member look closer to
        // their target than they were - a comparison invented out of an absence.
        var comparison = _analyser.Analyse(
        [
            Row(Start, protein: 100m, targetProtein: 160m),
            Row(Start.AddDays(1), protein: 100m, targetProtein: null)
        ]);

        comparison.TargetProteinG.ShouldBe(160m);
    }

    [Fact]
    public void Energy_shares_are_taken_from_the_macronutrients_and_sum_to_one_hundred()
    {
        // 100 g protein (400 kcal) + 200 g carbs (800) + 100 g fat (900) = 2,100 kcal.
        var comparison = _analyser.Analyse([Row(Start, protein: 100m, carbs: 200m, fat: 100m)]);

        comparison.ProteinShare.ShouldBe(19.0m, tolerance: 0.1m);
        comparison.CarbsShare.ShouldBe(38.1m, tolerance: 0.1m);
        comparison.FatShare.ShouldBe(42.9m, tolerance: 0.1m);

        (comparison.ProteinShare + comparison.CarbsShare + comparison.FatShare)
            .ShouldBe(100m, tolerance: 0.2m);
    }

    [Fact]
    public void A_period_with_nothing_logged_is_zeros_rather_than_a_division_by_zero()
    {
        var comparison = _analyser.Analyse([]);

        comparison.ProteinG.ShouldBe(0m);
        comparison.AveragedOverDays.ShouldBe(0);
        comparison.HasTargets.ShouldBeFalse();
        comparison.ProteinShare.ShouldBe(0m);
    }

    [Fact]
    public void Protein_attainment_is_the_share_of_target_reached_or_null_without_one()
    {
        _analyser.Analyse([Row(Start, protein: 96m, targetProtein: 120m)])
            .ProteinAttainment.ShouldBe(0.8m);

        _analyser.Analyse([Row(Start, protein: 96m, targetProtein: null)])
            .ProteinAttainment.ShouldBeNull();
    }

    [Fact]
    public void Fractional_grams_survive_being_averaged()
    {
        // The reason these stay decimal and are summed in memory rather than in SQL (ADR 0002).
        // (8.4 + 1.3) / 2 = 4.85, which lands exactly on a rounding boundary. Math.Round rounds
        // half to even by default, so this is 4.8 - the same convention NutritionValues already
        // uses for macronutrient grams. Pinned rather than left to chance.
        var comparison = _analyser.Analyse(
        [
            Row(Start, protein: 8.4m),
            Row(Start.AddDays(1), protein: 1.3m)
        ]);

        comparison.ProteinG.ShouldBe(4.8m);
    }

    internal static DayIntakeRow Row(
        DateOnly date,
        decimal protein = 100m,
        decimal carbs = 200m,
        decimal fat = 70m,
        int calories = 2100,
        int targetCalories = 2100,
        decimal? targetProtein = 157.5m,
        decimal? targetCarbs = 210m,
        decimal? targetFat = 70m) =>
        new(date, calories, protein, carbs, fat, targetCalories, targetProtein, targetCarbs, targetFat);
}
