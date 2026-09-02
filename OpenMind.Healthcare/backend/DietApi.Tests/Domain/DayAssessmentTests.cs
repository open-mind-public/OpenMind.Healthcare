using DietApi.Domain.ValueObjects;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// A day judges itself against the target that was snapshotted onto it, not against whatever the
/// plan says today. That is what keeps history stable when a member changes their target.
/// </summary>
public class DayAssessmentTests
{
    [Fact]
    public void A_day_under_target_is_on_target()
    {
        var day = LoggedDayBuilder.ADay().Targeting(2100)
            .Ate(FakeFoodLibraryRepository.Oats())
            .Build();

        var assessment = day.Assess();

        assessment.State.ShouldBe(DayState.OnTarget);
        assessment.ConsumedCalories.ShouldBe(228);
        assessment.RemainingCalories.ShouldBe(1872);
        assessment.OverageCalories.ShouldBe(0);
    }

    [Fact]
    public void A_day_exactly_at_target_is_on_target()
    {
        // The boundary belongs to the member: hitting the target exactly is a success.
        var day = LoggedDayBuilder.ADay().Targeting(228)
            .Ate(FakeFoodLibraryRepository.Oats())
            .Build();

        var assessment = day.Assess();

        assessment.State.ShouldBe(DayState.OnTarget);
        assessment.RemainingCalories.ShouldBe(0);
        assessment.OverageCalories.ShouldBe(0);
    }

    [Fact]
    public void A_day_one_calorie_over_is_over_target()
    {
        var day = LoggedDayBuilder.ADay().Targeting(227)
            .Ate(FakeFoodLibraryRepository.Oats())
            .Build();

        var assessment = day.Assess();

        assessment.State.ShouldBe(DayState.OverTarget);
        assessment.RemainingCalories.ShouldBe(-1);
        assessment.OverageCalories.ShouldBe(1);
    }

    [Fact]
    public void An_over_target_day_reports_the_size_of_the_overage()
    {
        var day = LoggedDayBuilder.ADay().Targeting(300)
            .Ate(FakeFoodLibraryRepository.Oats(), quantity: 3m)
            .Build();

        var assessment = day.Assess();

        assessment.ConsumedCalories.ShouldBe(684);
        assessment.OverageCalories.ShouldBe(384);
    }

    [Fact]
    public void A_day_with_no_entries_is_not_logged_rather_than_perfectly_compliant()
    {
        var day = LoggedDayBuilder.ADay().Targeting(2100).Build();

        day.Assess().State.ShouldBe(DayState.NotLogged);
    }

    [Fact]
    public void A_day_keeps_the_target_that_was_in_force_when_it_was_logged()
    {
        // The plan's target may since have been lowered to 1500. The day does not know or care -
        // it holds its own snapshot, so it cannot be retroactively re-judged.
        var day = LoggedDayBuilder.ADay().Targeting(2100)
            .Ate(FakeFoodLibraryRepository.Oats(), quantity: 8m)
            .Build();

        var assessment = day.Assess();

        assessment.TargetCalories.ShouldBe(2100);
        assessment.ConsumedCalories.ShouldBe(1824);
        assessment.State.ShouldBe(DayState.OnTarget);
    }

    [Fact]
    public void The_assessment_is_recomputed_from_the_entries_every_time()
    {
        var day = LoggedDayBuilder.ADay().Targeting(300)
            .Ate(FakeFoodLibraryRepository.Oats())
            .Build();

        day.Assess().State.ShouldBe(DayState.OnTarget);

        var oats = FakeFoodLibraryRepository.Oats();
        var serving = oats.ServingSizes.First();
        day.AddEntry(oats.Id, serving.Id, oats.Name, serving.Label, 1m, MealType.Lunch, serving.Nutrition);

        day.Assess().State.ShouldBe(DayState.OverTarget);
    }
}
