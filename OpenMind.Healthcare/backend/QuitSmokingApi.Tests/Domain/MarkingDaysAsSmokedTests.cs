using DDD.BuildingBlocks;
using QuitSmokingApi.Domain.Entities;
using QuitSmokingApi.Tests.TestSupport;

namespace QuitSmokingApi.Tests.Domain;

public class MarkingDaysAsSmokedTests
{
    [Fact]
    public void Marking_a_day_records_what_was_smoked_and_why()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30);
        var journey = builder.Build();

        journey.MarkDayAsSmoked(builder.DaysAgo(4), 7, RelapseTrigger.Stress, "deadline week", builder.Clock);

        var day = journey.GetSmokedDay(builder.DaysAgo(4)).ShouldNotBeNull();
        day.Date.ShouldBe(builder.DaysAgo(4));
        day.CigarettesSmoked.ShouldBe(7);
        day.Trigger.ShouldBe(RelapseTrigger.Stress);
        day.Note.ShouldBe("deadline week");
        journey.IsSmokedDay(builder.DaysAgo(4)).ShouldBeTrue();
    }

    [Fact]
    public void A_day_that_was_never_marked_is_not_a_smoked_day()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30).SmokedDaysAgo(4);
        var journey = builder.Build();

        journey.IsSmokedDay(builder.DaysAgo(5)).ShouldBeFalse();
        journey.GetSmokedDay(builder.DaysAgo(5)).ShouldBeNull();
    }

    [Fact]
    public void Marking_the_same_day_again_corrects_the_record_rather_than_adding_a_second_one()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(30)
            .SmokedDaysAgo(4, cigarettes: 3, trigger: RelapseTrigger.Boredom, note: "first guess");
        var journey = builder.Build();

        journey.MarkDayAsSmoked(builder.DaysAgo(4), 9, RelapseTrigger.Social, "actually a party", builder.Clock);

        journey.SmokedDays.Count.ShouldBe(1);
        var day = journey.GetSmokedDay(builder.DaysAgo(4)).ShouldNotBeNull();
        day.CigarettesSmoked.ShouldBe(9);
        day.Trigger.ShouldBe(RelapseTrigger.Social);
        day.Note.ShouldBe("actually a party");
    }

    [Fact]
    public void A_day_before_the_quit_date_cannot_be_marked()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30);
        var journey = builder.Build();

        var mark = () => journey.MarkDayAsSmoked(builder.DaysAgo(31), 1, asOf: builder.Clock);

        mark.ShouldThrow<BusinessRuleValidationException>()
            .Message.ShouldContain("cannot be earlier than the quit date");
    }

    [Fact]
    public void A_day_in_the_future_cannot_be_marked()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30);
        var journey = builder.Build();

        var mark = () => journey.MarkDayAsSmoked(builder.Today.AddDays(1), 1, asOf: builder.Clock);

        mark.ShouldThrow<BusinessRuleValidationException>()
            .Message.ShouldBe("A smoked day cannot be in the future");
    }

    [Fact]
    public void Today_can_be_marked()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30);
        var journey = builder.Build();

        journey.MarkDayAsSmoked(builder.Today, 2, asOf: builder.Clock);

        journey.IsSmokedDay(builder.Today).ShouldBeTrue();
    }

    [Fact]
    public void The_quit_day_itself_can_be_marked()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30);
        var journey = builder.Build();

        journey.MarkDayAsSmoked(journey.QuitDay, 2, asOf: builder.Clock);

        journey.IsSmokedDay(builder.DaysAgo(30)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Marking_a_day_means_at_least_one_cigarette_was_smoked(int cigarettes)
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30);
        var journey = builder.Build();

        var mark = () => journey.MarkDayAsSmoked(builder.DaysAgo(2), cigarettes, asOf: builder.Clock);

        mark.ShouldThrow<BusinessRuleValidationException>()
            .Message.ShouldBe("Cigarettes smoked must be at least one");
    }

    [Fact]
    public void A_rejected_correction_leaves_the_existing_record_untouched()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30).SmokedDaysAgo(2, cigarettes: 4);
        var journey = builder.Build();

        var mark = () => journey.MarkDayAsSmoked(builder.DaysAgo(2), 0, asOf: builder.Clock);
        mark.ShouldThrow<BusinessRuleValidationException>();

        journey.GetSmokedDay(builder.DaysAgo(2))!.CigarettesSmoked.ShouldBe(4);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_blank_note_is_stored_as_no_note_at_all(string? note)
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30);
        var journey = builder.Build();

        journey.MarkDayAsSmoked(builder.DaysAgo(2), 1, RelapseTrigger.Habit, note, builder.Clock);

        journey.GetSmokedDay(builder.DaysAgo(2))!.Note.ShouldBeNull();
    }

    [Fact]
    public void Surrounding_whitespace_is_stripped_from_a_note()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30);
        var journey = builder.Build();

        journey.MarkDayAsSmoked(builder.DaysAgo(2), 1, RelapseTrigger.Habit, "  after dinner  ", builder.Clock);

        journey.GetSmokedDay(builder.DaysAgo(2))!.Note.ShouldBe("after dinner");
    }

    [Fact]
    public void An_over_long_note_is_cut_down_rather_than_rejected()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30);
        var journey = builder.Build();

        journey.MarkDayAsSmoked(builder.DaysAgo(2), 1, RelapseTrigger.Habit, new string('x', 900), builder.Clock);

        journey.GetSmokedDay(builder.DaysAgo(2))!.Note!.Length.ShouldBe(SmokedDay.MaxNoteLength);
    }

    [Fact]
    public void Unmarking_a_day_removes_it_from_the_journey()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30).SmokedDaysAgo(4).SmokedDaysAgo(9);
        var journey = builder.Build();

        var removed = journey.UnmarkSmokedDay(builder.DaysAgo(4));

        removed.ShouldBeTrue();
        journey.IsSmokedDay(builder.DaysAgo(4)).ShouldBeFalse();
        journey.SmokedDays.Select(d => d.Date).ShouldBe([builder.DaysAgo(9)]);
    }

    [Fact]
    public void Unmarking_a_day_that_was_never_marked_changes_nothing()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(30).SmokedDaysAgo(9);
        var journey = builder.Build();

        var removed = journey.UnmarkSmokedDay(builder.DaysAgo(4));

        removed.ShouldBeFalse();
        journey.SmokedDays.Count.ShouldBe(1);
    }

    [Fact]
    public void Smoked_days_can_be_listed_for_a_date_range()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(60)
            .SmokedDaysAgo(50)
            .SmokedDaysAgo(20)
            .SmokedDaysAgo(10)
            .SmokedDaysAgo(1);
        var journey = builder.Build();

        var inRange = journey.GetSmokedDaysBetween(builder.DaysAgo(20), builder.DaysAgo(10));

        inRange.Select(d => d.Date).ShouldBe([builder.DaysAgo(20), builder.DaysAgo(10)]);
    }

    [Fact]
    public void Smoked_days_are_listed_oldest_first()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(60)
            .SmokedDaysAgo(3)
            .SmokedDaysAgo(40)
            .SmokedDaysAgo(20);
        var journey = builder.Build();

        var days = journey.GetSmokedDaysInJourney(builder.Clock);

        days.Select(d => d.Date).ShouldBe([builder.DaysAgo(40), builder.DaysAgo(20), builder.DaysAgo(3)]);
    }
}
