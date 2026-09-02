using DietApi.Domain.ValueObjects;
using DietApi.Tests.TestSupport;

namespace DietApi.Tests.Domain;

/// <summary>
/// What happens when a member deletes a day's last entry.
/// </summary>
/// <remarks>
/// Leaving a zero-calorie shell behind would create a day that counts as perfectly on target
/// precisely because the member logged nothing - the exact outcome the "no entries means not
/// logged" rule exists to prevent. So the day empties and the repository deletes it.
/// </remarks>
public class EmptyDayTests
{
    [Fact]
    public void Removing_the_last_entry_leaves_the_day_empty()
    {
        var day = LoggedDayBuilder.ADay().Ate(FakeFoodLibraryRepository.Oats()).Build();

        day.IsEmpty.ShouldBeFalse();

        day.RemoveEntry(day.Entries.Single().Id);

        day.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void An_emptied_day_reports_not_logged_rather_than_a_zero_calorie_success()
    {
        var day = LoggedDayBuilder.ADay().Targeting(2100).Ate(FakeFoodLibraryRepository.Oats()).Build();

        day.RemoveEntry(day.Entries.Single().Id);

        var assessment = day.Assess();

        assessment.State.ShouldBe(DayState.NotLogged);
        assessment.ConsumedCalories.ShouldBe(0);

        // Note what this is *not*: zero calories against a 2100 target would otherwise be the most
        // compliant day imaginable.
        assessment.State.ShouldNotBe(DayState.OnTarget);
    }

    [Fact]
    public void Removing_one_of_several_entries_leaves_the_day_alive()
    {
        var day = LoggedDayBuilder.ADay()
            .Ate(FakeFoodLibraryRepository.Oats())
            .Ate(FakeFoodLibraryRepository.Banana(), meal: MealType.Snack)
            .Build();

        day.RemoveEntry(day.Entries.First().Id);

        day.IsEmpty.ShouldBeFalse();
        day.Assess().State.ShouldNotBe(DayState.NotLogged);
    }
}
