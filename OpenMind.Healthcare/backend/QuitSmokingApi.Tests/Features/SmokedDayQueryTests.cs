using QuitSmokingApi.Domain.Entities;
using QuitSmokingApi.Domain.ValueObjects;
using QuitSmokingApi.Features.SmokedDays.GetRelapseAnalytics;
using QuitSmokingApi.Features.SmokedDays.GetSmokedDays;
using QuitSmokingApi.Tests.TestSupport;

namespace QuitSmokingApi.Tests.Features;

public class GetSmokedDaysHandlerTests
{
    [Fact]
    public async Task A_user_with_no_journey_has_no_smoked_days_to_show()
    {
        var handler = new GetSmokedDaysHandler(FakeQuitJourneyRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        var days = await handler.Handle(new GetSmokedDaysQuery(), CancellationToken.None);

        days.ShouldBeEmpty();
    }

    [Fact]
    public async Task Every_marked_day_is_returned_oldest_first_when_no_range_is_asked_for()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(60).SmokedDaysAgo(5).SmokedDaysAgo(40).SmokedDaysAgo(20);
        var journey = builder.Build();
        var handler = new GetSmokedDaysHandler(FakeQuitJourneyRepository.Containing(journey), SignedInUser.WithId(journey.UserId));

        var days = await handler.Handle(new GetSmokedDaysQuery(), CancellationToken.None);

        days.Select(d => d.Date).ShouldBe([builder.DaysAgo(40), builder.DaysAgo(20), builder.DaysAgo(5)]);
    }

    [Fact]
    public async Task Only_the_days_inside_the_asked_for_range_come_back()
    {
        var builder = JourneyBuilder.AJourney().StartedDaysAgo(60).SmokedDaysAgo(50).SmokedDaysAgo(20).SmokedDaysAgo(10).SmokedDaysAgo(1);
        var journey = builder.Build();
        var handler = new GetSmokedDaysHandler(FakeQuitJourneyRepository.Containing(journey), SignedInUser.WithId(journey.UserId));

        var days = await handler.Handle(
            new GetSmokedDaysQuery(builder.DaysAgo(20), builder.DaysAgo(10)),
            CancellationToken.None);

        days.Select(d => d.Date).ShouldBe([builder.DaysAgo(20), builder.DaysAgo(10)]);
    }

    [Fact]
    public async Task Each_day_reports_what_was_smoked_what_it_cost_and_why()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(30)
            .Smoking(20, 20, 40_000m, "VND")
            .SmokedDaysAgo(4, cigarettes: 3, trigger: RelapseTrigger.Coffee, note: " morning coffee ");
        var journey = builder.Build();
        var handler = new GetSmokedDaysHandler(FakeQuitJourneyRepository.Containing(journey), SignedInUser.WithId(journey.UserId));

        var day = (await handler.Handle(new GetSmokedDaysQuery(), CancellationToken.None)).ShouldHaveSingleItem();

        day.CigarettesSmoked.ShouldBe(3);
        day.Trigger.ShouldBe(nameof(RelapseTrigger.Coffee));
        day.Note.ShouldBe("morning coffee");
        day.MoneySpent.ShouldBe(6_000m); // three cigarettes at 2,000 each
        day.Currency.ShouldBe("VND");
    }

    [Fact]
    public async Task A_caller_without_an_identity_is_refused()
    {
        var handler = new GetSmokedDaysHandler(FakeQuitJourneyRepository.Empty(), SignedInUser.Anonymous());

        var handle = async () => await handler.Handle(new GetSmokedDaysQuery(), CancellationToken.None);

        await handle.ShouldThrowAsync<UnauthorizedAccessException>();
    }
}

public class GetRelapseAnalyticsHandlerTests
{
    [Fact]
    public async Task A_user_with_no_journey_gets_an_empty_analytics_snapshot_rather_than_an_error()
    {
        var handler = new GetRelapseAnalyticsHandler(FakeQuitJourneyRepository.Empty(), SignedInUser.WithId(Guid.NewGuid()));

        var analytics = await handler.Handle(new GetRelapseAnalyticsQuery(), CancellationToken.None);

        analytics.TotalDaysInJourney.ShouldBe(0);
        analytics.SmokedDays.ShouldBe(0);
        analytics.MoneySpentOnRelapses.ShouldBe(0m);
        analytics.Currency.ShouldBe("USD");
        analytics.Trend.ShouldBe(nameof(RelapseTrend.NotEnoughData));
        analytics.MostCommonTrigger.ShouldBeNull();
        analytics.RiskiestWeekday.ShouldBeNull();
        analytics.TriggerBreakdown.ShouldBeEmpty();
        analytics.WeekdayBreakdown.ShouldBeEmpty();
        analytics.MonthlyBreakdown.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_snapshot_names_the_triggers_weekdays_and_months_in_readable_form()
    {
        var builder = JourneyBuilder.AJourney()
            .StartedDaysAgo(90)
            .Smoking(20, 20, 10m)
            .SmokedDaysAgo(7, cigarettes: 5, trigger: RelapseTrigger.WorkPressure)
            .SmokedDaysAgo(14, cigarettes: 4, trigger: RelapseTrigger.WorkPressure);
        var journey = builder.Build();
        var handler = new GetRelapseAnalyticsHandler(FakeQuitJourneyRepository.Containing(journey), SignedInUser.WithId(journey.UserId));

        var analytics = await handler.Handle(new GetRelapseAnalyticsQuery(), CancellationToken.None);

        analytics.SmokedDays.ShouldBe(2);
        analytics.TotalCigarettesSmoked.ShouldBe(9);
        analytics.MoneySpentOnRelapses.ShouldBe(4.5m);
        analytics.LifeLostFormatted.ShouldBe("1 hours 39 minutes");
        analytics.MostCommonTrigger.ShouldBe(nameof(RelapseTrigger.WorkPressure));
        analytics.TriggerBreakdown.ShouldHaveSingleItem().Trigger.ShouldBe(nameof(RelapseTrigger.WorkPressure));
        analytics.RiskiestWeekday.ShouldBe(builder.Today.DayOfWeek.ToString());
        analytics.WeekdayBreakdown.Count.ShouldBe(7);
        analytics.MonthlyBreakdown.ShouldNotBeEmpty();
        analytics.MonthlyBreakdown.ShouldAllBe(m => m.Label.Length > 0);
    }

    [Fact]
    public async Task A_caller_without_an_identity_is_refused()
    {
        var handler = new GetRelapseAnalyticsHandler(FakeQuitJourneyRepository.Empty(), SignedInUser.Anonymous());

        var handle = async () => await handler.Handle(new GetRelapseAnalyticsQuery(), CancellationToken.None);

        await handle.ShouldThrowAsync<UnauthorizedAccessException>();
    }
}
